using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(30)]
    public class DuplicateCheckerPlugin : IEventPlugin, IDisposable {
        private readonly ConcurrentQueue<RecentErrorDetail> _recentlyProcessedErrors = new ConcurrentQueue<RecentErrorDetail>();
        private readonly Timer _timer;

        public DuplicateCheckerPlugin() {
            _timer = new Timer(OnTimer, _recentlyProcessedErrors, -1, 10000);
        }

        public void Dispose() {
            _timer.Dispose();
            GC.SuppressFinalize(this);
        }

        private static void OnTimer(object state) {
            var queue = (ConcurrentQueue<RecentErrorDetail>)state;
            DateTime repeatWindowCap = DateTime.Now.AddSeconds(-30);

            var expired = queue.Where(x => x.FirstOccurrence < repeatWindowCap).ToList();

            foreach (var recentError in expired) {
                recentError.Send();
            }
        }

        public void Run(EventPluginContext context) {
            // If Event.Value is set before we hit this plugin, it's being used for something else for a good reason. Don't deduplicate. This also prevents problems with reentrancy
            if (context.Event.Value.HasValue)
                return;

            int hashCode = context.Event.GetHashCode();

            // make sure that we don't process the same error multiple times within 2 seconds.
            var recentError = _recentlyProcessedErrors.FirstOrDefault(s => s.HashCode == hashCode);
            if (recentError != null) {
                recentError.IncrementCount();

                context.Log.FormattedInfo(typeof(ExceptionlessClient), "Ignoring duplicate error event: hash={0} count={1}", hashCode, recentError.Count);
                context.Cancel = true;

                return;
            }

            // add this exception to our list of recent errors that we have processed.
            _recentlyProcessedErrors.Enqueue(new RecentErrorDetail(hashCode, context));

            // only keep the last 10 recent errors
            RecentErrorDetail temp;
            while (_recentlyProcessedErrors.Count > 10)
                _recentlyProcessedErrors.TryDequeue(out temp);
        }

        class RecentErrorDetail {

            public RecentErrorDetail(int hashCode, EventPluginContext context) {
                HashCode = hashCode;
                Event = context.Event;
                Client = context.Client;
                ContextData = context.ContextData;
                FirstOccurrence = LastOccurrence = DateTime.Now;
                Count = 1;
            }

            public ContextData ContextData { get; private set; }

            public Event Event { get; private set; }
            public ExceptionlessClient Client { get; private set; }
            public int HashCode { get; private set; }
            public DateTime FirstOccurrence { get; private set; }
            public DateTime LastOccurrence { get; private set; }
            public int Count { get; private set; }

            public void IncrementCount() {
                if (Count == 0) {
                    // Handle the case after the reset.
                    FirstOccurrence = DateTime.Now;
                }

                Count++;
                LastOccurrence = DateTime.Now;
            }

            public void Send() {
                Event.Value = Count;

                // reset counter to be ready for the next wave
                Count = 0;

                Client.SubmitEvent(Event, ContextData);
            }
        }
    }
}