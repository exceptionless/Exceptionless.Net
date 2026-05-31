using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ClientConfigurationSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteClientConfiguration_ProducesSnakeCaseJson() {
            // Arrange
            var config = new ClientConfiguration {
                Version = 5,
                Settings = {
                    { "@@log:*", "Off" },
                    { "IncludeConditionalData", "true" },
                    { "DataExclusions", "password,credit_card" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(config);

            // Assert
            SerializerContractAssertions.IncludesProperties(json, "version", "settings");
            SerializerContractAssertions.ExcludesProperties(json, "Version", "Settings");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesSettings() {
            // Arrange
            var serializer = GetSerializer();
            var original = new ClientConfiguration {
                Version = 3,
                Settings = {
                    { "@@log:*", "Warn" },
                    { "TestKey", "TestValue" },
                    { "@@DataExclusions", "secret" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (ClientConfiguration)serializer.Deserialize(json, typeof(ClientConfiguration));

            // Assert
            Assert.Equal(3, deserialized.Version);
            Assert.NotNull(deserialized.Settings);
            Assert.Equal(3, deserialized.Settings.Count);
            Assert.Equal("Warn", deserialized.Settings.GetString("@@log:*"));
            Assert.Equal("TestValue", deserialized.Settings.GetString("TestKey"));
        }

        [Fact]
        public void Deserialize_EmptySettings_ReturnsEmptyDictionary() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{\"version\":1,\"settings\":{}}";

            // Act
            var config = (ClientConfiguration)serializer.Deserialize(json, typeof(ClientConfiguration));

            // Assert
            Assert.NotNull(config);
            Assert.Equal(1, config.Version);
            Assert.NotNull(config.Settings);
            Assert.Empty(config.Settings);
        }

        [Fact]
        public void Deserialize_WithVersion_PreservesVersionNumber() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{\"version\":42,\"settings\":{}}";

            // Act
            var config = (ClientConfiguration)serializer.Deserialize(json, typeof(ClientConfiguration));

            // Assert
            Assert.Equal(42, config.Version);
        }

        [Fact]
        public void Serialize_SettingsDictionaryKeys_PreservesOriginalCasing() {
            // Arrange
            var config = new ClientConfiguration {
                Version = 1,
                Settings = {
                    { "MyCustomKey", "value" },
                    { "@@log:MyApp.*", "Debug" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(config);

            // Assert - Dictionary keys should preserve their original casing
            Assert.Contains("\"MyCustomKey\"", json);
            Assert.Contains("\"@@log:MyApp.*\"", json);
        }

        [Fact]
        public void Deserialize_SettingsAreCaseInsensitive() {
            // Arrange
            var serializer = GetSerializer();
            var config = new ClientConfiguration {
                Version = 1,
                Settings = {
                    { "TestKey", "TestValue" }
                }
            };

            // Act
            string json = serializer.Serialize(config);
            var deserialized = (ClientConfiguration)serializer.Deserialize(json, typeof(ClientConfiguration));

            // Assert - SettingsDictionary uses OrdinalIgnoreCase comparer
            Assert.Equal("TestValue", deserialized.Settings.GetString("testkey"));
            Assert.Equal("TestValue", deserialized.Settings.GetString("TESTKEY"));
        }

        [Fact]
        public void Serialize_DefaultConfiguration_ProducesMinimalJson() {
            // Arrange
            var config = new ClientConfiguration();
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(config);

            // Assert
            Assert.Contains("\"version\":0", json);
            Assert.Contains("\"settings\":{}", json);
        }

        [Fact]
        public void Deserialize_SettingsWithBooleanValues_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            var config = new ClientConfiguration {
                Version = 1,
                Settings = {
                    { "IncludeConditionalData", "true" },
                    { "DisableFeature", "false" }
                }
            };

            // Act
            string json = serializer.Serialize(config);
            var deserialized = (ClientConfiguration)serializer.Deserialize(json, typeof(ClientConfiguration));

            // Assert
            Assert.True(deserialized.Settings.GetBoolean("IncludeConditionalData"));
            Assert.False(deserialized.Settings.GetBoolean("DisableFeature"));
        }
    }
}
