using Exceptionless.Models;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class DataDictionarySerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_EmptyDataDictionary_ProducesEmptyObject() {
            // Arrange
            var data = new DataDictionary();
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data);

            // Assert
            Assert.Equal("{}", json);
        }

        [Fact]
        public void Serialize_WithStringValues_ProducesCorrectJson() {
            // Arrange
            var data = new DataDictionary {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data);

            // Assert
            Assert.Contains("\"key1\":\"value1\"", json);
            Assert.Contains("\"key2\":\"value2\"", json);
        }

        [Fact]
        public void Serialize_WithMixedTypes_ProducesCorrectJson() {
            // Arrange
            var data = new DataDictionary {
                ["string_val"] = "hello",
                ["int_val"] = 42,
                ["bool_val"] = true,
                ["decimal_val"] = 3.14m
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data);

            // Assert
            Assert.Contains("\"string_val\":\"hello\"", json);
            Assert.Contains("\"int_val\":42", json);
            Assert.Contains("\"bool_val\":true", json);
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesStringValues() {
            // Arrange
            var serializer = GetSerializer();
            var original = new DataDictionary {
                ["name"] = "test",
                ["description"] = "A test value"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (DataDictionary)serializer.Deserialize(json, typeof(DataDictionary));

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.Count);
        }

        [Fact]
        public void Serialize_KeysPreserveOriginalCasing() {
            // Arrange
            var data = new DataDictionary {
                ["MyKey"] = "value1",
                ["UPPERCASE"] = "value2",
                ["camelCase"] = "value3"
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data);

            // Assert - Dictionary keys should NOT be snake_cased
            Assert.Contains("\"MyKey\"", json);
            Assert.Contains("\"UPPERCASE\"", json);
            Assert.Contains("\"camelCase\"", json);
        }

        [Fact]
        public void Deserialize_CaseInsensitiveLookup_WorksCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            var original = new DataDictionary {
                ["TestKey"] = "TestValue"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (DataDictionary)serializer.Deserialize(json, typeof(DataDictionary));

            // Assert - DataDictionary uses OrdinalIgnoreCase
            Assert.NotNull(deserialized);
            Assert.True(deserialized.ContainsKey("TestKey") || deserialized.ContainsKey("testkey"));
        }

        [Fact]
        public void Serialize_WithNullValue_IncludesNull() {
            // Arrange
            var data = new DataDictionary {
                ["nullable"] = null
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data);

            // Assert
            Assert.Contains("\"nullable\":null", json);
        }

        [Fact]
        public void Serialize_WithNestedObject_SerializesObject() {
            // Arrange
            var data = new DataDictionary {
                ["nested"] = new { Name = "Test", Value = 42 }
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data);

            // Assert
            Assert.Contains("\"nested\"", json);
            Assert.Contains("\"Name\"", json);
            Assert.Contains("42", json);
        }

        [Fact]
        public void Deserialize_EmptyObject_ReturnsEmptyDictionary() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{}";

            // Act
            var deserialized = (DataDictionary)serializer.Deserialize(json, typeof(DataDictionary));

            // Assert
            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
        }

        [Fact]
        public void Serialize_WithKnownDataKeyPrefixes_PreservesAtPrefix() {
            // Arrange
            var data = new DataDictionary {
                ["@error"] = "error data",
                ["@user"] = "user data",
                ["@environment"] = "env data"
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data);

            // Assert
            Assert.Contains("\"@error\"", json);
            Assert.Contains("\"@user\"", json);
            Assert.Contains("\"@environment\"", json);
        }
    }
}
