using System;

namespace Exceptionless.Logging {
    public class NullExceptionlessLog : IExceptionlessLog {
        [Android.Preserve]
        public NullExceptionlessLog() {}

        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {}

        public void Info(string message, string source = null) {}

        public void Debug(string message, string source = null) {}

        public void Warn(string message, string source = null) {}

        public void Trace(string message, string source = null) {}

        public void Flush() {}
    }
}