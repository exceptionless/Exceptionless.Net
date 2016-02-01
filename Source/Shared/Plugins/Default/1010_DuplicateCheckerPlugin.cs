using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Dependency;

namespace Exceptionless.Plugins.Default {
    [Priority(1010)]
    public class DuplicateCheckerPlugin : IEventPlugin, IDisposable {
        private readonly ConcurrentQueue<Tuple<int, DateTimeOffset>> _recentlyProcessedErrors = new ConcurrentQueue<Tuple<int, DateTimeOffset>>();
        private readonly ConcurrentQueue<RecentErrorDetail> _recentDuplicates = new ConcurrentQueue<RecentErrorDetail>();
        private Timer _timer;

        public DuplicateCheckerPlugin() {
            _timer = new Timer(OnTimer, null, -1, 10000);
        }

        public void Dispose() {
            _timer.Dispose();
            _timer = null;

            GC.SuppressFinalize(this);
        }

        private void OnTimer(object state) {
            DateTime repeatWindowCap = DateTime.Now.AddSeconds(-30);

            var expired = _recentDuplicates.Where(x => x.FirstOccurrence < repeatWindowCap).ToList();

            foreach (var recentError in expired) {
                recentError.Send();
            }
        }

        public void Run(EventPluginContext context) {
            // If Event.Value is set before we hit this plugin, it's being used for something else for a good reason. Don't deduplicate. This also prevents problems with reentrancy
            if (context.Event.Value.HasValue)
                return;

            int hashCode = context.Event.GetHashCode();

            DateTimeOffset repeatWindow = DateTimeOffset.Now.AddSeconds(-2);
            if (_recentlyProcessedErrors.Any(s => s.Item1 == hashCode && s.Item2 >= repeatWindow))
            {
                context.Cancel = true;

                // Keep count of number of times the duplication occurs
                var recentError = _recentDuplicates.FirstOrDefault(s => s.HashCode == hashCode);
                if (recentError != null)
                {
                    recentError.IncrementCount();
                    context.Log.FormattedInfo(typeof(ExceptionlessClient), "Ignoring duplicate error event: hash={0}", hashCode);

                    return;
                }

                // This event is a duplicate for the first time, lets save it so we can delay it while keeping count
                _recentDuplicates.Enqueue(new RecentErrorDetail(hashCode, context));
            }

            // add this exception to our list of recent errors that we have processed.
            _recentlyProcessedErrors.Enqueue(Tuple.Create(hashCode, DateTimeOffset.Now));

            // only keep the last 10 recent errors
            Tuple<int, DateTimeOffset> temp;
            while (_recentlyProcessedErrors.Count > 10)
                _recentlyProcessedErrors.TryDequeue(out temp);
        }

        class RecentErrorDetail {

            public RecentErrorDetail(int hashCode, EventPluginContext context) {
                HashCode = hashCode;
                Context = context;
                FirstOccurrence = LastOccurrence = DateTime.Now;
                _count = 1;
            }

            public EventPluginContext Context { get; private set; }
            public int HashCode { get; private set; }
            public DateTime FirstOccurrence { get; private set; }
            public DateTime LastOccurrence { get; private set; }
            public int _count;

            public void IncrementCount() {
                if (_count == 0) {
                    // Handle the case after the reset.
                    FirstOccurrence = DateTime.Now;
                }

                Interlocked.Increment(ref _count);
                LastOccurrence = DateTime.Now;
            }

            public void Send() {
                Context.Event.Value = _count;

                // reset counter to be ready for the next wave
                _count = 0;

                Context.Resolver.GetEventQueue().Enqueue(Context.Event);
            }
        }
    }
}