using System;
using Exceptionless.Dependency;
using Exceptionless.Extensions;

namespace Exceptionless.Plugins.Default {
    [Priority(110)]
    public class IgnoreUserAgentPlugin : IEventPlugin {
        [Android.Preserve]
        public IgnoreUserAgentPlugin() {}

        public void Run(EventPluginContext context) {
            var request = context.Event.GetRequestInfo(context.Client.Configuration.Resolver.GetJsonSerializer());
            if (request == null)
                return;

            if (request.UserAgent.AnyWildcardMatches(context.Client.Configuration.UserAgentBotPatterns, true)) {
                context.Log.Info(String.Concat("Cancelling event as the request infos user agent matches a known bot pattern: ", request.UserAgent), typeof(IgnoreUserAgentPlugin).Name);
                context.Cancel = true;
            }
        }
    }
}