#if NET10_0_OR_GREATER
using System;
using System.Reflection;
using Exceptionless.Dependency;
using Exceptionless.Log4net;
using Exceptionless.Submission;
using Exceptionless.Tests.Utility;
using log4net.Appender;
using log4net.Core;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class Log4netAppenderTests {
        private const string ApiKey = "LhhP1C9gijpSKCslHHCvwdSIz298twx271nTest";

        [Fact]
        public void ExceptionlessAppender_Flush_SendsQueuedEvents() {
            var appender = new ExceptionlessAppender { ApiKey = ApiKey };
            appender.ActivateOptions();
            var submissionClient = UseInMemorySubmissionClient(appender);

            appender.DoAppend(CreateLoggingEvent());

            Assert.True(appender.Flush(5000));
            Assert.Single(submissionClient.Events);
            appender.Close();
        }

        [Fact]
        public void ExceptionlessAppender_Close_SendsQueuedEvents() {
            var appender = new ExceptionlessAppender { ApiKey = ApiKey };
            appender.ActivateOptions();
            var submissionClient = UseInMemorySubmissionClient(appender);

            appender.DoAppend(CreateLoggingEvent());
            appender.Close();

            Assert.Single(submissionClient.Events);
        }

        [Fact]
        public void BufferingExceptionlessAppender_Close_SendsBufferedEvents() {
            var appender = new BufferingExceptionlessAppender { ApiKey = ApiKey, BufferSize = 10 };
            appender.ActivateOptions();
            var submissionClient = UseInMemorySubmissionClient(appender);

            appender.DoAppend(CreateLoggingEvent("first"));
            appender.DoAppend(CreateLoggingEvent("second"));
            Assert.Empty(submissionClient.Events);

            appender.Close();

            Assert.Equal(2, submissionClient.Events.Count);
        }

        private static InMemorySubmissionClient UseInMemorySubmissionClient(IAppender appender) {
            var field = appender.GetType().GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            var client = (ExceptionlessClient)field.GetValue(appender)!;
            Assert.NotSame(ExceptionlessClient.Default, client);

            var submissionClient = new InMemorySubmissionClient();
            client.Configuration.Resolver.Register<ISubmissionClient>(submissionClient);
            client.Configuration.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
            return submissionClient;
        }

        private static LoggingEvent CreateLoggingEvent(string message = "test message") {
            return new LoggingEvent(typeof(Log4netAppenderTests), null, "Test", Level.Error, message, null);
        }
    }
}
#endif
