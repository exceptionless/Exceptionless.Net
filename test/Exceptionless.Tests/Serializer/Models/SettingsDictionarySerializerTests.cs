using Exceptionless.Models;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class SettingsDictionarySerializerTests : SerializerTestBase {
        private const string MinimalJson = /* lang=json */ """{}""";
        private const string CompleteJson = /* lang=json */ """{"@@log:*":"Off"}""";
        private const string KnownJson = /* lang=json */ """{"@@DataExclusions":"password,secret","@@UserAgentBotPatterns":"Googlebot,Bingbot","max_events":"25","is_enabled":"true"}""";
        private const string BooleanJson = /* lang=json */ """{"enabled":"true","disabled":"false","fallback":"maybe"}""";
        private const string IntegerJson = /* lang=json */ """{"max_events":"25","invalid":"abc"}""";

        [Fact]
        public void Serialize_MinimalSettingsDictionary_ProducesCorrectJson() {
            // Arrange
            var settings = new SettingsDictionary();

            // Act
            string json = Serialize(settings);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteSettingsDictionary_ProducesCorrectJson() {
            // Arrange
            var settings = new SettingsDictionary {
                [SettingsDictionary.KnownKeys.LogLevelPrefix + "*"] = "Off"
            };

            // Act
            string json = Serialize(settings);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_SettingsDictionary_RoundTrips() {
            // Arrange
            var settings = new SettingsDictionary {
                [SettingsDictionary.KnownKeys.LogLevelPrefix + "*"] = "Off",
                ["max_events"] = "25",
                ["is_enabled"] = "true"
            };

            // Act
            SettingsDictionary roundTripped = RoundTrip(settings);

            // Assert
            Assert.Equal("Off", roundTripped["@@log:*"]);
            Assert.Equal(25, roundTripped.GetInt32("max_events"));
            Assert.True(roundTripped.GetBoolean("is_enabled"));
        }

        [Fact]
        public void Deserialize_SettingsDictionary_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = KnownJson;

            // Act
            SettingsDictionary settings = Deserialize<SettingsDictionary>(json);

            // Assert
            Assert.Equal("password,secret", settings[SettingsDictionary.KnownKeys.DataExclusions]);
            Assert.Equal("Googlebot,Bingbot", settings[SettingsDictionary.KnownKeys.UserAgentBotPatterns]);
            Assert.Equal(25, settings.GetInt32("max_events"));
            Assert.True(settings.GetBoolean("is_enabled"));
        }

        [Fact]
        public void Deserialize_SettingsDictionary_GetBooleanParsesValues() {
            // Arrange
            const string json = BooleanJson;

            // Act
            SettingsDictionary settings = Deserialize<SettingsDictionary>(json);

            // Assert
            Assert.True(settings.GetBoolean("enabled"));
            Assert.False(settings.GetBoolean("disabled", true));
            Assert.True(settings.GetBoolean("fallback", true));
        }

        [Fact]
        public void Deserialize_SettingsDictionary_GetInt32ParsesValues() {
            // Arrange
            const string json = IntegerJson;

            // Act
            SettingsDictionary settings = Deserialize<SettingsDictionary>(json);

            // Assert
            Assert.Equal(25, settings.GetInt32("max_events"));
            Assert.Equal(99, settings.GetInt32("invalid", 99));
        }
    }
}
