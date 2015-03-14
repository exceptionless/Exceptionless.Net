using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Core;

namespace Exceptionless.NLog {
    public static class ExceptionlessClientExtensions {
        public static EventBuilder CreateFromLogEvent(this ExceptionlessClient client, LoggingEvent ev) {
            var builder = ev.ExceptionObject != null ? client.CreateException(ev.ExceptionObject) : client.CreateLog(ev.LoggerName, ev.RenderedMessage, ev.Level.Name);
            builder.Target.Date = ev.TimeStamp;

            if (!String.IsNullOrWhiteSpace(ev.RenderedMessage))
                builder.SetMessage(ev.RenderedMessage);

            if (ev.ExceptionObject != null)
                builder.SetSource(ev.LoggerName);

            var props = ev.GetProperties();
            foreach (var key in props.GetKeys().Where(key => !_ignoredEventProperties.Contains(key, StringComparer.OrdinalIgnoreCase)))
                builder.SetProperty(key, props[key]);

            return builder;
        }

        public static void SubmitFromLogEvent(this ExceptionlessClient client, LoggingEvent ev) {
            CreateFromLogEvent(client, ev).Submit();
        }

        private static readonly List<string> _ignoredEventProperties = new List<string> {
            "CallerFilePath",
            "CallerMemberName",
            "CallerLineNumber"
        };
    }
}
