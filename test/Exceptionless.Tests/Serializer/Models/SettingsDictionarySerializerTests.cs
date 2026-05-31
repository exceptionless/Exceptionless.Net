using Exceptionless.Models;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class SettingsDictionarySerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_EmptySettingsDictionary_ProducesEmptyObject() {
            // Arrange
            var settings = new SettingsDictionary();
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(settings);

            // Assert
            Assert.Equal("{}", json);
        }

        [Fact]
        public void Serialize_WithMultipleSettings_ProducesCorrectJson() {
            // Arrange
            var settings = new SettingsDictionary {
                { "@@log:*", "Off" },
                { "@@DataExclusions", "password,secret" },
                { "CustomSetting", "value" }
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(settings);

            // Assert
            Assert.Contains("\"@@log:*\":\"Off\"", json);
            Assert.Contains("\"@@DataExclusions\":\"password,secret\"", json);
            Assert.Contains("\"CustomSetting\":\"value\"", json);
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllSettings() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SettingsDictionary {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SettingsDictionary)serializer.Deserialize(json, typeof(SettingsDictionary));

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Count);
            Assert.Equal("value1", deserialized.GetString("key1"));
            Assert.Equal("value2", deserialized.GetString("key2"));
            Assert.Equal("value3", deserialized.GetString("key3"));
        }

        [Fact]
        public void Deserialize_CaseInsensitiveKeys_LooksUpCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SettingsDictionary {
                { "TestKey", "TestValue" }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SettingsDictionary)serializer.Deserialize(json, typeof(SettingsDictionary));

            // Assert - SettingsDictionary is case-insensitive
            Assert.Equal("TestValue", deserialized.GetString("TESTKEY"));
            Assert.Equal("TestValue", deserialized.GetString("testkey"));
            Assert.Equal("TestValue", deserialized.GetString("TestKey"));
        }

        [Fact]
        public void Deserialize_BooleanValues_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SettingsDictionary {
                { "enabled", "true" },
                { "disabled", "false" }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SettingsDictionary)serializer.Deserialize(json, typeof(SettingsDictionary));

            // Assert
            Assert.True(deserialized.GetBoolean("enabled"));
            Assert.False(deserialized.GetBoolean("disabled"));
        }

        [Fact]
        public void Deserialize_IntegerValues_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SettingsDictionary {
                { "maxEvents", "100" },
                { "timeout", "30000" }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SettingsDictionary)serializer.Deserialize(json, typeof(SettingsDictionary));

            // Assert
            Assert.Equal(100, deserialized.GetInt32("maxEvents"));
            Assert.Equal(30000, deserialized.GetInt32("timeout"));
        }

        [Fact]
        public void Serialize_KnownKeys_PreservesDoubleAtPrefix() {
            // Arrange
            var settings = new SettingsDictionary {
                { SettingsDictionary.KnownKeys.DataExclusions, "password,credit_card" },
                { SettingsDictionary.KnownKeys.UserAgentBotPatterns, "Googlebot,Bingbot" },
                { "@@log:MyApp", "Debug" }
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(settings);

            // Assert
            Assert.Contains("\"@@DataExclusions\"", json);
            Assert.Contains("\"@@UserAgentBotPatterns\"", json);
            Assert.Contains("\"@@log:MyApp\"", json);
        }

        [Fact]
        public void Deserialize_FromJsonInput_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{\"@@log:*\":\"Warn\",\"setting1\":\"abc\",\"setting2\":\"123\"}";

            // Act
            var deserialized = (SettingsDictionary)serializer.Deserialize(json, typeof(SettingsDictionary));

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Count);
            Assert.Equal("Warn", deserialized.GetString("@@log:*"));
            Assert.Equal("abc", deserialized.GetString("setting1"));
            Assert.Equal(123, deserialized.GetInt32("setting2"));
        }
    }
}
