#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Reflection;
using Exceptionless.Log4net;
using Exceptionless.Models;
using Exceptionless.NLog;
using log4net.Appender;
using log4net.Core;
using NLog;
using NLog.Config;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class LoggingTargetEnvironmentTests {
        private const string ApiKey = "LhhP1C9gijpSKCslHHCvwdSIz298twx271nTest";

        [Theory]
        [InlineData(" Staging ", "Staging")]
        [InlineData("", "qa-west")]
        [InlineData("Staging West", null)]
        public void NLogTarget_Environment_OverridesClientEnvironment(string environment, string? expected) {
            // Arrange
            var target = new ExceptionlessTarget { ApiKey = ApiKey, Environment = environment };
            var config = new LoggingConfiguration();
            config.AddRuleForAllLevels(target);
            using var factory = new LogFactory { Configuration = config };
            var client = GetClient(target);
            client.Configuration.SetEnvironment("qa-west");
            var submitted = Capture(client);

            // Act
            factory.GetLogger("test").Error("environment test");

            // Assert
            var ev = Assert.Single(submitted);
            Assert.Equal(expected, ev.Environment);
        }

        [Fact]
        public void NLogTarget_EnvironmentLayout_RendersPerEvent() {
            // Arrange
            var target = new ExceptionlessTarget { ApiKey = ApiKey, Environment = "${event-properties:item=env}" };
            var config = new LoggingConfiguration();
            config.AddRuleForAllLevels(target);
            using var factory = new LogFactory { Configuration = config };
            var submitted = Capture(GetClient(target));

            // Act
            factory.GetLogger("test").WithProperty("env", "staging").Error("environment layout test");

            // Assert
            Assert.Equal("staging", Assert.Single(submitted).Environment);
        }

        [Theory]
        [InlineData(false, " Staging ", "Staging")]
        [InlineData(false, null, "qa-west")]
        [InlineData(true, " Staging ", "Staging")]
        [InlineData(true, null, "qa-west")]
        public void Log4netAppender_Environment_OverridesClientEnvironment(bool buffering, string? environment, string expected) {
            // Arrange
            AppenderSkeleton appender = buffering
                ? new BufferingExceptionlessAppender { ApiKey = ApiKey, Environment = environment, BufferSize = 0 }
                : new ExceptionlessAppender { ApiKey = ApiKey, Environment = environment };
            appender.ActivateOptions();
            var client = GetClient(appender);
            client.Configuration.SetEnvironment("qa-west");
            var submitted = Capture(client);

            // Act
            appender.DoAppend(new LoggingEvent(new LoggingEventData {
                LoggerName = "test",
                Level = Level.Error,
                Message = "environment test",
                TimeStampUtc = DateTime.UtcNow
            }));

            // Assert
            Assert.Equal(expected, Assert.Single(submitted).Environment);
        }

        private static ExceptionlessClient GetClient(object target) {
            var field = target.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (ExceptionlessClient)field.GetValue(target)!;
        }

        private static List<Event> Capture(ExceptionlessClient client) {
            var submitted = new List<Event>();
            client.SubmittingEvent += (sender, args) => {
                submitted.Add(args.Event);
                args.Cancel = true;
            };
            return submitted;
        }
    }
}
#endif
