using System;
using System.Text.Json;
using Exceptionless.Models;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class DataDictionarySerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{}""";
        /* lang=json */
        private const string CompleteJson = """{"string_value":"hello","int_value":42,"bool_value":true,"decimal_value":3.14,"null_value":null}""";
        /* lang=json */
        private const string KnownJson = """{"MyKey":"value","Count":42,"IsActive":true}""";
        /* lang=json */
        private const string NestedObjectJson = """{"nested":{"key":"val"}}""";
        /* lang=json */
        private const string NestedArrayJson = """{"items":["a","b"]}""";

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

        [Fact]
        public void Serialize_DataDictionary_LiteralJsonString_RemainsString() {
            // Regression test: literal string values that happen to look like JSON
            // must stay strings on the wire. Only values produced from complex objects
            // should be emitted as raw JSON.
            var data = new DataDictionary {
                ["payload"] = """{"a":1}""",
                ["items"] = """[1,2]"""
            };

            string json = Serialize(data);

            using var doc = JsonDocument.Parse(json);
            Assert.Equal("""{"a":1}""", doc.RootElement.GetProperty("payload").GetString());
            Assert.Equal("[1,2]", doc.RootElement.GetProperty("items").GetString());
        }
    }
}
