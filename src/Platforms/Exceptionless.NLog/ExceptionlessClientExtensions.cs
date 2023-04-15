using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Plugins;
using Exceptionless.Models;
using NLog;

namespace Exceptionless.NLog {
    public static class ExceptionlessClientExtensions {
        public static EventBuilder CreateFromLogEvent(this ExceptionlessClient client, LogEventInfo ev) {
            return CreateFromLogEvent(client, ev, ev.FormattedMessage);
        }

        public static EventBuilder CreateFromLogEvent(this ExceptionlessClient client, LogEventInfo ev, string formattedMessage) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var data = ev.GetContextData();
            var contextData = data != null ? new ContextData(data) : new ContextData();
            if (ev.Exception != null)
                contextData.SetException(ev.Exception);

            var builder = client.CreateEvent(contextData);
            builder.Target.Date = ev.TimeStamp;
            builder.SetSource(ev.LoggerName);

            if (ev.HasProperties) {
                foreach (var property in ev.Properties) {
                    string propertyKey = property.Key.ToString();
                    if (_ignoredEventProperties.Contains(propertyKey))
                        continue;

                    if (propertyKey.Equals("@value", StringComparison.OrdinalIgnoreCase)) {
                        try {
                            builder.SetValue(Convert.ToDecimal(property.Value));
                        } catch (Exception) { }

                        continue;
                    }

                    if (propertyKey.Equals(Event.KnownDataKeys.ManualStackingInfo, StringComparison.OrdinalIgnoreCase)) {
                        try {
                            builder.SetManualStackingKey(property.Value.ToString());
                        } catch (Exception) { }

                        continue;
                    }

                    builder.SetProperty(propertyKey, property.Value);
                }
            }

            if (ev.Exception == null) {
                builder.SetType(Event.KnownTypes.Log);
                builder.SetProperty(Event.KnownDataKeys.Level, ev.Level.Name);
            } else {
                builder.SetType(Event.KnownTypes.Error);
            }

            if (!String.IsNullOrWhiteSpace(formattedMessage))
                builder.SetMessage(formattedMessage);

            var tags = ev.GetTags();
            if (tags != null)
                builder.AddTags(tags.ToArray());

            return builder;
        }

        public static void SubmitFromLogEvent(this ExceptionlessClient client, LogEventInfo ev) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            CreateFromLogEvent(client, ev).Submit();
        }

        private static readonly HashSet<string> _ignoredEventProperties = new HashSet<string> {
            "Tags",
            "ContextData"
        };
    }
}
