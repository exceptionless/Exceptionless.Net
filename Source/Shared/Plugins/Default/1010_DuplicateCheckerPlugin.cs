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
        private Timer _timer;

        public DuplicateCheckerPlugin(TimeSpan? interval = null) {
            if (!interval.HasValue)
                interval = TimeSpan.FromSeconds(5);

            _timer = new Timer(OnTimer, null, interval.Value, interval.Value);
        }
        
        private void OnTimer(object state) {
            // NOTE: There's a chance the timer runs just after the duplicate occurred, that's not a problem because it will catch a lot of other duplicates from occurring.
            MergedEvent mergedEvent;
            while (_mergedEvents.TryDequeue(out mergedEvent))
                mergedEvent.Send();
        }

        public void Run(EventPluginContext context) {
            // If Event.Value is set before we hit this plugin, it's being used for something else for a good reason. Don't deduplicate. This also prevents problems with reentrancy
            if (context.Event.Value.HasValue)
                return;

            int hashCode = context.Event.GetHashCode();
            
            // Increment the occurrence count if the event is already queued for submission.
            var merged = _mergedEvents.FirstOrDefault(s => s.HashCode == hashCode);
            if (merged != null) {
                merged.IncrementCount();
                context.Log.FormattedInfo(typeof(DuplicateCheckerPlugin), String.Concat("Ignoring duplicate error event with hash:", hashCode));
                context.Cancel = true;
                return;
            }

            DateTimeOffset repeatWindow = DateTimeOffset.UtcNow.AddSeconds(-2);
            if (_processed.Any(s => s.Item1 == hashCode && s.Item2 >= repeatWindow)) {
                // This event is a duplicate for the first time, lets save it so we can delay it while keeping count
                _mergedEvents.Enqueue(new MergedEvent(hashCode, context));
                context.Cancel = true;
            } else {
                _processed.Enqueue(Tuple.Create(hashCode, DateTimeOffset.UtcNow));
            }
            
            Tuple<int, DateTimeOffset> temp;
            while (_processed.Count > 50)
                _processed.TryDequeue(out temp);
        }

        public void Dispose() {
            if (_timer != null) {
                _timer.Dispose();
                _timer = null;
            }
        }

        private class MergedEvent {
            private int _count = 1;
            private readonly EventPluginContext _context;

            public MergedEvent(int hashCode, EventPluginContext context) {
                HashCode = hashCode;
                _context = context;
            }

            public int HashCode { get; }

            public void IncrementCount() {
                Interlocked.Increment(ref _count);
            }

            public void Send() {
                _context.Event.Value = _count;
                _context.Resolver.GetEventQueue().Enqueue(_context.Event);
            }
        }
    }
}