using System.IO;
using System.Text.Json;
using Exceptionless.Dependency;
using Exceptionless.Models;
using Exceptionless.Serializer;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.MessagePack.Tests {
    public class MessagePackStorageSerializerTests : StorageSerializerTestBase {
        public MessagePackStorageSerializerTests() {
            Resolver.Register<IStorageSerializer>(new MessagePackStorageSerializer(Resolver));
        }

        [Fact]
        public override void CanSerializeTraceLogEntries() {
            base.CanSerializeTraceLogEntries();
        }

        [Fact(Skip = "This test is flakey cross platform")]
        public override void CanSerializeEnvironmentInfo() {
            base.CanSerializeEnvironmentInfo();
        }

        [Fact]
        public override void CanSerializeError() {
            base.CanSerializeError();
        }

        [Fact]
        public override void CanSerializeManualStackingInfo() {
            base.CanSerializeManualStackingInfo();
        }

        [Fact]
        public override void CanSerializeRequestInfo() {
            base.CanSerializeRequestInfo();
        }

        [Fact]
        public override void CanSerializeSimpleDataValues() {
            base.CanSerializeSimpleDataValues();
        }

        [Fact]
        public override void CanSerializeSimpleError() {
            base.CanSerializeSimpleError();
        }

        [Fact]
        public override void CanSerializeSimpleEvent() {
            base.CanSerializeSimpleEvent();
        }

        [Fact]
        public override void CanSerializeTags() {
            base.CanSerializeTags();
        }

        [Fact]
        public override void CanSerializeUserDescription() {
            base.CanSerializeUserDescription();
        }

        [Fact]
        public override void CanSerializeUserInfo() {
            base.CanSerializeUserInfo();
        }

        [Fact]
        public void LiteralJsonStringsRemainStringsAcrossStorageRoundTrip() {
            var original = new Event {
                Type = Event.KnownTypes.Log,
                Data = { ["literal"] = /* lang=json */ "{\"value\":true}" }
            };

            Event roundTripped;
            using (var stream = new MemoryStream()) {
                Resolver.GetStorageSerializer().Serialize(original, stream);
                stream.Position = 0;
                roundTripped = Resolver.GetStorageSerializer().Deserialize<Event>(stream);
            }

            Assert.Equal("{\"value\":true}", roundTripped.Data["literal"]);
            using var document = JsonDocument.Parse(Resolver.GetJsonSerializer().Serialize(roundTripped));
            Assert.Equal(JsonValueKind.String, document.RootElement.GetProperty("data").GetProperty("literal").ValueKind);
        }
    }
}
