using System;

namespace Exceptionless.Plugins {
    [Priority(25)]
    public class SessionIdManagementPlugin : IEventPlugin {
        private string _sessionId;
        public void Run(EventPluginContext context) {
            if (context.Event.IsSessionStart() || String.IsNullOrEmpty(_sessionId))
                _sessionId = Guid.NewGuid().ToString("N");

            if (context.Event.IsSessionStart())
                context.Event.ReferenceId = _sessionId;
            else
                context.Event.SetEventReference("session", _sessionId);

            if (context.Event.IsSessionEnd())
                _sessionId = null;
        }
    }
}