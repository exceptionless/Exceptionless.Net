using System;

namespace Exceptionless.Plugins.Default {
    [Priority(1000)]
    public class CancelSessionsWithNoUserPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (!context.Event.IsSessionStart() && !context.Event.IsSessionEnd() && !context.Event.IsSessionHeartbeat())
                return;

            var user = context.Event.GetUserIdentity();
            if (user != null && !String.IsNullOrEmpty(user.Identity))
                return;

            context.Log.Info("Cancelling session event as no user identity was specified.");
            context.Cancel = true;
        }
    }
}