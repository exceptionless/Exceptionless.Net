using System;

namespace Exceptionless.Logging {
    public class DebugExceptionlessLog : IExceptionlessLog {
        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {
            if (LogLevel.Error >= MinimumLogLevel)
                System.Diagnostics.Debug.WriteLine(message);
        }

        public void Info(string message, string source = null) {
            if (LogLevel.Info >= MinimumLogLevel)
                System.Diagnostics.Debug.WriteLine(message);
        }

        public void Debug(string message, string source = null) {
            if (LogLevel.Debug >= MinimumLogLevel)
                System.Diagnostics.Debug.WriteLine(message);
        }

        public void Warn(string message, string source = null) {
            if (LogLevel.Warn >= MinimumLogLevel)
                System.Diagnostics.Debug.WriteLine(message);
        }

        public void Trace(string message, string source = null) {
            if (LogLevel.Trace >= MinimumLogLevel)
                System.Diagnostics.Debug.WriteLine(message);
        }

        public void Flush() { }
    }
}