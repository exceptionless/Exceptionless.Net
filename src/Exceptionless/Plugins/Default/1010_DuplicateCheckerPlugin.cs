using System;
using System.Threading;
using Exceptionless.Logging;
using Exceptionless.Dependency;
using System.Collections.Generic;
using System.Linq;

namespace Exceptionless.Plugins.Default {
    [Priority(1010)]
    public class DuplicateCheckerPlugin : IEventPlugin, IDisposable {
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
            int hashCode = context.Event.GetHashCode();
            int count = context.Event.Count ?? 1;
            context.Log.FormattedTrace(typeof(DuplicateCheckerPlugin), String.Concat("Checking event: ", context.Event.Message, " with hash: ", hashCode));

            lock (_lock) {
                // Increment the occurrence count if the event is already queued for submission.
                var merged = _mergedEvents.FirstOrDefault(s => s.HashCode == hashCode);
                if (merged != null) {
                    merged.IncrementCount(count);
                    merged.UpdateDate(context.Event.Date);
                    context.Log.FormattedInfo(typeof(DuplicateCheckerPlugin), String.Concat("Ignoring duplicate event with hash:", hashCode));
                    context.Cancel = true;
                    return;
                }

                DateTimeOffset repeatWindow = DateTimeOffset.UtcNow.Subtract(_interval);
                if (_processed.Any(s => s.Item1 == hashCode && s.Item2 >= repeatWindow)) {
                    context.Log.FormattedInfo(typeof(DuplicateCheckerPlugin), String.Concat("Adding event with hash:", hashCode, " to cache."));
                    // This event is a duplicate for the first time, lets save it so we can delay it while keeping count
                    _mergedEvents.Enqueue(new MergedEvent(hashCode, context, count));
                    context.Cancel = true;
                    return;
                }

                context.Log.FormattedInfo(typeof(DuplicateCheckerPlugin), String.Concat("Enqueueing event with hash:", hashCode, " to cache."));
                _processed.Enqueue(Tuple.Create(hashCode, DateTimeOffset.UtcNow));
                
                while (_processed.Count > 50)
                    _processed.Dequeue();
            }
        }

        private void OnTimer(object state) {
            EnqueueMergedEvents();
        }

        private void EnqueueMergedEvents() {
            lock (_lock) {
                while (_mergedEvents.Count > 0)
                    _mergedEvents.Dequeue().Resubmit();
            }
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
                _context.Resolver.GetEventQueue().Enqueue(_context.Event);
            }

            public void UpdateDate(DateTimeOffset date) {
                if (date > _context.Event.Date)
                    _context.Event.Date = date;
            }
        }
    }
}
