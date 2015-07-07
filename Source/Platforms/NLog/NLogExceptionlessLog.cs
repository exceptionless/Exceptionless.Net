using System;
using Exceptionless.Logging;
using NLog.Fluent;

namespace Exceptionless.NLog {
    public class NLogExceptionlessLog : IExceptionlessLog {
        public NLogExceptionlessLog(LogLevel? minimumLogLevel = null) {
            if (minimumLogLevel.HasValue)
                MinimumLogLevel = minimumLogLevel.Value;
        }

        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {
            if (LogLevel.Error < MinimumLogLevel)
                return;

            Log.Error().Message(message).LoggerName(source).Exception(exception).Write();
        }

        public void Info(string message, string source = null) {
            if (LogLevel.Info < MinimumLogLevel)
                return;

            Log.Info().Message(message).LoggerName(source).Write();
        }

        public void Debug(string message, string source = null) {
            if (LogLevel.Debug < MinimumLogLevel)
                return;

            Log.Debug().Message(message).LoggerName(source).Write();
        }

        public void Warn(string message, string source = null) {
            if (LogLevel.Warn < MinimumLogLevel)
                return;

            Log.Warn().Message(message).LoggerName(source).Write();
        }

        public void Trace(string message, string source = null) {
            if (LogLevel.Trace < MinimumLogLevel)
                return;

            Log.Trace().Message(message).LoggerName(source).Write();
        }

        public void Flush() { }
    }
}
