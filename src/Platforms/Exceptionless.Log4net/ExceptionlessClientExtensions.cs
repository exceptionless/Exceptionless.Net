using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless;
using Exceptionless.Logging;
using log4net.Core;

namespace Exceptionless.Log4net {
    public static class ExceptionlessClientExtensions {
        public static EventBuilder CreateFromLogEvent(this ExceptionlessClient client, LoggingEvent ev) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var builder = ev.ExceptionObject != null ? client.CreateException(ev.ExceptionObject) : client.CreateLog(ev.LoggerName, ev.RenderedMessage, ev.Level.ToLogLevel());
            builder.Target.Date = ev.TimeStamp;

            if (!String.IsNullOrWhiteSpace(ev.RenderedMessage))
                builder.SetMessage(ev.RenderedMessage);

            if (ev.ExceptionObject != null)
                builder.SetSource(ev.LoggerName);

            var props = ev.GetProperties();
            foreach (var key in props.GetKeys().Where(key => !_ignoredEventProperties.Contains(key, StringComparer.OrdinalIgnoreCase))) {
                string propName = key;
                if (propName.StartsWith("log4net:"))
                    propName = propName.Substring(8);
                builder.SetProperty(propName, props[key]);
            }

            return builder;
        }

        public static LogLevel ToLogLevel(this Level level) {
            if (level == Level.Trace || level == Level.Finest)
                return LogLevel.Trace;
            if (level == Level.Debug || level == Level.Fine)
                return LogLevel.Debug;
            if (level == Level.Info)
                return LogLevel.Info;
            if (level == Level.Warn)
                return LogLevel.Warn;
            if (level == Level.Error)
                return LogLevel.Error;
            if (level == Level.Fatal)
                return LogLevel.Fatal;
            if (level == Level.Off)
                return LogLevel.Off;

            return LogLevel.Off;
        }

        public static void SubmitFromLogEvent(this ExceptionlessClient client, LoggingEvent ev) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            CreateFromLogEvent(client, ev).Submit();
        }

        private static readonly List<string> _ignoredEventProperties = new List<string> {
            "CallerFilePath",
            "CallerMemberName",
            "CallerLineNumber"
        };
    }
}
