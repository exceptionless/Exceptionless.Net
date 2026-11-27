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
                var child = serializer.Deserialize(serializer.Serialize(context.Event), typeof(Event)) as Event;
                if (child != null)
                    child.HasEnvironmentOverride = context.Event.HasEnvironmentOverride;
                context.Client.SubmitEvent(child, ctx);
            }

            context.Cancel = true;
        }
    }
}