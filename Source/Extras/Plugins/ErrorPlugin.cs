using System;
using Exceptionless.Extras;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.Plugins {
    [Priority(40)]
    public class ErrorPlugin : IEventPlugin {
        private readonly IExceptionlessLog _log;

        public ErrorPlugin(IExceptionlessLog log) {
            _log = log;
        }

        public void Run(EventPluginContext context) {
            var exception = context.ContextData.GetException();
            if (exception == null)
                return;

            context.Event.Type = Event.KnownTypes.Error;
            context.Event.Data[Event.KnownDataKeys.Error] = exception.ToErrorModel(_log);
        }
    }
}