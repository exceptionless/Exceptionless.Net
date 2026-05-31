using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class SimpleErrorSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteSimpleError_ProducesSnakeCaseJson() {
            // Arrange
            var error = new SimpleError {
                Message = "Test error",
                Type = "System.Exception",
                StackTrace = "at TestClass.TestMethod() in File.cs:line 42",
                Data = {
                    [SimpleError.KnownDataKeys.ExtraProperties] = new { OrderId = 10 }
                },
                Inner = new SimpleInnerError {
                    Message = "Inner",
                    Type = "System.ArgumentException",
                    StackTrace = "at Inner.Method()"
                },
                Modules = new ModuleCollection {
                    new Module { ModuleId = 1, Name = "App.dll", Version = "1.0.0" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(error);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "message", "type", "stack_trace", "data", "inner", "modules");
            SerializerContractAssertions.ExcludesProperties(json,
                "Message", "Type", "StackTrace", "Modules");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SimpleError {
                Message = "File not found",
                Type = "System.IO.FileNotFoundException",
                StackTrace = "at System.IO.File.Open(String path)",
                Modules = new ModuleCollection {
                    new Module { ModuleId = 1, Name = "mscorlib.dll", Version = "4.0.0" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SimpleError)serializer.Deserialize(json, typeof(SimpleError));

            // Assert
            Assert.Equal(original.Message, deserialized.Message);
            Assert.Equal(original.Type, deserialized.Type);
            Assert.Equal(original.StackTrace, deserialized.StackTrace);
            Assert.NotNull(deserialized.Modules);
            Assert.Single(deserialized.Modules);
            Assert.Equal("mscorlib.dll", deserialized.Modules[0].Name);
        }

        [Fact]
        public void Deserialize_WithInnerError_PreservesHierarchy() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SimpleError {
                Message = "Outer",
                Type = "System.Exception",
                StackTrace = "at Outer()",
                Inner = new SimpleInnerError {
                    Message = "Inner",
                    Type = "System.ArgumentException",
                    StackTrace = "at Inner()",
                    Inner = new SimpleInnerError {
                        Message = "Root",
                        Type = "System.NullReferenceException",
                        StackTrace = "at Root()"
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SimpleError)serializer.Deserialize(json, typeof(SimpleError));

            // Assert
            Assert.Equal("Outer", deserialized.Message);
            Assert.NotNull(deserialized.Inner);
            Assert.Equal("Inner", deserialized.Inner.Message);
            Assert.NotNull(deserialized.Inner.Inner);
            Assert.Equal("Root", deserialized.Inner.Inner.Message);
        }

        [Fact]
        public void Deserialize_MinimalSimpleError_PreservesProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new SimpleError {
                Message = "Simple",
                Type = "System.Exception"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (SimpleError)serializer.Deserialize(json, typeof(SimpleError));

            // Assert
            Assert.Equal("Simple", deserialized.Message);
            Assert.Equal("System.Exception", deserialized.Type);
            Assert.Null(deserialized.Inner);
        }

        [Fact]
        public void Serialize_ExtraPropertiesKey_UsesCorrectConstant() {
            // Arrange
            var error = new SimpleError {
                Message = "Test",
                Type = "System.Exception",
                Data = {
                    [SimpleError.KnownDataKeys.ExtraProperties] = new { Key = "Value" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(error);

            // Assert
            Assert.Contains("\"@ext\"", json);
        }
    }
}
