using System;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(50)]
    public class EnvironmentInfoPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (context.Event.Data.ContainsKey(Event.KnownDataKeys.EnvironmentInfo))
                return;

            if (!String.IsNullOrEmpty(context.Event.SessionId) && context.Event.IsSessionEnd() || context.Event.IsSessionHeartbeat())
                return;

            try {
                var collector = context.Resolver.GetEnvironmentInfoCollector();
                if (collector == null)
                    return;

                var info = collector.GetEnvironmentInfo();
                info.InstallId = context.Client.Configuration.GetInstallId();
                context.Event.Data[Event.KnownDataKeys.EnvironmentInfo] = info;
            } catch (Exception ex) {
                context.Log.FormattedError(typeof(EnvironmentInfoPlugin), ex, "Error adding environment information: {0}", ex.Message);
            }
        }
    }
}