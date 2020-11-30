using System;
using System.Threading;
using Exceptionless.Logging;
using Exceptionless.Dependency;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(910)]
    public class DuplicateCheckerPlugin : IEventPlugin, IDisposable {
        private const string LOG_SOURCE = nameof(DuplicateCheckerPlugin);
        private static readonly Type _logSourceType = typeof(DuplicateCheckerPlugin);
        private readonly Queue<Tuple<int, DateTimeOffset>> _processed = new Queue<Tuple<int, DateTimeOffset>>();
        private readonly Queue<MergedEvent> _mergedEvents = new Queue<MergedEvent>();
        private readonly object _lock = new object();
        private readonly TimeSpan _interval;
        private Timer _timer;

        /// <summary>
        /// Duplicates events based on hashcode and interval.
        /// </summary>
        public DuplicateCheckerPlugin() : this(null) {}

        /// <summary>
        /// Duplicates events based on hashcode and interval.
        /// </summary>
        /// <param name="interval">The amount of time events will be deduplicated.</param>
        public DuplicateCheckerPlugin(TimeSpan? interval) {
            _interval = interval ?? TimeSpan.FromSeconds(60);
            _timer = new Timer(OnTimer, null, _interval, _interval);
        }

        public void Run(EventPluginContext context) {
            if (LOG_SOURCE == context.Event.Source)
                return;

            int hashCode = context.Event.GetHashCode();
            int count = context.Event.Count ?? 1;
            context.Log.FormattedTrace(_logSourceType, "Checking event: {0} with hash: {1}", context.Event.Message, hashCode);

            lock (_lock) {
                // Increment the occurrence count if the event is already queued for submission.
                var merged = _mergedEvents.FirstOrDefault(s => s.HashCode == hashCode);
                if (merged != null) {
                    merged.IncrementCount(count);
                    merged.UpdateDate(context.Event.Date);
                    context.Log.FormattedInfo(_logSourceType, "Ignoring duplicate event: {0} with hash: {1}", context.Event.Message, hashCode);
                    context.Cancel = true;
                    return;
                }

                var repeatWindow = DateTimeOffset.UtcNow.Subtract(_interval);
                if (_processed.Any(s => s.Item1 == hashCode && s.Item2 >= repeatWindow)) {
                    context.Log.FormattedTrace(_logSourceType, "Adding duplicate event: {0} with hash: {1} to cache for later submission.", context.Event.Message, hashCode);
                    // This event is a duplicate for the first time, lets save it so we can delay it while keeping count
                    _mergedEvents.Enqueue(new MergedEvent(hashCode, context, count));
                    context.Cancel = true;
                    return;
                }

                context.Log.FormattedTrace(_logSourceType, "Enqueueing event with hash: {0} to cache.", hashCode);
                _processed.Enqueue(Tuple.Create(hashCode, DateTimeOffset.UtcNow));

                while (_processed.Count > 50)
                    _processed.Dequeue();
            }
        }

        private void OnTimer(object state) {
            EnqueueMergedEvents();
        }

        private void EnqueueMergedEvents() {
            bool more;
            do {
                MergedEvent mergedEvent = null;
                lock (_lock) {
                    if (_mergedEvents.Count > 0) {
                        mergedEvent = _mergedEvents.Dequeue();
                    }
                    more = _mergedEvents.Count > 0;
                }
                mergedEvent?.Resubmit();
            } while (more);
        }

        public void Dispose() {
            EnqueueMergedEvents();

            if (_timer != null) {
                _timer.Dispose();
                _timer = null;
            }
        }

        private class MergedEvent {
            private int _count;
            private readonly EventPluginContext _context;

            public MergedEvent(int hashCode, EventPluginContext context, int count) {
                HashCode = hashCode;
                _context = context;
                _count = count;
            }

            public int HashCode { get; private set; }

            public void IncrementCount(int value) {
                Interlocked.Add(ref _count, value);
            }

            public void Resubmit() {
                _context.Event.Count = _count;

                // ensure all required data
                if (String.IsNullOrEmpty(_context.Event.Type))
                    _context.Event.Type = Event.KnownTypes.Log;
                if (_context.Event.Date == DateTimeOffset.MinValue)
                    _context.Event.Date = DateTimeOffset.Now;

                if (!_context.Client.OnSubmittingEvent(_context.Event, _context.ContextData)) {
                    _context.Log.FormattedInfo(_logSourceType, "Event submission cancelled by event handler: id={0} type={1}", _context.Event.ReferenceId, _context.Event.Type);
                    return;
                }

                _context.Log.FormattedTrace(_logSourceType, "Submitting event: type={0}{1}", _context.Event.Type, !String.IsNullOrEmpty(_context.Event.ReferenceId) ? " refid=" + _context.Event.ReferenceId : String.Empty);
                _context.Resolver.GetEventQueue().Enqueue(_context.Event);

                if (!String.IsNullOrEmpty(_context.Event.ReferenceId)) {
                    _context.Log.FormattedTrace(_logSourceType, "Setting last reference id: {0}", _context.Event.ReferenceId);
                    _context.Resolver.GetLastReferenceIdManager().SetLast(_context.Event.ReferenceId);
                }

                _context.Client.OnSubmittedEvent(new EventSubmittedEventArgs(_context.Client, _context.Event, _context.ContextData));
            }

            public void UpdateDate(DateTimeOffset date) {
                if (date > _context.Event.Date)
                    _context.Event.Date = date;
            }
        }
    }
}
