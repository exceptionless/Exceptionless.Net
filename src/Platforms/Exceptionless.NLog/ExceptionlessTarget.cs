using System;
using System.Collections.Generic;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;

namespace Exceptionless.NLog {
    [Target("Exceptionless")]
    public class ExceptionlessTarget : TargetWithLayout {
        private ExceptionlessClient _client = ExceptionlessClient.Default;

        public string ApiKey { get; set; }
        public string ServerUrl { get; set; }

        [ArrayParameter(typeof(ExceptionlessField), "field")]
        public IList<ExceptionlessField> Fields { get; private set; }

        public ExceptionlessTarget() {
            Fields = new List<ExceptionlessField>();
        }

        protected override void InitializeTarget() {
            base.InitializeTarget();

            if (!String.IsNullOrEmpty(ApiKey) || !String.IsNullOrEmpty(ServerUrl))
                _client = new ExceptionlessClient(config => {
                    if (!String.IsNullOrEmpty(ApiKey) && ApiKey != "API_KEY_HERE")
                        config.ApiKey = ApiKey;
                    if (!String.IsNullOrEmpty(ServerUrl))
                        config.ServerUrl = ServerUrl;
                    config.UseInMemoryStorage();
                });
        }

        protected override void Write(AsyncLogEventInfo info) {
            if (!_client.Configuration.IsValid)
                return;

            LogLevel minLogLevel = LogLevel.FromOrdinal(_client.Configuration.Settings.GetMinLogLevel(info.LogEvent.LoggerName).Ordinal);
            if (info.LogEvent.Level < minLogLevel)
                return;

            var builder = _client.CreateFromLogEvent(info.LogEvent);
            foreach (var field in Fields) {
                var renderedField = field.Layout.Render(info.LogEvent);
                if (!String.IsNullOrWhiteSpace(renderedField))
                    builder.AddObject(renderedField, field.Name);
            }

            builder.Submit();
        }
    }
}
