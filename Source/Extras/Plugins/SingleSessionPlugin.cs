using System;

namespace Exceptionless.Plugins {
    [Priority(90)]
    public class SingleSessionPlugin : IEventPlugin {
        private readonly string _sessionId = Guid.NewGuid().ToString("N").Substring(0, 16);

        public void Run(EventPluginContext context) {
            if (String.IsNullOrEmpty(context.Event.SessionId))
                context.Event.SessionId = _sessionId;
        }
    }
}