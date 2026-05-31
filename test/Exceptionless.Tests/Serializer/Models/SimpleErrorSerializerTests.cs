using System;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;
using Module = Exceptionless.Models.Data.Module;

namespace Exceptionless.Tests.Serializer.Models {
    public class SimpleErrorSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"modules":[],"message":null,"type":null,"stack_trace":null,"data":{},"inner":null}""";
        /* lang=json */
        private const string CompleteJson = """{"modules":[{"module_id":1,"name":"TestModule","version":"1.0.0","is_entry":true,"created_date":"2023-05-01T12:00:00Z","modified_date":"2023-05-02T12:00:00Z","data":{"PublicKeyToken":"b77a5c561934e089"}}],"message":"Test error message","type":"System.Exception","stack_trace":"at TestClass.TestMethod()","data":{"@ext":{"OrderNumber":10}},"inner":{"message":"Inner error message","type":"System.NullReferenceException","stack_trace":"at InnerTestClass.InnerTestMethod()","data":{},"inner":null}}""";
        /* lang=json */
        private const string CompactExtraPropertiesJson = """{"OrderNumber":10}""";

        [Fact]
        public void Serialize_MinimalSimpleError_ProducesCorrectJson() {
            // Arrange
            var error = new SimpleError();

            // Act
            string json = Serialize(error);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteSimpleError_ProducesCorrectJson() {
            // Arrange
            var error = CreateCompleteSimpleError();

            // Act
            string json = Serialize(error);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_SimpleError_RoundTrips() {
            // Arrange
            var error = CreateCompleteSimpleError();

            // Act
            SimpleError roundTripped = RoundTrip(error);

            // Assert
            Assert.Single(roundTripped.Modules);
            Assert.Equal("TestModule", roundTripped.Modules[0].Name);
            Assert.Equal("Test error message", roundTripped.Message);
            Assert.Equal("System.Exception", roundTripped.Type);
            Assert.Equal("at TestClass.TestMethod()", roundTripped.StackTrace);
            Assert.Equal(CompactExtraPropertiesJson, roundTripped.Data[SimpleError.KnownDataKeys.ExtraProperties]);
            Assert.Equal("Inner error message", roundTripped.Inner.Message);
            Assert.Equal("System.NullReferenceException", roundTripped.Inner.Type);
        }

        [Fact]
        public void Deserialize_SimpleError_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            SimpleError error = Deserialize<SimpleError>(json);

            // Assert
            Assert.Single(error.Modules);
            Assert.Equal("b77a5c561934e089", error.Modules[0].Data["PublicKeyToken"]);
            Assert.Equal("Test error message", error.Message);
            Assert.Equal("System.Exception", error.Type);
            Assert.Equal("at TestClass.TestMethod()", error.StackTrace);
            Assert.Equal(CompactExtraPropertiesJson, error.Data[SimpleError.KnownDataKeys.ExtraProperties]);
            Assert.Equal("Inner error message", error.Inner.Message);
            Assert.Equal("at InnerTestClass.InnerTestMethod()", error.Inner.StackTrace);
        }

        private static SimpleError CreateCompleteSimpleError() {
            return new SimpleError {
                Modules = new ModuleCollection {
                    new Module {
                        ModuleId = 1,
                        Name = "TestModule",
                        Version = "1.0.0",
                        IsEntry = true,
                        CreatedDate = new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                        ModifiedDate = new DateTime(2023, 5, 2, 12, 0, 0, DateTimeKind.Utc),
                        Data = {
                            ["PublicKeyToken"] = "b77a5c561934e089"
                        }
                    }
                },
                Message = "Test error message",
                Type = "System.Exception",
                StackTrace = "at TestClass.TestMethod()",
                Data = {
                    [SimpleError.KnownDataKeys.ExtraProperties] = new { OrderNumber = 10 }
                },
                Inner = new SimpleInnerError {
                    Message = "Inner error message",
                    Type = "System.NullReferenceException",
                    StackTrace = "at InnerTestClass.InnerTestMethod()"
                }
            };
        }
    }
}
