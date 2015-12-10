using System;

namespace Exceptionless.Plugins {
    [Priority(60)]
    public class PrivateInformationPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (!context.Client.Configuration.IncludePrivateInformation)
                return;

            var user = context.Event.GetUserIdentity();
            if (String.IsNullOrEmpty(user?.Identity))
                context.Event.SetUserIdentity(Environment.UserName);
        }
    }
}