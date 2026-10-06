using System;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;
using Module = Exceptionless.Models.Data.Module;

namespace Exceptionless.Tests.Serializer.Models {
    public class ErrorSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"modules":[],"message":null,"type":null,"code":null,"data":{},"inner":null,"stack_trace":[],"target_method":null}""";
        /* lang=json */
        private const string CompleteJson = """{"modules":[{"module_id":1,"name":"TestModule","version":"1.0.0","is_entry":true,"created_date":"2023-05-01T12:00:00Z","modified_date":"2023-05-02T12:00:00Z","data":{"PublicKeyToken":"b03f5f7f11d50a3a"}}],"message":"Test error message","type":"System.Exception","code":"1001","data":{"@ext":{"order_number":10}},"inner":{"message":"Inner error message","type":"System.ArgumentException","code":"2002","data":{},"inner":null,"stack_trace":[{"file_name":null,"line_number":20,"column":0,"is_signature_target":false,"declaring_namespace":null,"declaring_type":null,"name":"InnerMethodName","module_id":0,"data":{},"generic_arguments":[],"parameters":[]}],"target_method":null},"stack_trace":[{"file_name":"TestFile.cs","line_number":20,"column":5,"is_signature_target":true,"declaring_namespace":"TestNamespace","declaring_type":"TestClass","name":"InnerMethodName","module_id":1,"data":{"StackFrameKey":"StackFrameValue"},"generic_arguments":["T"],"parameters":[{"name":"param1","type":"System.String","type_namespace":"System","data":{"ParameterKey":"ParameterValue"},"generic_arguments":["U"]}]}],"target_method":null}""";
        /* lang=json */
        private const string ModulesOrderedJson = """{"modules":[{"module_id":1,"name":"TestModule","version":"1.0.0","is_entry":true,"created_date":"2023-05-01T12:00:00Z","modified_date":"2023-05-02T12:00:00Z","data":{"PublicKeyToken":"b03f5f7f11d50a3a"}}],"message":"Ordered","type":null,"code":null,"data":{},"inner":null,"stack_trace":[],"target_method":null}""";
        /* lang=json */
        private const string ExtraPropertiesJson = """{"modules":[],"message":"Test error message","type":"System.Exception","code":"1001","data":{"@ext":{"order_number":10}},"inner":null,"stack_trace":[],"target_method":null}""";
        /* lang=json */
        private const string CompactExtraPropertiesJson = """{"order_number":10}""";

        [Fact]
        public void Serialize_MinimalError_ProducesCorrectJson() {
            // Arrange
            var error = new Error();

            // Act
            string json = Serialize(error);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteError_ProducesCorrectJson() {
            // Arrange
            var error = CreateCompleteError();

            // Act
            string json = Serialize(error);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_Error_RoundTrips() {
            // Arrange
            var error = CreateCompleteError();

            // Act
            Error roundTripped = RoundTrip(error);

            // Assert
            Assert.Single(roundTripped.Modules);
            Assert.Equal(1, roundTripped.Modules[0].ModuleId);
            Assert.Equal("Test error message", roundTripped.Message);
            Assert.Equal("System.Exception", roundTripped.Type);
            Assert.Equal("1001", roundTripped.Code);
            Assert.Equal(CompactExtraPropertiesJson, roundTripped.Data[Error.KnownDataKeys.ExtraProperties]);
            Assert.Equal("Inner error message", roundTripped.Inner.Message);
            Assert.Equal(20, roundTripped.StackTrace[0].LineNumber);
            Assert.Equal("InnerMethodName", roundTripped.StackTrace[0].Name);
            Assert.Null(roundTripped.TargetMethod);
        }

        [Fact]
        public void Deserialize_Error_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            Error error = Deserialize<Error>(json);

            // Assert
            Assert.Single(error.Modules);
            Assert.Equal("TestModule", error.Modules[0].Name);
            Assert.Equal("Test error message", error.Message);
            Assert.Equal("System.Exception", error.Type);
            Assert.Equal("1001", error.Code);
            Assert.Equal(CompactExtraPropertiesJson, error.Data[Error.KnownDataKeys.ExtraProperties]);
            Assert.Equal("Inner error message", error.Inner.Message);
            Assert.Equal(20, error.Inner.StackTrace[0].LineNumber);
            Assert.Equal("TestFile.cs", error.StackTrace[0].FileName);
            Assert.Equal("T", error.StackTrace[0].GenericArguments[0]);
        }

        [Fact]
        public void Serialize_ErrorModules_ProduceDerivedPropertiesFirst() {
            // Arrange
            var error = new Error {
                Message = "Ordered",
                Modules = new ModuleCollection { CreateModule() }
            };

            // Act
            string json = Serialize(error);

            // Assert
            Assert.Equal(ModulesOrderedJson, json);
        }

        [Fact]
        public void Deserialize_ErrorExtraProperties_ConvertsJObjectToCompactString() {
            // Arrange
            const string json = ExtraPropertiesJson;

            // Act
            Error error = Deserialize<Error>(json);

            // Assert
            Assert.Equal(CompactExtraPropertiesJson, error.Data[Error.KnownDataKeys.ExtraProperties]);
        }

        private static Error CreateCompleteError() {
            return new Error {
                Modules = new ModuleCollection { CreateModule() },
                Message = "Test error message",
                Type = "System.Exception",
                Code = "1001",
                Data = {
                    [Error.KnownDataKeys.ExtraProperties] = new { OrderNumber = 10 }
                },
                Inner = new InnerError {
                    Message = "Inner error message",
                    Type = "System.ArgumentException",
                    Code = "2002",
                    StackTrace = new StackFrameCollection {
                        new StackFrame {
                            LineNumber = 20,
                            Name = "InnerMethodName"
                        }
                    }
                },
                StackTrace = new StackFrameCollection {
                    new StackFrame {
                        FileName = "TestFile.cs",
                        LineNumber = 20,
                        Column = 5,
                        IsSignatureTarget = true,
                        DeclaringNamespace = "TestNamespace",
                        DeclaringType = "TestClass",
                        Name = "InnerMethodName",
                        ModuleId = 1,
                        Data = {
                            ["StackFrameKey"] = "StackFrameValue"
                        },
                        GenericArguments = new GenericArguments { "T" },
                        Parameters = new ParameterCollection {
                            new Parameter {
                                Name = "param1",
                                Type = "System.String",
                                TypeNamespace = "System",
                                Data = {
                                    ["ParameterKey"] = "ParameterValue"
                                },
                                GenericArguments = new GenericArguments { "U" }
                            }
                        }
                    }
                }
            };
        }

        private static Module CreateModule() {
            return new Module {
                ModuleId = 1,
                Name = "TestModule",
                Version = "1.0.0",
                IsEntry = true,
                CreatedDate = new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2023, 5, 2, 12, 0, 0, DateTimeKind.Utc),
                Data = {
                    ["PublicKeyToken"] = "b03f5f7f11d50a3a"
                }
            };
        }
    }
}
