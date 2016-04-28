using System;
using log4net.Appender;
using log4net.Core;

namespace Exceptionless.Log4net {
    public class ExceptionlessAppender : AppenderSkeleton {
        private ExceptionlessClient _client = ExceptionlessClient.Default;

        public string ApiKey { get; set; }
        public string ServerUrl { get; set; }

        public override void ActivateOptions() {
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
            
            _client.SubmitFromLogEvent(loggingEvent);
        }
    }
}
