using System;
using Exceptionless.Logging;
using log4net;

namespace Exceptionless.Log4net {
    public class Log4netExceptionlessLog : IExceptionlessLog {
        public Log4netExceptionlessLog(LogLevel? minimumLogLevel = null) {
            if (minimumLogLevel.HasValue)
                MinimumLogLevel = minimumLogLevel.Value;
        }

        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {
            if (LogLevel.Error < MinimumLogLevel)
                return;

            LogManager.GetLogger(source ?? "Exceptionless").Error(message, exception);
        }

        public void Info(string message, string source = null) {
            if (LogLevel.Info < MinimumLogLevel)
                return;

            LogManager.GetLogger(source ?? "Exceptionless").Info(message);
        }

        public void Debug(string message, string source = null) {
            if (LogLevel.Debug < MinimumLogLevel)
                return;

            LogManager.GetLogger(source ?? "Exceptionless").Debug(message);
        }

        public void Warn(string message, string source = null) {
            if (LogLevel.Warn < MinimumLogLevel)
                return;

            LogManager.GetLogger(source ?? "Exceptionless").Warn(message);
        }

        public void Trace(string message, string source = null) {
            if (LogLevel.Trace < MinimumLogLevel)
                return;

            LogManager.GetLogger(source ?? "Exceptionless").Debug(message);
        }

        public void Flush() { }
    }
}
