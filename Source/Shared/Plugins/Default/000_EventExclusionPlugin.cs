using System;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(0)]
    public class EventExclusionPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            foreach (var callback in context.Client.Configuration.EventExclusions) {
                if (!callback(context.Event)) {
                    context.Cancel = true;
                    return;
                }
            }

            if (context.Event.IsLog()) {
                var minLogLevel = context.Client.Configuration.Settings.GetMinLogLevel(context.Event.Source);
                var logLevel = LogLevel.FromString(context.Event.Data.GetValueOrDefault(Event.KnownDataKeys.Level, "Trace").ToString());

                if (logLevel < minLogLevel)
                    context.Cancel = true;

                return;
            }

            if (context.Event.IsError()) {
                // TODO: Check exception type, do we need to check later in the pipeline?
                // can we even do this? Won't users expect the stacking target exception to be used and we can't get that on the client.
                return;
            }

            if (!context.Client.Configuration.Settings.GetTypeAndSourceEnabled(context.Event.Type, context.Event.Source))
                context.Cancel = true;
        }
    }
}
