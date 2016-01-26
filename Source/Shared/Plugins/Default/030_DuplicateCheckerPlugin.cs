using System;
using System.Collections.Concurrent;
using System.Linq;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models.Data;

namespace Exceptionless.Plugins.Default {
    [Priority(30)]
    public class DuplicateCheckerPlugin : IEventPlugin {
        private readonly ConcurrentQueue<Tuple<int, DateTime>> _recentlyProcessedErrors = new ConcurrentQueue<Tuple<int, DateTime>>();

        public void Run(EventPluginContext context) {
            if (!context.Event.IsError())
                return;

            Exception exception = context.ContextData.GetException().GetInnermostException();
            DateTime repeatWindow = DateTime.Now.AddSeconds(-2);

            int hashCode = CalculateHashCode(0, exception); ;
            
            // make sure that we don't process the same error multiple times within 2 seconds.
            if (_recentlyProcessedErrors.Any(s => s.Item1 == hashCode && s.Item2 >= repeatWindow))
            {
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
        }

        private static int CalculateHashCode(int hashCode, Exception exception) {
            if (exception == null)
                return hashCode;

            unchecked {
                // special handling of AggregateException, they have multiple inner exceptions
                var aggregate = exception as AggregateException;
                if (aggregate != null) {
                    foreach (var innerException in aggregate.Flatten().InnerExceptions) {
                        hashCode = CalculateHashCode(hashCode, innerException);
                    }
                } else {
                    hashCode = exception.GetType().GetHashCode();

                    if (exception.Message != null)
                        hashCode = (hashCode * 397) ^ exception.Message.GetHashCode();

                    if (exception.StackTrace != null)
                        hashCode = (hashCode * 397) ^ exception.StackTrace.GetHashCode();

                    hashCode = CalculateHashCode(hashCode, exception.InnerException);
                }
            }
            return hashCode;
        }
    }
}