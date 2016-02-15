using System;
using System.Collections.Concurrent;
using System.Linq;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Models.Data;

namespace Exceptionless.Plugins.Default {
    [Priority(30)]
    public class DuplicateCheckerPlugin : IEventPlugin {
        private readonly ConcurrentQueue<Tuple<int, DateTime>> _recentlyProcessedErrors = new ConcurrentQueue<Tuple<int, DateTime>>();

        public void Run(EventPluginContext context) {
            if (!context.Event.IsError())
                return;

            InnerError current = context.Event.GetError(context.Client.Configuration.Resolver.GetJsonSerializer());
            DateTime repeatWindow = DateTime.Now.AddSeconds(-2);

            while (current != null) {
                int hashCode = current.GetHashCode();

                // make sure that we don't process the same error multiple times within 2 seconds.
                if (_recentlyProcessedErrors.Any(s => s.Item1 == hashCode && s.Item2 >= repeatWindow)) {
                    context.Log.FormattedInfo(typeof(ExceptionlessClient), "Ignoring duplicate error event: hash={0}", hashCode);
                    context.Cancel = true;
                    return;
                }

                // add this exception to our list of recent errors that we have processed.
                _recentlyProcessedErrors.Enqueue(Tuple.Create(hashCode, DateTime.Now));

                // only keep the last 10 recent errors
                Tuple<int, DateTime> temp;
                while (_recentlyProcessedErrors.Count > 10)
                    _recentlyProcessedErrors.TryDequeue(out temp);

                current = current.Inner;
            }
        }
    }
}