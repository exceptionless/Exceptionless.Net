using System;

namespace Exceptionless.Logging {
    public interface IExceptionlessLog {
        LogLevel MinimumLogLevel { get; set; }
        void Error(string message, string source = null, Exception exception = null);
        void Info(string message, string source = null);
        void Debug(string message, string source = null);
        void Warn(string message, string source = null);
        void Trace(string message, string source = null);
        void Flush();
    }
}