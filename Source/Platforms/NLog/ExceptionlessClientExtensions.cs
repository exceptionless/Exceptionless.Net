using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Plugins;
using Exceptionless.Models;
using NLog;

namespace Exceptionless.NLog {
    public static class ExceptionlessClientExtensions {
        public static EventBuilder CreateFromLogEvent(this ExceptionlessClient client, LogEventInfo ev) {
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
            if (properties.TryGetValue("Value", out value)) {
                try {
                    builder.SetValue(Convert.ToDecimal(value));
                    properties.Remove("Value");
                } catch (Exception) {}
            }

            object stackingKey;
            if (properties.TryGetValue("StackingKey", out stackingKey)) {
                try {
                    builder.SetManualStackingKey(stackingKey.ToString());
                    properties.Remove("StackingKey");
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
