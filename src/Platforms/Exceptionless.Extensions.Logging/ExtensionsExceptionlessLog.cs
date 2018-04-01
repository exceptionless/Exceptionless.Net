using System;
using Exceptionless.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Exceptionless.Logging.LogLevel;

namespace Exceptionless.Extensions.Logging {
    public class ExtensionsExceptionlessLog : IExceptionlessLog {
        private readonly ILoggerFactory _loggerFactory;

        public ExtensionsExceptionlessLog(ILoggerFactory loggerFactory, LogLevel minimumLogLevel = null) {
            _loggerFactory = loggerFactory;
            if (minimumLogLevel != null)
                MinimumLogLevel = minimumLogLevel;
        }

        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {
            if (LogLevel.Error < MinimumLogLevel)
                return;

            GetLogger(source).LogError(exception, message);
        }

        public void Info(string message, string source = null) {
            if (LogLevel.Info < MinimumLogLevel)
                return;

            GetLogger(source).LogInformation(message);
        }

        public void Debug(string message, string source = null) {
            if (LogLevel.Debug < MinimumLogLevel)
                return;

            GetLogger(source).LogDebug(message);
        }

        public void Warn(string message, string source = null) {
            if (LogLevel.Warn < MinimumLogLevel)
                return;

            GetLogger(source).LogWarning(message);
        }

        public void Trace(string message, string source = null) {
            if (LogLevel.Trace < MinimumLogLevel)
                return;

            GetLogger(source).LogDebug(message);
        }

        private ILogger GetLogger(string source) {
            return _loggerFactory.CreateLogger(source ?? "Exceptionless");
        }

        public void Flush() { }
    }
}