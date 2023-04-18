using System;
using System.Collections.Generic;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Exceptionless.NLog {
    [Target("Exceptionless")]
    public class ExceptionlessTarget : TargetWithLayout {
        private ExceptionlessClient _client = ExceptionlessClient.Default;

        public Layout ApiKey { get; set; }
        public Layout ServerUrl { get; set; }
        public Layout UserIdentity { get; set; }
        public Layout UserIdentityName { get; set; }

        [ArrayParameter(typeof(ExceptionlessField), "field")]
        public IList<ExceptionlessField> Fields { get; set; }

        public ExceptionlessTarget() {
            Fields = new List<ExceptionlessField>();
            Layout = "${message}";
        }

        protected override void InitializeTarget() {
            base.InitializeTarget();

            var apiKey = RenderLogEvent(ApiKey, LogEventInfo.CreateNullEvent());
            var serverUrl = RenderLogEvent(ServerUrl, LogEventInfo.CreateNullEvent());

            if (!String.IsNullOrEmpty(apiKey) || !String.IsNullOrEmpty(serverUrl))
                _client = new ExceptionlessClient(config => {
                    if (!String.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE")
                        config.ApiKey = apiKey;
                    if (!String.IsNullOrEmpty(serverUrl))
                        config.ServerUrl = serverUrl;
                    config.UseInMemoryStorage();
                });
        }

        protected override void Write(LogEventInfo logEvent) {
            if (!_client.Configuration.IsValid)
                return;

            LogLevel minLogLevel = LogLevel.FromOrdinal(_client.Configuration.Settings.GetMinLogLevel(logEvent.LoggerName).Ordinal);
            if (logEvent.Level < minLogLevel)
                return;

            var formattedMessage = RenderLogEvent(Layout, logEvent);
            var builder = _client.CreateFromLogEvent(logEvent, formattedMessage);

            var userIdentity = RenderLogEvent(UserIdentity, logEvent);
            var userIdentityName = RenderLogEvent(UserIdentityName, logEvent);
            builder.Target.SetUserIdentity(userIdentity, userIdentityName);

            foreach (var field in Fields) {
                var renderedField = RenderLogEvent(field.Layout, logEvent);
                if (!String.IsNullOrWhiteSpace(renderedField))
                    builder.AddObject(renderedField, field.Name);
            }

            builder.Submit();
        }
    }
}
