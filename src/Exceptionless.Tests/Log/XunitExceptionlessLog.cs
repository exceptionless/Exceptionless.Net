using System;
using Exceptionless.Logging;
using Exceptionless.Tests.Utility;

namespace Exceptionless.Tests.Log {

    public class XunitExceptionlessLog : IExceptionlessLog {
        private readonly TestOutputWriter _writer;

        public XunitExceptionlessLog(TestOutputWriter writer) {
            _writer = writer;
        }

        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {
            if (LogLevel.Error >= MinimumLogLevel)
                _writer.WriteLine(message);
        }

        public void Info(string message, string source = null) {
            if (LogLevel.Info >= MinimumLogLevel)
                _writer.WriteLine(message);
        }

        public void Debug(string message, string source = null) {
            if (LogLevel.Debug >= MinimumLogLevel)
                _writer.WriteLine(message);
        }

        public void Warn(string message, string source = null) {
            if (LogLevel.Warn >= MinimumLogLevel)
                _writer.WriteLine(message);
        }

        public void Trace(string message, string source = null) {
            if (LogLevel.Trace >= MinimumLogLevel)
                _writer.WriteLine(message);
        }

        public void Flush() { }
    }
}
