using System;
using Exceptionless.Dependency;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(5)]
    public class HandleAggregateExceptionsPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            var aggregateException = context.ContextData.GetException() as AggregateException;
            if (aggregateException == null)
                return;

            var exception = aggregateException.Flatten();
            if (exception.InnerExceptions.Count == 1) {
                context.ContextData.SetException(exception.InnerException);
                return;
            }

            foreach (var ex in exception.InnerExceptions) {
                var ctx = new ContextData(context.ContextData);
                ctx.SetException(ex);

                var serializer = context.Resolver.GetJsonSerializer();
                context.Client.SubmitEvent(serializer.Deserialize(serializer.Serialize(context.Event), typeof(Event)) as Event, ctx);
            }

            context.Cancel = true;
        }
    }
}