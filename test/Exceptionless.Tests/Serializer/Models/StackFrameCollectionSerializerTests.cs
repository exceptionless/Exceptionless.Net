using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class StackFrameCollectionSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_EmptyCollection_ProducesEmptyArray() {
            // Arrange
            var collection = new StackFrameCollection();
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(collection);

            // Assert
            Assert.Equal("[]", json);
        }

        [Fact]
        public void Serialize_SingleFrame_ProducesArrayWithOneElement() {
            // Arrange
            var collection = new StackFrameCollection {
                new StackFrame { Name = "Method1", LineNumber = 10 }
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(collection);

            // Assert
            Assert.Contains("\"name\":\"Method1\"", json);
            Assert.Contains("\"line_number\":10", json);
        }

        [Fact]
        public void Deserialize_RoundTrip_MultipleFrames_PreservesOrder() {
            // Arrange
            var serializer = GetSerializer();
            var original = new StackFrameCollection {
                new StackFrame { Name = "Frame1", LineNumber = 10 },
                new StackFrame { Name = "Frame2", LineNumber = 20 },
                new StackFrame { Name = "Frame3", LineNumber = 30 }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (StackFrameCollection)serializer.Deserialize(json, typeof(StackFrameCollection));

            // Assert
            Assert.Equal(3, deserialized.Count);
            Assert.Equal("Frame1", deserialized[0].Name);
            Assert.Equal("Frame2", deserialized[1].Name);
            Assert.Equal("Frame3", deserialized[2].Name);
        }

        [Fact]
        public void Deserialize_WithCompleteFrames_PreservesAllFrameProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new StackFrameCollection {
                new StackFrame {
                    FileName = "Service.cs",
                    LineNumber = 42,
                    Column = 13,
                    IsSignatureTarget = true,
                    DeclaringNamespace = "App.Services",
                    DeclaringType = "UserService",
                    Name = "GetUser",
                    ModuleId = 1,
                    GenericArguments = new GenericArguments { "T" },
                    Parameters = new ParameterCollection {
                        new Parameter { Name = "id", Type = "Int32", TypeNamespace = "System" }
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (StackFrameCollection)serializer.Deserialize(json, typeof(StackFrameCollection));

            // Assert
            Assert.Single(deserialized);
            var frame = deserialized[0];
            Assert.Equal("Service.cs", frame.FileName);
            Assert.Equal(42, frame.LineNumber);
            Assert.Equal(13, frame.Column);
            Assert.True(frame.IsSignatureTarget);
            Assert.Equal("App.Services", frame.DeclaringNamespace);
            Assert.Equal("UserService", frame.DeclaringType);
            Assert.Equal("GetUser", frame.Name);
        }
    }
}
