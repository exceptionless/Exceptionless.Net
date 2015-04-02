using System;
using System.Diagnostics;
using Exceptionless.Plugins;
using Exceptionless.Models;

namespace Exceptionless.SampleConsole.Plugins {
    public class SystemUptimePlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            // Only update feature usage events.
            if (context.Event.Type != Event.KnownTypes.FeatureUsage)
                return;

            // Get the system uptime
            using (var uptime = new PerformanceCounter("System", "System Up Time")) {
                uptime.NextValue();

                // Store the system uptime as an extended property.
                context.Event.SetProperty("System Uptime", TimeSpan.FromSeconds(uptime.NextValue()).ToString("g"));
            }
        }
    }
}