using System;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(50)]
    public class EnvironmentInfoPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            //TODO: This needs to be uncommented when the client is sending session start and end.
            if (context.Event.Data.ContainsKey(Event.KnownDataKeys.EnvironmentInfo)) // || context.Event.Type != Event.KnownTypes.SessionStart)
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