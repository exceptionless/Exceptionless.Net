using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Exceptionless.Logging;
using Exceptionless.Dependency;

namespace Exceptionless.Plugins.Default {
    [Priority(1010)]
    public class DuplicateCheckerPlugin : IEventPlugin, IDisposable {
        private readonly ConcurrentQueue<Tuple<int, DateTimeOffset>> _processed = new ConcurrentQueue<Tuple<int, DateTimeOffset>>();
        private readonly ConcurrentQueue<MergedEvent> _mergedEvents = new ConcurrentQueue<MergedEvent>();
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

            // Increment the occurrence count if the event is already queued for submission.
            var merged = _mergedEvents.FirstOrDefault(s => s.HashCode == hashCode);
            if (merged != null) {
                merged.IncrementCount(count);
                context.Log.FormattedInfo(typeof(DuplicateCheckerPlugin), String.Concat("Ignoring duplicate error event with hash:", hashCode));
                context.Cancel = true;
                return;
            }

            DateTimeOffset repeatWindow = DateTimeOffset.UtcNow.Subtract(_interval);
            if (_processed.Any(s => s.Item1 == hashCode && s.Item2 >= repeatWindow)) {
                // This event is a duplicate for the first time, lets save it so we can delay it while keeping count
                _mergedEvents.Enqueue(new MergedEvent(hashCode, context, count));
                context.Cancel = true;
            } else {
                _processed.Enqueue(Tuple.Create(hashCode, DateTimeOffset.UtcNow));
            }
            
            Tuple<int, DateTimeOffset> temp;
            while (_processed.Count > 50)
                _processed.TryDequeue(out temp);
        }
        
        private void OnTimer(object state) {
            EnqueueMergedEvents();
        }

        private void EnqueueMergedEvents() {
            MergedEvent mergedEvent;
            while (_mergedEvents.TryDequeue(out mergedEvent))
                mergedEvent.Enqueue();
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

            public void Enqueue() {
                _context.Event.Count = _count;
                _context.Resolver.GetEventQueue().Enqueue(_context.Event);
            }
        }
    }
}