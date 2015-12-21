using System;

namespace Exceptionless.Plugins {
    [Priority(15)]
    public class PrivateInformationPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (!context.Client.Configuration.IncludePrivateInformation)
                return;

            var user = context.Event.GetUserIdentity();
            if (user == null)
                context.Event.SetUserIdentity(Environment.UserName);
        }
    }
}