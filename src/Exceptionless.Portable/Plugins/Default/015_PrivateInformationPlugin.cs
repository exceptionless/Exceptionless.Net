#if NET45
using System;
using Exceptionless.Dependency;

namespace Exceptionless.Plugins {
    [Priority(15)]
    public class SetEnvironmentUserPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (!context.Client.Configuration.IncludePrivateInformation)
                return;

            var user = context.Event.GetUserIdentity(context.Client.Configuration.Resolver.GetJsonSerializer());
            if (user == null)
                context.Event.SetUserIdentity(Environment.UserName);
        }
    }
}
#endif