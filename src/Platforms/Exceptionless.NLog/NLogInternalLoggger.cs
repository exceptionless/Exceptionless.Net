using System;
using Exceptionless.Logging;
using NLog.Common;

namespace Exceptionless.NLog {
    internal class NLogInternalLoggger : IExceptionlessLog {
        public LogLevel MinimumLogLevel { get; set; }

        public void Debug(string message, string source = null) {
            InternalLogger.Debug("ExceptionLess: {0} Source={1}", message, source);
        }

        public void Error(string message, string source = null, Exception exception = null) {
            if (exception is null)
                InternalLogger.Error("ExceptionLess: {0} Source={1}", message, source);
            else
                InternalLogger.Error(exception, "ExceptionLess: {0} Source={1}", message, source);
        }

        public void Info(string message, string source = null) {
            InternalLogger.Info("ExceptionLess: {0} Source={1}", message, source);
        }

        public void Trace(string message, string source = null) {
            InternalLogger.Trace("ExceptionLess: {0} Source={1}", message, source);
        }

        public void Warn(string message, string source = null) {
            InternalLogger.Warn("ExceptionLess: {0} Source={1}", message, source);
        }

        public void Flush() {
            // NLog InternalLogger has no flush
        }
    }
}
