using System;
using Exceptionless.Dependency;

namespace Exceptionless.Plugins.Default {
    [Priority(900)]
    public class CancelSessionsWithNoUserPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (!context.Event.IsSessionStart())
                return;

            var user = context.Event.GetUserIdentity(context.Client.Configuration.Resolver.GetJsonSerializer());
            if (user != null && !String.IsNullOrEmpty(user.Identity))
                return;

            context.Log.Info("Cancelling session event as no user identity was specified.");
            context.Cancel = true;
        }
    }
}