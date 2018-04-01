using System;
using Exceptionless.Tests.Log;
using Exceptionless.Tests.Utility;
using Xunit;
using Xunit.Abstractions;
using LogLevel = Exceptionless.Logging.LogLevel;

namespace Exceptionless.Tests {
    public class EventBuilderTests {
        private readonly TestOutputWriter _writer;
        public EventBuilderTests(ITestOutputHelper output) {
            _writer = new TestOutputWriter(output);
        }

        private ExceptionlessClient CreateClient() {
            return new ExceptionlessClient(c => {
                c.UseLogger(new XunitExceptionlessLog(_writer) { MinimumLogLevel = LogLevel.Trace });
                c.UserAgent = "testclient/1.0.0.0";

                // Disable updating settings.
                c.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
            });
        }

        [Fact]
        public void CanCreateEventWithNoDuplicateTags() {
            var client = CreateClient();
            var builder = client.CreateLog("Tag Example");
            Assert.Empty(builder.Target.Tags);

            builder.AddTags("Exceptionless", null, "");
            Assert.Single(builder.Target.Tags);

            builder.AddTags("Exceptionless");
            Assert.Single(builder.Target.Tags);

            builder.AddTags("test", "Exceptionless", "exceptionless");
            Assert.Equal(2, builder.Target.Tags.Count);
        }
    }
}