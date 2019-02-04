#if NET45 || NETSTANDARD2_0
using System;
using Exceptionless.Dependency;

namespace Exceptionless.Plugins.Default {
    [Priority(15)]
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
#endif