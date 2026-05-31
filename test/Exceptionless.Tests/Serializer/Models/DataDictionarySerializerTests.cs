using System;
using Exceptionless.Models;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class DataDictionarySerializerTests : SerializerTestBase {
        private const string MinimalJson = /* lang=json */ """{}""";
        private const string CompleteJson = /* lang=json */ """{"string_value":"hello","int_value":42,"bool_value":true,"decimal_value":3.14,"null_value":null}""";
        private const string KnownJson = /* lang=json */ """{"MyKey":"value","Count":42,"IsActive":true}""";
        private const string NestedObjectJson = /* lang=json */ """{"nested":{"key":"val"}}""";
        private const string NestedArrayJson = /* lang=json */ """{"items":["a","b"]}""";

        [Fact]
        public void Serialize_MinimalDataDictionary_ProducesCorrectJson() {
            // Arrange
            var data = new DataDictionary();

            // Act
            string json = Serialize(data);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteDataDictionary_ProducesCorrectJson() {
            // Arrange
            var data = new DataDictionary {
                ["string_value"] = "hello",
                ["int_value"] = 42,
                ["bool_value"] = true,
                ["decimal_value"] = 3.14m,
                ["null_value"] = null
            };

            // Act
            string json = Serialize(data);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_DataDictionary_RoundTrips() {
            // Arrange
            var data = new DataDictionary {
                ["string_value"] = "hello",
                ["int_value"] = 42,
                ["bool_value"] = true,
                ["decimal_value"] = 3.14m,
                ["null_value"] = null
            };

            // Act
            DataDictionary roundTripped = RoundTrip(data);

            // Assert
            Assert.Equal("hello", roundTripped["string_value"]);
            Assert.Equal(42L, Convert.ToInt64(roundTripped["int_value"]));
            Assert.True((bool)roundTripped["bool_value"]);
            Assert.Equal(3.14m, (decimal)roundTripped["decimal_value"]);
            Assert.Null(roundTripped["null_value"]);
        }

        [Fact]
        public void Deserialize_DataDictionary_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = KnownJson;

            // Act
            DataDictionary data = Deserialize<DataDictionary>(json);

            // Assert
            Assert.Equal("value", data["MyKey"]);
            Assert.Equal(42L, Convert.ToInt64(data["Count"]));
            Assert.True((bool)data["IsActive"]);
        }

        [Fact]
        public void Deserialize_DataDictionary_JObjectValuesBecomeCompactStrings() {
            // Arrange
            const string json = NestedObjectJson;
            const string expected = /* lang=json */ """{"key":"val"}""";

            // Act
            DataDictionary data = Deserialize<DataDictionary>(json);

            // Assert
            Assert.Equal(expected, data["nested"]);
        }

        [Fact]
        public void Deserialize_DataDictionary_JArrayValuesBecomeCompactStrings() {
            // Arrange
            const string json = NestedArrayJson;
            const string expected = /* lang=json */ """["a","b"]""";

            // Act
            DataDictionary data = Deserialize<DataDictionary>(json);

            // Assert
            Assert.Equal(expected, data["items"]);
        }
    }
}
