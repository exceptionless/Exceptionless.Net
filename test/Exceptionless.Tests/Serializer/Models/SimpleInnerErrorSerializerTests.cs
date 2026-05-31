using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class SimpleInnerErrorSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteSimpleInnerError_ProducesSnakeCaseJson() {
            // Arrange
            var error = new SimpleInnerError {
                Message = "Parameter cannot be null",
                Type = "System.ArgumentNullException",
                StackTrace = "at Namespace.Class.Method(String param)"
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(error);

            // Assert
            SerializerContractAssertions.IncludesProperties(json, "message", "type", "stack_trace", "data", "inner");
            SerializerContractAssertions.ExcludesProperties(json, "Message", "Type", "StackTrace");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SimpleInnerError {
                Message = "Connection refused",
                Type = "System.Net.Sockets.SocketException",
                StackTrace = "at System.Net.Sockets.Socket.Connect(EndPoint remoteEP)"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SimpleInnerError)serializer.Deserialize(json, typeof(SimpleInnerError));

            // Assert
            Assert.Equal(original.Message, deserialized.Message);
            Assert.Equal(original.Type, deserialized.Type);
            Assert.Equal(original.StackTrace, deserialized.StackTrace);
        }

        [Fact]
        public void Deserialize_WithNestedInner_PreservesChain() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SimpleInnerError {
                Message = "Outer error",
                Type = "System.Exception",
                StackTrace = "at Outer()",
                Inner = new SimpleInnerError {
                    Message = "Inner error",
                    Type = "System.InvalidOperationException",
                    StackTrace = "at Inner()"
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SimpleInnerError)serializer.Deserialize(json, typeof(SimpleInnerError));

            // Assert
            Assert.Equal("Outer error", deserialized.Message);
            Assert.NotNull(deserialized.Inner);
            Assert.Equal("Inner error", deserialized.Inner.Message);
            Assert.Equal("System.InvalidOperationException", deserialized.Inner.Type);
        }

        [Fact]
        public void Deserialize_WithDataDictionary_PreservesData() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SimpleInnerError {
                Message = "Error with data",
                Type = "System.Exception",
                Data = {
                    ["key1"] = "value1"
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SimpleInnerError)serializer.Deserialize(json, typeof(SimpleInnerError));

            // Assert
            Assert.NotNull(deserialized.Data);
            Assert.True(deserialized.Data.Count >= 1);
        }

        [Fact]
        public void Deserialize_NullStackTrace_HandlesGracefully() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SimpleInnerError {
                Message = "Error without stack",
                Type = "System.Exception",
                StackTrace = null
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SimpleInnerError)serializer.Deserialize(json, typeof(SimpleInnerError));

            // Assert
            Assert.Equal("Error without stack", deserialized.Message);
            Assert.Null(deserialized.StackTrace);
        }
    }
}
