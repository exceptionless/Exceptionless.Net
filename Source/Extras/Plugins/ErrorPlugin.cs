using System;
using Exceptionless.Extras;
using Exceptionless.Models;

namespace Exceptionless.Plugins {
    [Priority(30)]
    public class ErrorPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            var exception = context.ContextData.GetException();
            if (exception == null)
                return;

            context.Event.Type = Event.KnownTypes.Error;
            context.Event.Data[Event.KnownDataKeys.Error] = exception.ToErrorModel(context.Log);
        }
    }
}