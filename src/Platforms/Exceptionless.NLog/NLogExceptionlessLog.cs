using System;
using Exceptionless.Logging;
using NLog;
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
            
            _logger.ForErrorEvent().Message(message).LoggerName(source).Exception(exception).Log(typeof(NLogExceptionlessLog));
        }

        public void Info(string message, string source = null) {
            if (LogLevel.Info < MinimumLogLevel)
                return;
            
            _logger.ForInfoEvent().Message(message).LoggerName(source).Log(typeof(NLogExceptionlessLog));
        }

        public void Debug(string message, string source = null) {
            if (LogLevel.Debug < MinimumLogLevel)
                return;
            
            _logger.ForDebugEvent().Message(message).LoggerName(source).Log(typeof(NLogExceptionlessLog));
        }

        public void Warn(string message, string source = null) {
            if (LogLevel.Warn < MinimumLogLevel)
                return;
            
            _logger.ForWarnEvent().Message(message).LoggerName(source).Log(typeof(NLogExceptionlessLog));
        }

        public void Trace(string message, string source = null) {
            if (LogLevel.Trace < MinimumLogLevel)
                return;
            
            _logger.ForTraceEvent().Message(message).LoggerName(source).Log(typeof(NLogExceptionlessLog));
        }

        public void Flush() { }
    }
}
