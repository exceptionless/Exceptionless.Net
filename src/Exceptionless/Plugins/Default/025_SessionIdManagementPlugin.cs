using System;

namespace Exceptionless.Plugins.Default {
    [Priority(25)]
    public class SessionIdManagementPlugin : IEventPlugin {
        [Android.Preserve]
        public SessionIdManagementPlugin() {}

        public void Run(EventPluginContext context) {
            if (context.Event.IsSessionStart() || String.IsNullOrEmpty(context.Client.Configuration.CurrentSessionIdentifier))
                context.Client.Configuration.CurrentSessionIdentifier = Guid.NewGuid().ToString("N");

            if (context.Event.IsSessionStart())
                context.Event.ReferenceId = context.Client.Configuration.CurrentSessionIdentifier;
            else
                context.Event.SetEventReference("session", context.Client.Configuration.CurrentSessionIdentifier);
        }
    }
}