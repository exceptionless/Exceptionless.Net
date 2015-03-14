using System;
using Exceptionless.Logging;
using log4net;

namespace Exceptionless.NLog {
    public class Log4netExceptionlessLog : IExceptionlessLog {
        // ignore and let NLog determine what should be captured.
        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {
            LogManager.GetLogger(source ?? "Exceptionless").Error(message, exception);
        }

        public void Info(string message, string source = null) {
            LogManager.GetLogger(source ?? "Exceptionless").Info(message);
        }

        public void Debug(string message, string source = null) {
            LogManager.GetLogger(source ?? "Exceptionless").Debug(message);
        }

        public void Warn(string message, string source = null) {
            LogManager.GetLogger(source ?? "Exceptionless").Warn(message);
        }

        public void Trace(string message, string source = null) {
            LogManager.GetLogger(source ?? "Exceptionless").Debug(message);
        }

        public void Flush() { }
    }
}
