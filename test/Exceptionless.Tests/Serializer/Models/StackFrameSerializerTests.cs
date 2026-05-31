using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class StackFrameSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteStackFrame_ProducesSnakeCaseJson() {
            // Arrange
            var frame = new StackFrame {
                FileName = "UserService.cs",
                LineNumber = 42,
                Column = 13,
                IsSignatureTarget = true,
                DeclaringNamespace = "MyApp.Services",
                DeclaringType = "UserService",
                Name = "GetUserAsync",
                ModuleId = 1,
                Data = { ["ILOffset"] = 64 },
                GenericArguments = new GenericArguments { "T" },
                Parameters = new ParameterCollection {
                    new Parameter { Name = "id", Type = "Int32", TypeNamespace = "System" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(frame);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "file_name", "line_number", "column", "is_signature_target",
                "declaring_namespace", "declaring_type", "name", "module_id",
                "data", "generic_arguments", "parameters");
            SerializerContractAssertions.ExcludesProperties(json,
                "FileName", "LineNumber", "Column", "IsSignatureTarget",
                "DeclaringNamespace", "DeclaringType", "ModuleId");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new StackFrame {
                FileName = "Program.cs",
                LineNumber = 100,
                Column = 20,
                IsSignatureTarget = true,
                DeclaringNamespace = "App",
                DeclaringType = "Program",
                Name = "Main",
                ModuleId = 5
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (StackFrame)serializer.Deserialize(json, typeof(StackFrame));

            // Assert
            Assert.Equal("Program.cs", deserialized.FileName);
            Assert.Equal(100, deserialized.LineNumber);
            Assert.Equal(20, deserialized.Column);
            Assert.True(deserialized.IsSignatureTarget);
            Assert.Equal("App", deserialized.DeclaringNamespace);
            Assert.Equal("Program", deserialized.DeclaringType);
            Assert.Equal("Main", deserialized.Name);
            Assert.Equal(5, deserialized.ModuleId);
        }

        [Fact]
        public void Deserialize_MinimalStackFrame_PreservesProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new StackFrame {
                Name = "Execute",
                LineNumber = 10
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (StackFrame)serializer.Deserialize(json, typeof(StackFrame));

            // Assert
            Assert.Equal("Execute", deserialized.Name);
            Assert.Equal(10, deserialized.LineNumber);
            Assert.Null(deserialized.FileName);
            Assert.Equal(0, deserialized.Column);
        }

        [Fact]
        public void Deserialize_WithGenericArguments_PreservesGenericArgs() {
            // Arrange
            var serializer = GetSerializer();
            var original = new StackFrame {
                Name = "Process",
                GenericArguments = new GenericArguments { "TInput", "TOutput" }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (StackFrame)serializer.Deserialize(json, typeof(StackFrame));

            // Assert
            Assert.Equal(2, deserialized.GenericArguments.Count);
            Assert.Equal("TInput", deserialized.GenericArguments[0]);
            Assert.Equal("TOutput", deserialized.GenericArguments[1]);
        }

        [Fact]
        public void Deserialize_WithParameters_PreservesParameters() {
            // Arrange
            var serializer = GetSerializer();
            var original = new StackFrame {
                Name = "SaveUser",
                Parameters = new ParameterCollection {
                    new Parameter {
                        Name = "user",
                        Type = "UserModel",
                        TypeNamespace = "App.Models",
                        GenericArguments = new GenericArguments { "T" }
                    },
                    new Parameter {
                        Name = "saveOptions",
                        Type = "SaveOptions",
                        TypeNamespace = "App.Core"
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (StackFrame)serializer.Deserialize(json, typeof(StackFrame));

            // Assert
            Assert.Equal(2, deserialized.Parameters.Count);
            Assert.Equal("user", deserialized.Parameters[0].Name);
            Assert.Equal("UserModel", deserialized.Parameters[0].Type);
            Assert.Equal("App.Models", deserialized.Parameters[0].TypeNamespace);
            Assert.Equal("saveOptions", deserialized.Parameters[1].Name);
        }

        [Fact]
        public void Serialize_ZeroLineNumber_SerializesAsZero() {
            // Arrange
            var frame = new StackFrame { Name = "Native", LineNumber = 0, Column = 0 };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(frame);

            // Assert
            Assert.Contains("\"line_number\":0", json);
            Assert.Contains("\"column\":0", json);
        }

        [Fact]
        public void Deserialize_WithFilePathContainingBackslashes_PreservesPath() {
            // Arrange
            var serializer = GetSerializer();
            var original = new StackFrame {
                FileName = "C:\\Users\\dev\\src\\App\\Service.cs",
                Name = "Run",
                LineNumber = 55
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (StackFrame)serializer.Deserialize(json, typeof(StackFrame));

            // Assert
            Assert.Equal("C:\\Users\\dev\\src\\App\\Service.cs", deserialized.FileName);
        }
    }
}
