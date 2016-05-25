using System;
using Exceptionless.Logging;
using NLog;
using NLog.Fluent;
using LogLevel = Exceptionless.Logging.LogLevel;

namespace Exceptionless.NLog {
    public class NLogExceptionlessLog : IExceptionlessLog {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public NLogExceptionlessLog(LogLevel minimumLogLevel = null) {
            if (minimumLogLevel != null)
                MinimumLogLevel = minimumLogLevel;
        }

        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {
            if (LogLevel.Error < MinimumLogLevel)
                return;

            _logger.Error().Message(message).LoggerName(source).Exception(exception).Write();
        }

        public void Info(string message, string source = null) {
            if (LogLevel.Info < MinimumLogLevel)
                return;

            _logger.Info().Message(message).LoggerName(source).Write();
        }

        public void Debug(string message, string source = null) {
            if (LogLevel.Debug < MinimumLogLevel)
                return;

            _logger.Debug().Message(message).LoggerName(source).Write();
        }

        public void Warn(string message, string source = null) {
            if (LogLevel.Warn < MinimumLogLevel)
                return;

            _logger.Warn().Message(message).LoggerName(source).Write();
        }

        public void Trace(string message, string source = null) {
            if (LogLevel.Trace < MinimumLogLevel)
                return;

            _logger.Trace().Message(message).LoggerName(source).Write();
        }

        public void Flush() { }
    }
}
