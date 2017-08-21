using System;
using System.Reflection;
using Exceptionless.Logging;
using log4net;

namespace Exceptionless.Log4net {
    public class Log4netExceptionlessLog : IExceptionlessLog {
        public Log4netExceptionlessLog(LogLevel minimumLogLevel = null) {
            if (minimumLogLevel != null)
                MinimumLogLevel = minimumLogLevel;
        }

        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {
            if (LogLevel.Error < MinimumLogLevel)
                return;

            GetLogger(source).Error(message, exception);
        }

        public void Info(string message, string source = null) {
            if (LogLevel.Info < MinimumLogLevel)
                return;

            GetLogger(source).Info(message);
        }

        public void Debug(string message, string source = null) {
            if (LogLevel.Debug < MinimumLogLevel)
                return;

            GetLogger(source).Debug(message);
        }

        public void Warn(string message, string source = null) {
            if (LogLevel.Warn < MinimumLogLevel)
                return;

            GetLogger(source).Warn(message);
        }

        public void Trace(string message, string source = null) {
            if (LogLevel.Trace < MinimumLogLevel)
                return;

            GetLogger(source).Debug(message);
        }

        private ILog GetLogger(string name) {
#if NETSTANDARD2_0
            return LogManager.GetLogger(typeof(Log4netExceptionlessLog).GetTypeInfo().Assembly, name ?? "Exceptionless");
#else
            return LogManager.GetLogger(name ?? "Exceptionless");
#endif
        }

        public void Flush() { }
    }
}
