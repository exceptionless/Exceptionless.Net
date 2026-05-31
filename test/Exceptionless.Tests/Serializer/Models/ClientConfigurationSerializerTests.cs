using Exceptionless.Models;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ClientConfigurationSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"version":0,"settings":{}}""";
        /* lang=json */
        private const string CompleteJson = """{"version":1,"settings":{"@@log:*":"Off"}}""";

        [Fact]
        public void Serialize_MinimalClientConfiguration_ProducesCorrectJson() {
            // Arrange
            var configuration = new ClientConfiguration();

            // Act
            string json = Serialize(configuration);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteClientConfiguration_ProducesCorrectJson() {
            // Arrange
            var configuration = new ClientConfiguration { Version = 1 };
            configuration.Settings[SettingsDictionary.KnownKeys.LogLevelPrefix + "*"] = "Off";

            // Act
            string json = Serialize(configuration);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_ClientConfiguration_RoundTrips() {
            // Arrange
            var configuration = new ClientConfiguration { Version = 1 };
            configuration.Settings[SettingsDictionary.KnownKeys.LogLevelPrefix + "*"] = "Off";

            // Act
            ClientConfiguration roundTripped = RoundTrip(configuration);

            // Assert
            Assert.Equal(1, roundTripped.Version);
            Assert.Equal("Off", roundTripped.Settings["@@log:*"]);
        }

        [Fact]
        public void Deserialize_ClientConfiguration_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            ClientConfiguration configuration = Deserialize<ClientConfiguration>(json);

            // Assert
            Assert.Equal(1, configuration.Version);
            Assert.Single(configuration.Settings);
            Assert.Equal("Off", configuration.Settings["@@log:*"]);
        }
    }
}
