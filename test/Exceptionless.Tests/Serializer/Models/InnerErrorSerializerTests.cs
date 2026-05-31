using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class InnerErrorSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteInnerError_ProducesSnakeCaseJson() {
            // Arrange
            var innerError = new InnerError {
                Message = "Argument is null",
                Type = "System.ArgumentNullException",
                Code = "ARG_NULL",
                StackTrace = new StackFrameCollection {
                    new StackFrame { Name = "Validate", LineNumber = 15 }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(innerError);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "message", "type", "code", "data", "inner", "stack_trace", "target_method");
            SerializerContractAssertions.ExcludesProperties(json,
                "Message", "Type", "Code", "Inner", "StackTrace", "TargetMethod");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new InnerError {
                Message = "Connection timeout",
                Type = "System.TimeoutException",
                Code = "TIMEOUT",
                StackTrace = new StackFrameCollection {
                    new StackFrame {
                        Name = "Connect",
                        DeclaringNamespace = "App.Data",
                        DeclaringType = "DbClient",
                        LineNumber = 55
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (InnerError)serializer.Deserialize(json, typeof(InnerError));

            // Assert
            Assert.Equal(original.Message, deserialized.Message);
            Assert.Equal(original.Type, deserialized.Type);
            Assert.Equal(original.Code, deserialized.Code);
            Assert.NotNull(deserialized.StackTrace);
            Assert.Single(deserialized.StackTrace);
            Assert.Equal("Connect", deserialized.StackTrace[0].Name);
        }

        [Fact]
        public void Deserialize_WithNestedInner_PreservesChain() {
            // Arrange
            var serializer = GetSerializer();
            var original = new InnerError {
                Message = "Outer",
                Type = "System.Exception",
                Inner = new InnerError {
                    Message = "Middle",
                    Type = "System.InvalidOperationException",
                    Inner = new InnerError {
                        Message = "Root",
                        Type = "System.NullReferenceException"
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (InnerError)serializer.Deserialize(json, typeof(InnerError));

            // Assert
            Assert.Equal("Outer", deserialized.Message);
            Assert.NotNull(deserialized.Inner);
            Assert.Equal("Middle", deserialized.Inner.Message);
            Assert.NotNull(deserialized.Inner.Inner);
            Assert.Equal("Root", deserialized.Inner.Inner.Message);
        }

        [Fact]
        public void Deserialize_WithDataDictionary_PreservesData() {
            // Arrange
            var serializer = GetSerializer();
            var original = new InnerError {
                Message = "Error with context",
                Type = "System.Exception",
                Data = {
                    ["context_key"] = "context_value"
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (InnerError)serializer.Deserialize(json, typeof(InnerError));

            // Assert
            Assert.NotNull(deserialized.Data);
            Assert.True(deserialized.Data.Count >= 1);
        }

        [Fact]
        public void Deserialize_WithTargetMethod_PreservesMethod() {
            // Arrange
            var serializer = GetSerializer();
            var original = new InnerError {
                Message = "Test",
                Type = "System.Exception",
                TargetMethod = new Method {
                    Name = "Execute",
                    DeclaringNamespace = "App.Core",
                    DeclaringType = "Executor",
                    IsSignatureTarget = true
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (InnerError)serializer.Deserialize(json, typeof(InnerError));

            // Assert
            Assert.NotNull(deserialized.TargetMethod);
            Assert.Equal("Execute", deserialized.TargetMethod.Name);
            Assert.Equal("App.Core", deserialized.TargetMethod.DeclaringNamespace);
            Assert.Equal("Executor", deserialized.TargetMethod.DeclaringType);
            Assert.True(deserialized.TargetMethod.IsSignatureTarget);
        }

        [Fact]
        public void Deserialize_MinimalInnerError_PreservesBasicProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new InnerError {
                Message = "Simple error",
                Type = "System.Exception"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (InnerError)serializer.Deserialize(json, typeof(InnerError));

            // Assert
            Assert.Equal("Simple error", deserialized.Message);
            Assert.Equal("System.Exception", deserialized.Type);
            Assert.Null(deserialized.Inner);
            Assert.Null(deserialized.TargetMethod);
        }
    }
}
