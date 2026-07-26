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
        public void Serialize_WithLiteralJsonString_PreservesStringAcrossStorageRoundTrip() {
            // Arrange
            var original = new Event {
                Type = Event.KnownTypes.Log,
                Data = { ["literal"] = /* lang=json */ "{\"value\":true}" }
            };

            // Act
            Event roundTripped;
            using (var stream = new MemoryStream()) {
                Resolver.GetStorageSerializer().Serialize(original, stream);
                stream.Position = 0;
                roundTripped = Resolver.GetStorageSerializer().Deserialize<Event>(stream);
            }

            using var document = JsonDocument.Parse(Resolver.GetJsonSerializer().Serialize(roundTripped));

            // Assert
            Assert.Equal("{\"value\":true}", roundTripped.Data["literal"]);
            Assert.Equal(JsonValueKind.String, document.RootElement.GetProperty("data").GetProperty("literal").ValueKind);
        }

        [Fact]
        public void Serialize_WithRawJsonValue_PreservesStructureAcrossStorageRoundTrip() {
            // Arrange
            const string json = "{\"type\":\"log\",\"data\":{\"payload\":{"
                + "\"timestamp\":\"2026-07-25T12:34:56.0000000+00:00\","
                + "\"count\":42,\"enabled\":true,\"items\":[1,null,\"value\"]}}}";
            var jsonSerializer = Resolver.GetJsonSerializer();
            var original = (Event)jsonSerializer.Deserialize(json, typeof(Event));
            object originalPayload = original.Data["payload"];

            // Act
            Event roundTripped;
            using (var stream = new MemoryStream()) {
                Resolver.GetStorageSerializer().Serialize(original, stream);
                stream.Position = 0;
                roundTripped = Resolver.GetStorageSerializer().Deserialize<Event>(stream);
            }

            using var document = JsonDocument.Parse(jsonSerializer.Serialize(roundTripped));
            JsonElement payload = document.RootElement.GetProperty("data").GetProperty("payload");

            // Assert
            Assert.IsType<string>(originalPayload);
            Assert.Equal(JsonValueKind.Object, payload.ValueKind);
            Assert.Equal("2026-07-25T12:34:56.0000000+00:00", payload.GetProperty("timestamp").GetString());
            Assert.Equal(42, payload.GetProperty("count").GetInt32());
            Assert.True(payload.GetProperty("enabled").GetBoolean());
            Assert.Equal(JsonValueKind.Null, payload.GetProperty("items")[1].ValueKind);
        }
    }
}
