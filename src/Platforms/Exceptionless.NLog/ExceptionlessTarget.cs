﻿using System;
using System.Collections.Generic;
using Exceptionless.Dependency;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Exceptionless.NLog {
    [Target("Exceptionless")]
    public sealed class ExceptionlessTarget : TargetWithLayout {
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

            string apiKey = RenderLogEvent(ApiKey, LogEventInfo.CreateNullEvent());
            string serverUrl = RenderLogEvent(ServerUrl, LogEventInfo.CreateNullEvent());

            if (!String.IsNullOrEmpty(apiKey) || !String.IsNullOrEmpty(serverUrl)) {
                _client = new ExceptionlessClient(config => {
                    if (!String.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE")
                        config.ApiKey = apiKey;
                    if (!String.IsNullOrEmpty(serverUrl))
                        config.ServerUrl = serverUrl;

                    config.UseLogger(new NLogInternalLoggger());
                    config.UseInMemoryStorage();

                    // Rely on Logging Rules
                    config.SetDefaultMinLogLevel(Logging.LogLevel.Trace);
                });
            } else {
                // Rely on Logging Rules
                _client.Configuration.SetDefaultMinLogLevel(Logging.LogLevel.Trace);

                if (_client.Configuration.Resolver.HasDefaultRegistration<Logging.IExceptionlessLog, Logging.NullExceptionlessLog>()) {
                    _client.Configuration.UseLogger(new NLogInternalLoggger());
                }
            }
        }

        protected override void Write(LogEventInfo logEvent) {
            if (!_client.Configuration.IsValid)
                return;

            var minLogLevel = LogLevel.FromOrdinal(_client.Configuration.Settings.GetMinLogLevel(logEvent.LoggerName).Ordinal);
            if (logEvent.Level < minLogLevel)
                return;

            string formattedMessage = RenderLogEvent(Layout, logEvent);
            var builder = _client.CreateFromLogEvent(logEvent, formattedMessage);

            string userIdentity = RenderLogEvent(UserIdentity, logEvent);
            string userIdentityName = RenderLogEvent(UserIdentityName, logEvent);
            builder.Target.SetUserIdentity(userIdentity, userIdentityName);

            foreach (var field in Fields) {
                string renderedField = RenderLogEvent(field.Layout, logEvent);
                if (!String.IsNullOrWhiteSpace(renderedField))
                    builder.AddObject(renderedField, field.Name);
            }

            builder.Submit();
        }

        protected override void FlushAsync(AsyncContinuation asyncContinuation) {
            _client.ProcessQueueAsync().ContinueWith(t => asyncContinuation(t.Exception));
        }
    }
}
