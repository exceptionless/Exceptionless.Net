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
            
            var uptime = TimeSpan.FromSeconds((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);

            // Store the system uptime as an extended property.
            context.Event.SetProperty("System Uptime", $"{uptime.Days} Days {uptime.Hours} Hours {uptime.Minutes} Minutes {uptime.Seconds} Seconds");
        }
    }
}