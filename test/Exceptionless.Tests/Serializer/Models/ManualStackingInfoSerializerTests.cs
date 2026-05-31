using System.Collections.Generic;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ManualStackingInfoSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteManualStackingInfo_ProducesSnakeCaseJson() {
            // Arrange
            var info = new ManualStackingInfo {
                Title = "Payment Processing Error",
                SignatureData = new Dictionary<string, string> {
                    { "provider", "stripe" },
                    { "error_code", "card_declined" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(info);

            // Assert
            SerializerContractAssertions.IncludesProperties(json, "title", "signature_data");
            SerializerContractAssertions.ExcludesProperties(json, "Title", "SignatureData");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new ManualStackingInfo {
                Title = "Custom Stack Title",
                SignatureData = new Dictionary<string, string> {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" },
                    { "Key3", "Value3" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (ManualStackingInfo)serializer.Deserialize(json, typeof(ManualStackingInfo));

            // Assert
            Assert.Equal("Custom Stack Title", deserialized.Title);
            Assert.NotNull(deserialized.SignatureData);
            Assert.Equal(3, deserialized.SignatureData.Count);
            Assert.Equal("Value1", deserialized.SignatureData["Key1"]);
            Assert.Equal("Value2", deserialized.SignatureData["Key2"]);
            Assert.Equal("Value3", deserialized.SignatureData["Key3"]);
        }

        [Fact]
        public void Deserialize_WithMinimalProperties_PreservesTitle() {
            // Arrange
            var serializer = GetSerializer();
            var original = new ManualStackingInfo { Title = "Simple Stack" };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (ManualStackingInfo)serializer.Deserialize(json, typeof(ManualStackingInfo));

            // Assert
            Assert.Equal("Simple Stack", deserialized.Title);
            Assert.NotNull(deserialized.SignatureData);
            Assert.Empty(deserialized.SignatureData);
        }

        [Fact]
        public void Deserialize_FromSnakeCaseJson_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{\"title\":\"Custom Stack\",\"signature_data\":{\"key\":\"value\"}}";

            // Act
            var result = (ManualStackingInfo)serializer.Deserialize(json, typeof(ManualStackingInfo));

            // Assert
            Assert.Equal("Custom Stack", result.Title);
            Assert.NotNull(result.SignatureData);
            Assert.Equal("value", result.SignatureData["key"]);
        }

        [Fact]
        public void Deserialize_WithSpecialCharacters_PreservesValues() {
            // Arrange
            var serializer = GetSerializer();
            var original = new ManualStackingInfo {
                Title = "Error: \"Connection refused\" at /api/v2/events",
                SignatureData = new Dictionary<string, string> {
                    { "path", "/api/v2/events?filter=type:error" },
                    { "message", "Connection refused: host=db.example.com, port=5432" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (ManualStackingInfo)serializer.Deserialize(json, typeof(ManualStackingInfo));

            // Assert
            Assert.Contains("Connection refused", deserialized.Title);
            Assert.Equal("/api/v2/events?filter=type:error", deserialized.SignatureData["path"]);
        }

        [Fact]
        public void Serialize_ConstructorWithTitle_TrimsWhitespace() {
            // Arrange
            var info = new ManualStackingInfo("  Trimmed Title  ");
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(info);

            // Assert
            Assert.Contains("\"title\":\"Trimmed Title\"", json);
        }

        [Fact]
        public void Serialize_ConstructorWithTitleAndData_PreservesBoth() {
            // Arrange
            var data = new Dictionary<string, string> { { "k1", "v1" } };
            var info = new ManualStackingInfo("Title", data);
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(info);
            var deserialized = (ManualStackingInfo)serializer.Deserialize(json, typeof(ManualStackingInfo));

            // Assert
            Assert.Equal("Title", deserialized.Title);
            Assert.Equal("v1", deserialized.SignatureData["k1"]);
        }
    }
}
