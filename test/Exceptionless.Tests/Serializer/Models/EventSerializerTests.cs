using System;
using System.Collections.Generic;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class EventSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteEvent_ProducesCorrectJson() {
            // Arrange
            var ev = new Event {
                Type = Event.KnownTypes.Log,
                Source = "TestApp",
                Date = new DateTimeOffset(2023, 6, 15, 10, 30, 0, TimeSpan.Zero),
                Tags = { "Critical", "Production" },
                Message = "Application error occurred",
                Geo = "51.5074,-0.1278",
                Value = 99.5m,
                Count = 3,
                ReferenceId = "ref-abc-123"
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(ev);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "type", "source", "date", "tags", "message", "geo", "value", "count", "reference_id");
            SerializerContractAssertions.ExcludesProperties(json,
                "Type", "Source", "Date", "Tags", "Message", "Geo", "Value", "Count", "ReferenceId");
        }

        [Fact]
        public void Serialize_MinimalEvent_ProducesValidJson() {
            // Arrange
            var ev = new Event {
                Type = Event.KnownTypes.Log,
                Source = "Minimal"
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(ev);

            // Assert
            Assert.Contains("\"type\":\"log\"", json);
            Assert.Contains("\"source\":\"Minimal\"", json);
        }

        [Fact]
        public void Deserialize_CompleteEvent_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Event {
                Type = Event.KnownTypes.Error,
                Source = "TestApp",
                Date = new DateTimeOffset(2023, 6, 15, 10, 30, 0, TimeSpan.Zero),
                Tags = { "Critical", "Production" },
                Message = "Test error",
                Geo = "40.7128,-74.0060",
                Value = 42.5m,
                Count = 5,
                ReferenceId = "ref-123"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Event)serializer.Deserialize(json, typeof(Event));

            // Assert
            Assert.Equal(original.Type, deserialized.Type);
            Assert.Equal(original.Source, deserialized.Source);
            Assert.Equal(original.Date, deserialized.Date);
            Assert.Equal(original.Message, deserialized.Message);
            Assert.Equal(original.Geo, deserialized.Geo);
            Assert.Equal(original.Value, deserialized.Value);
            Assert.Equal(original.Count, deserialized.Count);
            Assert.Equal(original.ReferenceId, deserialized.ReferenceId);
        }

        [Fact]
        public void Deserialize_EventWithTags_PreservesTags() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Event {
                Type = Event.KnownTypes.Log,
                Source = "Tags",
                Tags = { "tag1", "tag2", "tag3" }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Event)serializer.Deserialize(json, typeof(Event));

            // Assert
            Assert.NotNull(deserialized.Tags);
            Assert.Equal(3, deserialized.Tags.Count);
            Assert.Contains("tag1", deserialized.Tags);
            Assert.Contains("tag2", deserialized.Tags);
            Assert.Contains("tag3", deserialized.Tags);
        }

        [Fact]
        public void Deserialize_EventWithDataDictionary_PreservesSimpleValues() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Event {
                Type = Event.KnownTypes.Log,
                Source = "Data",
                Data = {
                    ["StringVal"] = "hello",
                    [Event.KnownDataKeys.Level] = "Error",
                    [Event.KnownDataKeys.Version] = "1.2.3"
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Event)serializer.Deserialize(json, typeof(Event));

            // Assert
            Assert.NotNull(deserialized.Data);
            Assert.True(deserialized.Data.ContainsKey("StringVal"));
            Assert.True(deserialized.Data.ContainsKey(Event.KnownDataKeys.Level));
            Assert.True(deserialized.Data.ContainsKey(Event.KnownDataKeys.Version));
        }

        [Fact]
        public void Deserialize_EventWithNullOptionalFields_HandlesGracefully() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{\"type\":\"log\",\"source\":\"test\",\"date\":\"2023-01-01T00:00:00+00:00\",\"tags\":[],\"message\":null,\"geo\":null,\"value\":null,\"count\":null,\"data\":{},\"reference_id\":null}";

            // Act
            var deserialized = (Event)serializer.Deserialize(json, typeof(Event));

            // Assert
            Assert.Equal("log", deserialized.Type);
            Assert.Equal("test", deserialized.Source);
            Assert.Null(deserialized.Message);
            Assert.Null(deserialized.Geo);
            Assert.Null(deserialized.Value);
            Assert.Null(deserialized.Count);
            Assert.Null(deserialized.ReferenceId);
        }

        [Fact]
        public void Deserialize_ReferenceId_SnakeCaseProperty() {
            // Arrange
            var serializer = GetSerializer();

            // Act
            var ev = (Event)serializer.Deserialize("{\"reference_id\": \"abc-123\"}", typeof(Event));

            // Assert
            Assert.Equal("abc-123", ev.ReferenceId);
        }

        [Fact]
        public void Serialize_EventWithUserDescription_SerializesNestedModel() {
            // Arrange
            var ev = new Event {
                Type = Event.KnownTypes.Log,
                Source = "test",
                Data = {
                    [Event.KnownDataKeys.UserDescription] = new UserDescription {
                        EmailAddress = "test@example.com",
                        Description = "Something broke"
                    }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(ev);

            // Assert
            Assert.Contains("\"@user_description\"", json);
            Assert.Contains("\"email_address\":\"test@example.com\"", json);
            Assert.Contains("\"description\":\"Something broke\"", json);
        }

        [Fact]
        public void Serialize_EventWithUserInfo_SerializesNestedModel() {
            // Arrange
            var ev = new Event {
                Type = Event.KnownTypes.Log,
                Source = "test",
                Data = {
                    [Event.KnownDataKeys.UserInfo] = new UserInfo("user123", "John Doe")
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(ev);

            // Assert
            Assert.Contains("\"@user\"", json);
            Assert.Contains("\"identity\":\"user123\"", json);
            Assert.Contains("\"name\":\"John Doe\"", json);
        }

        [Fact]
        public void Serialize_EventWithEnvironmentInfo_SerializesNestedModel() {
            // Arrange
            var ev = new Event {
                Type = Event.KnownTypes.Log,
                Source = "test",
                Data = {
                    [Event.KnownDataKeys.EnvironmentInfo] = new EnvironmentInfo {
                        MachineName = "PROD-01",
                        ProcessorCount = 8,
                        OSName = "Windows",
                        Architecture = "x64"
                    }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(ev);

            // Assert
            Assert.Contains("\"@environment\"", json);
            Assert.Contains("\"machine_name\":\"PROD-01\"", json);
            Assert.Contains("\"processor_count\":8", json);
        }

        [Fact]
        public void Serialize_EventWithTraceLog_SerializesStringList() {
            // Arrange
            var ev = new Event {
                Type = Event.KnownTypes.Log,
                Source = "test",
                Data = {
                    [Event.KnownDataKeys.TraceLog] = new List<string> { "trace line 1", "trace line 2" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(ev);

            // Assert
            Assert.Contains("\"@trace\":[\"trace line 1\",\"trace line 2\"]", json);
        }

        [Fact]
        public void Serialize_EventWithSpecialCharacters_EscapesCorrectly() {
            // Arrange
            var ev = new Event {
                Type = Event.KnownTypes.Log,
                Source = "test",
                Message = "Error: \"file not found\" at C:\\Users\\test\\file.txt"
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(ev);
            var deserialized = (Event)serializer.Deserialize(json, typeof(Event));

            // Assert
            Assert.Equal("Error: \"file not found\" at C:\\Users\\test\\file.txt", deserialized.Message);
        }

        [Fact]
        public void Serialize_EventWithDecimalValue_PreservesPrecision() {
            // Arrange
            var ev = new Event {
                Type = Event.KnownTypes.Log,
                Source = "test",
                Value = 123.456m
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(ev);
            var deserialized = (Event)serializer.Deserialize(json, typeof(Event));

            // Assert
            Assert.Equal(123.456m, deserialized.Value);
        }

        [Fact]
        public void Serialize_EventWithAllKnownTypes_SerializesTypeCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            var types = new[] { Event.KnownTypes.Error, Event.KnownTypes.FeatureUsage, Event.KnownTypes.Log, Event.KnownTypes.NotFound, Event.KnownTypes.Session };

            foreach (var type in types) {
                var ev = new Event { Type = type, Source = "test" };

                // Act
                string json = serializer.Serialize(ev);
                var deserialized = (Event)serializer.Deserialize(json, typeof(Event));

                // Assert
                Assert.Equal(type, deserialized.Type);
            }
        }
    }
}
