using System;
using Exceptionless.Extensions;

namespace Exceptionless.Plugins {
    [Priority(110)]
    public class IgnoreUserAgentPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            var request = context.Event.GetRequestInfo();
            if (request == null)
                return;

            if (request.UserAgent.AnyWildcardMatches(context.Client.Configuration.UserAgentBotPatterns, true)) {
                context.Log.Info(String.Concat("Cancelling event as the request infos user agent matches a known bot pattern: ", request.UserAgent), typeof(IgnoreUserAgentPlugin).Name);
                context.Cancel = true;
            }
        }
    }
}