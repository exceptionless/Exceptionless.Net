using System;
using Exceptionless;
using Xunit;

namespace Client.Tests {
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
            Assert.Equal(builder.Target.Tags.Count, 0);

            builder.AddTags("Exceptionless", null, "");
            Assert.Equal(builder.Target.Tags.Count, 1);

            builder.AddTags("Exceptionless");
            Assert.Equal(builder.Target.Tags.Count, 1);

            builder.AddTags("test", "Exceptionless", "exceptionless");
            Assert.Equal(builder.Target.Tags.Count, 2);
        }
    }
}