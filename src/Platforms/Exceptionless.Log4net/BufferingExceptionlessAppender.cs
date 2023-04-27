using System;
using log4net.Appender;
using log4net.Core;

namespace Exceptionless.Log4net {
    public class BufferingExceptionlessAppender : BufferingAppenderSkeleton {
        private ExceptionlessClient _client = ExceptionlessClient.Default;

        public string ApiKey { get; set; }
        public string ServerUrl { get; set; }

        protected override void SendBuffer(LoggingEvent[] events) {
            foreach (var e in events)
                _client.SubmitFromLogEvent(e);
        }

        public override void ActivateOptions() {
            base.ActivateOptions();

            if (String.IsNullOrEmpty(ApiKey) && String.IsNullOrEmpty(ServerUrl))
                return;

            _client = new ExceptionlessClient(config => {
                if (!String.IsNullOrEmpty(ApiKey) && ApiKey != "API_KEY_HERE")
                    config.ApiKey = ApiKey;
                if (!String.IsNullOrEmpty(ServerUrl))
                    config.ServerUrl = ServerUrl;
                config.UseInMemoryStorage();
            });
        }

        protected override void Append(LoggingEvent loggingEvent) {
            if (!_client.Configuration.IsValid)
                return;

            var minLogLevel = _client.Configuration.Settings.GetMinLogLevel(loggingEvent.LoggerName);
            if (loggingEvent.Level.ToLogLevel() < minLogLevel)
                return;

            base.Append(loggingEvent);
        }
    }
}