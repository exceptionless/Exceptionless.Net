using System.Collections.Generic;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ManualStackingInfoSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"title":null,"signature_data":{}}""";
        /* lang=json */
        private const string CompleteJson = """{"title":"Test Title","signature_data":{"Key1":"Value1","Key2":"Value2"}}""";

        [Fact]
        public void Serialize_MinimalManualStackingInfo_ProducesCorrectJson() {
            // Arrange
            var info = new ManualStackingInfo();

            // Act
            string json = Serialize(info);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteManualStackingInfo_ProducesCorrectJson() {
            // Arrange
            var info = new ManualStackingInfo {
                Title = "Test Title",
                SignatureData = new Dictionary<string, string> {
                    ["Key1"] = "Value1",
                    ["Key2"] = "Value2"
                }
            };

            // Act
            string json = Serialize(info);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_ManualStackingInfo_RoundTrips() {
            // Arrange
            var info = new ManualStackingInfo {
                Title = "Test Title",
                SignatureData = new Dictionary<string, string> {
                    ["Key1"] = "Value1",
                    ["Key2"] = "Value2"
                }
            };

            // Act
            ManualStackingInfo roundTripped = RoundTrip(info);

            // Assert
            Assert.Equal("Test Title", roundTripped.Title);
            Assert.Equal("Value1", roundTripped.SignatureData["Key1"]);
            Assert.Equal("Value2", roundTripped.SignatureData["Key2"]);
        }

        [Fact]
        public void Deserialize_ManualStackingInfo_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            ManualStackingInfo info = Deserialize<ManualStackingInfo>(json);

            // Assert
            Assert.Equal("Test Title", info.Title);
            Assert.Equal("Value1", info.SignatureData["Key1"]);
            Assert.Equal("Value2", info.SignatureData["Key2"]);
        }
    }
}
