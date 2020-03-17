using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Exceptionless.Diagnostics;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Collections;

namespace Exceptionless.Plugins.Default {
    [Priority(70)]
    public class TraceLogPlugin : IEventPlugin, IDisposable {
        public const string MaxEntriesToIncludeKey = "TraceLogLimit";
        public const int DefaultMaxEntriesToInclude = 25;

        private readonly ExceptionlessConfiguration _configuration;
        private readonly ExceptionlessTraceListener _listener;

        public TraceLogPlugin(ExceptionlessConfiguration config, ExceptionlessTraceListener listener = null) {
            _configuration = config;
            _configuration.Settings.Changed += OnSettingsChanged;
            _listener = listener ?? Trace.Listeners.OfType<ExceptionlessTraceListener>().FirstOrDefault();
        }

        public void Run(EventPluginContext context) {
            try {
                int maxEntriesToInclude = context.Client.Configuration.Settings.GetInt32(MaxEntriesToIncludeKey, DefaultMaxEntriesToInclude);
                if (maxEntriesToInclude > 0)
                    AddRecentTraceLogEntries(context.Event, _listener, maxEntriesToInclude);
            } catch (Exception ex) {
                context.Log.FormattedError(typeof(TraceLogPlugin), ex, "Error adding trace information: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Adds the trace info as extended data to the event.
        /// </summary>
        /// <param name="ev">The event model.</param>
        /// <param name="listener">The listener.</param>
        /// <param name="maxEntriesToInclude"></param>
        public static void AddRecentTraceLogEntries(Event ev, ExceptionlessTraceListener listener = null, int maxEntriesToInclude = DefaultMaxEntriesToInclude) {
            if (ev.Data.ContainsKey(Event.KnownDataKeys.TraceLog))
                return;

            listener = listener ?? Trace.Listeners.OfType<ExceptionlessTraceListener>().FirstOrDefault();
            if (listener == null)
                return;

            List<string> logEntries = listener.GetLogEntries(maxEntriesToInclude);
            if (logEntries.Count > 0)
                ev.Data.Add(Event.KnownDataKeys.TraceLog, logEntries);
        }

        public void Dispose() {
            if (_configuration != null && _configuration.Settings != null)
                _configuration.Settings.Changed -= OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, ChangedEventArgs<KeyValuePair<string, string>> e) {
            if (_listener == null || !String.Equals(e.Item.Key, MaxEntriesToIncludeKey, StringComparison.OrdinalIgnoreCase))
                return;

            _listener.MaxEntriesToStore = _configuration.Settings.GetInt32(MaxEntriesToIncludeKey, 0);
        }
    }
}