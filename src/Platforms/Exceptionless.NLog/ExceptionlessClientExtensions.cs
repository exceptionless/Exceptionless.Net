using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Plugins;
using Exceptionless.Models;
using NLog;

namespace Exceptionless.NLog {
    public static class ExceptionlessClientExtensions {
        public static EventBuilder CreateFromLogEvent(this ExceptionlessClient client, LogEventInfo ev) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var contextData = new ContextData(ev.GetContextData());

            if (ev.Exception != null)
                contextData.SetException(ev.Exception);

            var builder = client.CreateEvent(contextData);
            builder.Target.Date = ev.TimeStamp;
            builder.SetSource(ev.LoggerName);

            var properties = ev.Properties
                .Where(kvp => !_ignoredEventProperties.Contains(kvp.Key.ToString(), StringComparer.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

            object value;
            if (properties.TryGetValue("@value", out value)) {
                try {
                    builder.SetValue(Convert.ToDecimal(value));
                    properties.Remove("@value");
                } catch (Exception) {}
            }

            object stackingKey;
            if (properties.TryGetValue(Event.KnownDataKeys.ManualStackingInfo, out stackingKey)) {
                try {
                    builder.SetManualStackingKey(stackingKey.ToString());
                    properties.Remove(Event.KnownDataKeys.ManualStackingInfo);
                } catch (Exception) { }
            }
            
            if (ev.Exception == null)
                builder.SetProperty(Event.KnownDataKeys.Level, ev.Level.Name);
            
            if (!String.IsNullOrWhiteSpace(ev.FormattedMessage))
                builder.SetMessage(ev.FormattedMessage);
            
            var tagList = ev.GetTags();
            if (tagList.Count > 0)
                builder.AddTags(tagList.ToArray());

            foreach (var p in properties)
                builder.SetProperty(p.Key, p.Value);

            return builder;
        }

        public static void SubmitFromLogEvent(this ExceptionlessClient client, LogEventInfo ev) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            CreateFromLogEvent(client, ev).Submit();
        }

        private static readonly List<string> _ignoredEventProperties = new List<string> {
            "CallerFilePath",
            "CallerMemberName",
            "CallerLineNumber",
            "Tags",
            "ContextData"
        };
    }
}
