using System;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(10)]
    public sealed class EventExclusionPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            foreach (var callback in context.Client.Configuration.EventExclusions) {
                try {
                    if (!callback(context.Event)) {
                        context.Log.Info("Cancelling event from custom event exclusion.");
                        context.Cancel = true;
                        return;
                    }
                } catch (Exception ex) {
                    context.Log.Error(ex, "Error running custom event exclusion: " + ex.Message);
                }
            }

            if (context.Event.IsLog()) {
                var minLogLevel = context.Client.Configuration.Settings.GetMinLogLevel(context.Event.Source);
                var logLevel = LogLevel.FromString(context.Event.Data.GetValueOrDefault(Event.KnownDataKeys.Level, "Other").ToString());

                if (logLevel != LogLevel.Other && (logLevel == LogLevel.Off || logLevel < minLogLevel)) {
                    context.Log.Info("Cancelling log event due to minimum log level.");
                    context.Cancel = true;
                }

                return;
            }

            if (!context.Client.Configuration.Settings.GetTypeAndSourceEnabled(context.Event.Type, context.Event.Source)) {
                context.Log.Info(String.Format("Cancelling event from excluded type: {0} and source: {1}", context.Event.Type, context.Event.Source));
                context.Cancel = true;
                return;
            }

            try {
                var exception = context.ContextData.GetException();
                while (exception != null) {
                    if (!context.Client.Configuration.Settings.GetTypeAndSourceEnabled(Event.KnownTypes.Error, exception.GetType().FullName)) {
                        context.Log.Info("Cancelling error from excluded exception type: " + exception.GetType().FullName);
                        context.Cancel = true;
                        return;
                    }

                    exception = exception.InnerException;
                }
            } catch (Exception ex) {
                context.Log.Error(ex, "Error checking excluded exception types: " + ex.Message);
            }
        }
    }
}
