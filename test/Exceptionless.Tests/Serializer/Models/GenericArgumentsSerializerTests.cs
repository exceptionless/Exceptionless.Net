using Exceptionless.Models;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class GenericArgumentsSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_EmptyGenericArguments_ProducesEmptyArray() {
            // Arrange
            var args = new GenericArguments();
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(args);

            // Assert
            Assert.Equal("[]", json);
        }

        [Fact]
        public void Serialize_SingleGenericArgument_ProducesArrayWithOneElement() {
            // Arrange
            var args = new GenericArguments { "T" };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(args);

            // Assert
            Assert.Equal("[\"T\"]", json);
        }

        [Fact]
        public void Serialize_MultipleGenericArguments_ProducesArray() {
            // Arrange
            var args = new GenericArguments { "TKey", "TValue", "TResult" };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(args);

            // Assert
            Assert.Equal("[\"TKey\",\"TValue\",\"TResult\"]", json);
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllArguments() {
            // Arrange
            var serializer = GetSerializer();
            var original = new GenericArguments { "TInput", "TOutput" };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (GenericArguments)serializer.Deserialize(json, typeof(GenericArguments));

            // Assert
            Assert.Equal(2, deserialized.Count);
            Assert.Equal("TInput", deserialized[0]);
            Assert.Equal("TOutput", deserialized[1]);
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesOrder() {
            // Arrange
            var serializer = GetSerializer();
            var original = new GenericArguments { "A", "B", "C", "D" };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (GenericArguments)serializer.Deserialize(json, typeof(GenericArguments));

            // Assert
            Assert.Equal(4, deserialized.Count);
            Assert.Equal("A", deserialized[0]);
            Assert.Equal("B", deserialized[1]);
            Assert.Equal("C", deserialized[2]);
            Assert.Equal("D", deserialized[3]);
        }

        [Fact]
        public void Deserialize_FromJsonArray_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            string json = "[\"System.String\",\"System.Int32\"]";

            // Act
            var deserialized = (GenericArguments)serializer.Deserialize(json, typeof(GenericArguments));

            // Assert
            Assert.Equal(2, deserialized.Count);
            Assert.Equal("System.String", deserialized[0]);
            Assert.Equal("System.Int32", deserialized[1]);
        }
    }
}
