using System;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ErrorSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteError_ProducesSnakeCaseJson() {
            // Arrange
            var error = new Error {
                Message = "Object reference not set",
                Type = "System.NullReferenceException",
                Code = "NRE001",
                StackTrace = new StackFrameCollection {
                    new StackFrame {
                        Name = "GetUser",
                        DeclaringNamespace = "MyApp.Services",
                        DeclaringType = "UserService",
                        FileName = "UserService.cs",
                        LineNumber = 42,
                        Column = 13
                    }
                },
                Modules = new ModuleCollection {
                    new Module {
                        ModuleId = 1,
                        Name = "MyApp.dll",
                        Version = "2.0.0",
                        IsEntry = true,
                        CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        ModifiedDate = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc)
                    }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(error);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "message", "type", "code", "data", "stack_trace", "modules", "target_method");
            SerializerContractAssertions.ExcludesProperties(json,
                "Message", "Type", "Code", "StackTrace", "Modules", "TargetMethod");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Error {
                Message = "Division by zero",
                Type = "System.DivideByZeroException",
                Code = "DBZ",
                StackTrace = new StackFrameCollection {
                    new StackFrame {
                        Name = "Calculate",
                        DeclaringNamespace = "App.Math",
                        DeclaringType = "Calculator",
                        FileName = "Calculator.cs",
                        LineNumber = 100,
                        Column = 5,
                        IsSignatureTarget = true,
                        ModuleId = 1
                    }
                },
                Modules = new ModuleCollection {
                    new Module {
                        ModuleId = 1,
                        Name = "App.Math.dll",
                        Version = "1.0.0",
                        IsEntry = false
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Error)serializer.Deserialize(json, typeof(Error));

            // Assert
            Assert.Equal(original.Message, deserialized.Message);
            Assert.Equal(original.Type, deserialized.Type);
            Assert.Equal(original.Code, deserialized.Code);
            Assert.NotNull(deserialized.StackTrace);
            Assert.Single(deserialized.StackTrace);
            Assert.Equal("Calculate", deserialized.StackTrace[0].Name);
            Assert.Equal("App.Math", deserialized.StackTrace[0].DeclaringNamespace);
            Assert.Equal("Calculator", deserialized.StackTrace[0].DeclaringType);
            Assert.Equal("Calculator.cs", deserialized.StackTrace[0].FileName);
            Assert.Equal(100, deserialized.StackTrace[0].LineNumber);
            Assert.Equal(5, deserialized.StackTrace[0].Column);
            Assert.True(deserialized.StackTrace[0].IsSignatureTarget);
            Assert.NotNull(deserialized.Modules);
            Assert.Single(deserialized.Modules);
            Assert.Equal("App.Math.dll", deserialized.Modules[0].Name);
        }

        [Fact]
        public void Deserialize_ErrorWithInnerError_PreservesHierarchy() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Error {
                Message = "Outer exception",
                Type = "System.InvalidOperationException",
                Inner = new InnerError {
                    Message = "Inner exception",
                    Type = "System.ArgumentNullException",
                    Code = "ARG_NULL"
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Error)serializer.Deserialize(json, typeof(Error));

            // Assert
            Assert.NotNull(deserialized.Inner);
            Assert.Equal("Inner exception", deserialized.Inner.Message);
            Assert.Equal("System.ArgumentNullException", deserialized.Inner.Type);
            Assert.Equal("ARG_NULL", deserialized.Inner.Code);
        }

        [Fact]
        public void Deserialize_ErrorWithDeeplyNestedInner_PreservesFullHierarchy() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Error {
                Message = "Level 1",
                Type = "System.Exception",
                Inner = new InnerError {
                    Message = "Level 2",
                    Type = "System.InvalidOperationException",
                    Inner = new InnerError {
                        Message = "Level 3 - Root cause",
                        Type = "System.IO.FileNotFoundException"
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Error)serializer.Deserialize(json, typeof(Error));

            // Assert
            Assert.Equal("Level 1", deserialized.Message);
            Assert.NotNull(deserialized.Inner);
            Assert.Equal("Level 2", deserialized.Inner.Message);
            Assert.NotNull(deserialized.Inner.Inner);
            Assert.Equal("Level 3 - Root cause", deserialized.Inner.Inner.Message);
        }

        [Fact]
        public void Deserialize_ErrorWithExtraProperties_PreservesData() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Error {
                Message = "Error with data",
                Type = "System.Exception",
                Data = {
                    [Error.KnownDataKeys.ExtraProperties] = new { OrderId = 42, Status = "failed" }
                }
            };

            // Act
            string json = serializer.Serialize(original);

            // Assert
            Assert.Contains("\"@ext\"", json);
            Assert.Contains("\"OrderId\"", json);
        }

        [Fact]
        public void Deserialize_ErrorWithMultipleStackFrames_PreservesOrder() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Error {
                Message = "Stack overflow",
                Type = "System.StackOverflowException",
                StackTrace = new StackFrameCollection {
                    new StackFrame { Name = "MethodA", LineNumber = 10 },
                    new StackFrame { Name = "MethodB", LineNumber = 20 },
                    new StackFrame { Name = "MethodC", LineNumber = 30 }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Error)serializer.Deserialize(json, typeof(Error));

            // Assert
            Assert.Equal(3, deserialized.StackTrace.Count);
            Assert.Equal("MethodA", deserialized.StackTrace[0].Name);
            Assert.Equal("MethodB", deserialized.StackTrace[1].Name);
            Assert.Equal("MethodC", deserialized.StackTrace[2].Name);
            Assert.Equal(10, deserialized.StackTrace[0].LineNumber);
            Assert.Equal(20, deserialized.StackTrace[1].LineNumber);
            Assert.Equal(30, deserialized.StackTrace[2].LineNumber);
        }

        [Fact]
        public void Deserialize_ErrorWithModuleData_PreservesModuleData() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Error {
                Message = "Module test",
                Type = "System.Exception",
                Modules = new ModuleCollection {
                    new Module {
                        ModuleId = 1,
                        Name = "System.Private.CoreLib.dll",
                        Version = "6.0.0",
                        IsEntry = false,
                        Data = { ["PublicKeyToken"] = "b03f5f7f11d50a3a" }
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Error)serializer.Deserialize(json, typeof(Error));

            // Assert
            Assert.Single(deserialized.Modules);
            Assert.Equal("System.Private.CoreLib.dll", deserialized.Modules[0].Name);
            Assert.Equal("6.0.0", deserialized.Modules[0].Version);
            Assert.Equal(1, deserialized.Modules[0].ModuleId);
        }

        [Fact]
        public void Serialize_ErrorWithSpecialCharactersInMessage_EscapesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Error {
                Message = "Error: \"file not found\" at C:\\Users\\test\\path",
                Type = "System.IO.FileNotFoundException"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Error)serializer.Deserialize(json, typeof(Error));

            // Assert
            Assert.Equal("Error: \"file not found\" at C:\\Users\\test\\path", deserialized.Message);
        }

        [Fact]
        public void Deserialize_ErrorWithTargetMethod_PreservesMethod() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Error {
                Message = "Target method test",
                Type = "System.Exception",
                TargetMethod = new Method {
                    Name = "ProcessRequest",
                    DeclaringNamespace = "App.Controllers",
                    DeclaringType = "HomeController",
                    IsSignatureTarget = true,
                    ModuleId = 2
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Error)serializer.Deserialize(json, typeof(Error));

            // Assert
            Assert.NotNull(deserialized.TargetMethod);
            Assert.Equal("ProcessRequest", deserialized.TargetMethod.Name);
            Assert.Equal("App.Controllers", deserialized.TargetMethod.DeclaringNamespace);
            Assert.Equal("HomeController", deserialized.TargetMethod.DeclaringType);
            Assert.True(deserialized.TargetMethod.IsSignatureTarget);
        }
    }
}
