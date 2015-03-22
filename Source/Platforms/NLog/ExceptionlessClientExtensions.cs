using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Enrichments;
using Exceptionless.Models;
using NLog;
using NLog.Fluent;

namespace Exceptionless.NLog {
    public static class ExceptionlessClientExtensions {
        public static EventBuilder CreateFromLogEvent(this ExceptionlessClient client, LogEventInfo ev) {
            var contextData = ev.GetContextData();

            if (ev.Exception != null)
                contextData.SetException(ev.Exception);

            var builder = client.CreateEvent(contextData);
            if (ev.Exception == null) {
                builder.SetType(Event.KnownTypes.Log);
                builder.SetSource(ev.LoggerName);
                builder.SetProperty(Event.KnownDataKeys.Level, ev.Level.Name);
            } else {
                builder.SetType(Event.KnownTypes.Error);
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

        public static LogBuilder Tag(this LogBuilder builder, params string[] tags) {
            var tagList = builder.LogEventInfo.GetTags();
            tagList.AddRange(tags);

            return builder;
        }

        public static LogBuilder ContextProperty(this LogBuilder builder, string key, object value) {
            var contextData = builder.LogEventInfo.GetContextData();
            contextData[key] = value;

            return builder;
        }

        public static LogBuilder MarkUnhandled(this LogBuilder builder, string submissionMethod = null) {
            var contextData = builder.LogEventInfo.GetContextData();
            contextData.MarkAsUnhandledError();
            if (!String.IsNullOrEmpty(submissionMethod))
                contextData.SetSubmissionMethod(submissionMethod);

            return builder;
        }

        public static List<string> GetTags(this LogEventInfo ev) {
            var tagList = new List<string>();
            if (!ev.Properties.ContainsKey("Tags"))
                ev.Properties["Tags"] = tagList;

            if (ev.Properties.ContainsKey("Tags")
                && ev.Properties["Tags"] is List<string>)
                tagList = (List<string>)ev.Properties["Tags"];

            return tagList;
        }

        public static ContextData GetContextData(this LogEventInfo ev) {
            var contextData = new ContextData();
            if (!ev.Properties.ContainsKey("ContextData"))
                ev.Properties["ContextData"] = contextData;

            if (ev.Properties.ContainsKey("ContextData")
                && ev.Properties["ContextData"] is ContextData)
                contextData = (ContextData)ev.Properties["ContextData"];

            return contextData;
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
