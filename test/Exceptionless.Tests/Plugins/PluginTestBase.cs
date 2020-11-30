using System;
using System.Collections;
using Exceptionless.Logging;
using Exceptionless.Tests.Log;
using Exceptionless.Tests.Utility;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins
{
    public abstract class PluginTestBase {
        protected readonly TestOutputWriter Writer;

        protected PluginTestBase(ITestOutputHelper output) {
            Writer = new TestOutputWriter(output);
        }

        protected ExceptionlessClient CreateClient(Action<ExceptionlessConfiguration> config = null) {
            return new ExceptionlessClient(c => {
                c.UseLogger(new XunitExceptionlessLog(Writer) {MinimumLogLevel = LogLevel.Trace});
                c.ReadFromAttributes();
                c.UserAgent = "testclient/1.0.0.0";

                // Disable updating settings.
                c.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
                
                config?.Invoke(c);
            });
        }

        protected ExceptionWithOverriddenStackTrace GetExceptionWithOverriddenStackTrace(string message = "Test") {
            try {
                throw new ExceptionWithOverriddenStackTrace(message);
            } catch (ExceptionWithOverriddenStackTrace ex) {
                return ex;
            }
        }

        protected Exception GetException(string message = "Test") {
            try {
                throw new Exception(message);
            } catch (Exception ex) {
                return ex;
            }
        }

        protected Exception GetNestedSimpleException(string message = "Test") {
            try {
                throw new Exception("nested " + message);
            } catch (Exception ex) {
                return new ApplicationException(message, ex);
            }
        }

        protected enum TestEnum {
            None = 1
        }

        protected struct TestStruct {
            public int Id { get; set; }
        }

        public class MyApplicationException : Exception {
            public MyApplicationException(string message) : base(message) {
                SetsDataProperty = Data;
            }

            public string IgnoredProperty { get; set; }

            public string RandomValue { get; set; }

            public IDictionary SetsDataProperty { get; set; }

            public override IDictionary Data { get { return SetsDataProperty; }  }
        }

        [Serializable]
        public class ExceptionWithOverriddenStackTrace : Exception {
            private readonly string _stackTrace = Environment.StackTrace;
            public ExceptionWithOverriddenStackTrace(string message) : base(message) { }
            public override string StackTrace => _stackTrace;
        }
    }
}