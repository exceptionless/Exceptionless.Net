using System;
using Exceptionless;
using Xunit;

namespace Exceptionless.Tests {
    public class EventBuilderTests {
        private ExceptionlessClient CreateClient() {
            return new ExceptionlessClient(c => {
                c.UseTraceLogger();
                c.UserAgent = "testclient/1.0.0.0";
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