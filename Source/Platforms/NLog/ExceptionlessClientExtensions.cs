using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Enrichments;
using Exceptionless.Models;
using NLog;

namespace Exceptionless.NLog {
    public static class ExceptionlessClientExtensions {
        public static EventBuilder CreateFromLogEvent(this ExceptionlessClient client, LogEventInfo ev) {
            var contextData = new ContextData(ev.GetContextData());

            if (ev.Exception != null)
                contextData.SetException(ev.Exception);

            var builder = client.CreateEvent(contextData);
            if (ev.Exception == null) {
                builder.SetSource(ev.LoggerName);
                builder.SetProperty(Event.KnownDataKeys.Level, ev.Level.Name);
            }

            builder.Target.Date = ev.TimeStamp;

            if (!String.IsNullOrWhiteSpace(ev.FormattedMessage))
                builder.SetMessage(ev.FormattedMessage);

            if (ev.Exception != null)
                builder.SetSource(ev.LoggerName);

            var tagList = ev.GetTags();
            if (tagList.Count > 0)
                builder.AddTags(tagList.ToArray());

            foreach (var p in ev.Properties.Where(kvp => !_ignoredEventProperties.Contains(kvp.Key.ToString(), StringComparer.OrdinalIgnoreCase)))
                builder.SetProperty(p.Key.ToString(), p.Value);

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
