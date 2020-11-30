using System;
using Exceptionless.Dependency;

namespace Exceptionless.Plugins.Default {
    [Priority(16)]
    public class SetEnvironmentUserPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (!context.Client.Configuration.IncludeUserName)
                return;

            var user = context.Event.GetUserIdentity(context.Client.Configuration.Resolver.GetJsonSerializer());
            if (user == null)
                context.Event.SetUserIdentity(Environment.UserName);
        }
    }
}