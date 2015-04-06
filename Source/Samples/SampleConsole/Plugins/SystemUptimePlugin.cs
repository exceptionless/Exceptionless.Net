using System;
using System.Diagnostics;
using Exceptionless.Plugins;
using Exceptionless.Models;

namespace Exceptionless.SampleConsole.Plugins {
    [Priority(100)]
    public class SystemUptimePlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            // Only update feature usage events.
            if (context.Event.Type != Event.KnownTypes.FeatureUsage)
                return;

            // Get the system uptime
            using (var pc = new PerformanceCounter("System", "System Up Time")) {
                pc.NextValue();

                var uptime = TimeSpan.FromSeconds(pc.NextValue());

                // Store the system uptime as an extended property.
                context.Event.SetProperty("System Uptime", String.Format("{0} Days {1} Hours {2} Minutes {3} Seconds", uptime.Days, uptime.Hours, uptime.Minutes, uptime.Seconds));
            }
        }
    }
}