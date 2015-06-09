using System;
using Exceptionless.Extensions;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(20)]
    public class SimpleErrorPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            var exception = context.ContextData.GetException();
            if (exception == null)
                return;

            context.Event.Type = Event.KnownTypes.Error;
            context.Event.Data[Event.KnownDataKeys.SimpleError] = exception.ToSimpleErrorModel();
        }
    }
}