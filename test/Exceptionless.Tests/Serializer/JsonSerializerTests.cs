using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Xunit;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Exceptionless.Tests.Log;
using Exceptionless.Tests.Utility;
using Xunit.Abstractions;
using LogLevel = Exceptionless.Logging.LogLevel;
using Module = Exceptionless.Models.Data.Module;

namespace Exceptionless.Tests.Serializer {
    public class JsonSerializerTests {
        private readonly TestOutputWriter _writer;
        public JsonSerializerTests(ITestOutputHelper output) {
            _writer = new TestOutputWriter(output);
        }

        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_Event_IsValidJSON() {
            // Arrange
            var ev = new Event {
                Type = Event.KnownTypes.Log,
                Source = "SampleApp",
                Date = new DateTimeOffset(2023, 5, 2, 14, 30, 0, TimeSpan.Zero),
                Tags = { Event.KnownTags.Critical, "tag2" },
                Message = "An error occurred",
                Geo = "40.7128,-74.0060",
                Value = 42.0m,
                Count = 2,
                Data = {
                    ["FirstName"] = "Blake",
                    [Event.KnownDataKeys.Level] = "Warn",
                    [Event.KnownDataKeys.TraceLog] = new List<string> { "log 1" },
                    [Event.KnownDataKeys.UserDescription] = new UserDescription {
                        EmailAddress = "test@example.com",
                        Description = "Test user description"
                    }
                },
                ReferenceId = "ref123"
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(ev);

            // Assert
            Assert.Equal("{\"type\":\"log\",\"source\":\"SampleApp\",\"date\":\"2023-05-02T14:30:00+00:00\",\"tags\":[\"Critical\",\"tag2\"],\"message\":\"An error occurred\",\"geo\":\"40.7128,-74.0060\",\"value\":42.0,\"count\":2,\"data\":{\"FirstName\":\"Blake\",\"@level\":\"Warn\",\"@trace\":[\"log 1\"],\"@user_description\":{\"email_address\":\"test@example.com\",\"description\":\"Test user description\",\"data\":{}}},\"reference_id\":\"ref123\"}", json);
        }

        [Fact]
        public void Serialize_EnvironmentInfo_IsValidJSON() {
            // Arrange
            var environmentInfo = new EnvironmentInfo {
                ProcessorCount = 4,
                TotalPhysicalMemory = 8192,
                AvailablePhysicalMemory = 4096,
                CommandLine = "TestCommandLine",
                ProcessName = "TestProcess",
                ProcessId = "12345",
                ProcessMemorySize = 2048,
                ThreadName = "Thread",
                ThreadId = "67890",
                Architecture = "x64",
                OSName = "Windows",
                OSVersion = "10.0.19042",
                IpAddress = "192.168.1.1",
                MachineName = "Machine",
                InstallId = "InstallId",
                RuntimeVersion = "5.0.0",
                Data = { ["FrameworkDescription"] = "Test" }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(environmentInfo);

            // Assert
            Assert.Equal("{\"processor_count\":4,\"total_physical_memory\":8192,\"available_physical_memory\":4096,\"command_line\":\"TestCommandLine\",\"process_name\":\"TestProcess\",\"process_id\":\"12345\",\"process_memory_size\":2048,\"thread_name\":\"Thread\",\"thread_id\":\"67890\",\"architecture\":\"x64\",\"o_s_name\":\"Windows\",\"o_s_version\":\"10.0.19042\",\"ip_address\":\"192.168.1.1\",\"machine_name\":\"Machine\",\"install_id\":\"InstallId\",\"runtime_version\":\"5.0.0\",\"data\":{\"FrameworkDescription\":\"Test\"}}", json);
        }

        [Fact]
        public void Serialize_Error_IsValidJSON() {
            // Arrange
            var error = new Error {
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
                    StackTrace = new StackFrameCollection
                    {
                        new StackFrame { Name = "InnerMethodName", LineNumber = 20 }
                    }
                },
                StackTrace = new StackFrameCollection
                {
                    new StackFrame
                    {
                        FileName = "TestFile.cs",
                        LineNumber = 20,
                        Column = 5,
                        IsSignatureTarget = true,
                        DeclaringNamespace = "TestNamespace",
                        DeclaringType = "TestClass",
                        Name = "InnerMethodName",
                        ModuleId = 1,
                        Data = { ["StackFrameKey"] = "StackFrameValue" },
                        GenericArguments = new GenericArguments { "T" },
                        Parameters = new ParameterCollection
                        {
                            new Parameter
                            {
                                Name = "param1",
                                Type = "System.String",
                                TypeNamespace = "System",
                                Data = { ["ParameterKey"] = "ParameterValue" },
                                GenericArguments = new GenericArguments { "U" }
                            }
                        }
                    }
                },
                Modules = new ModuleCollection
                {
                    new Module
                    {
                        ModuleId = 1,
                        Name = "TestModule",
                        Version = "1.0.0",
                        IsEntry = true,
                        CreatedDate = new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                        ModifiedDate = new DateTime(2023, 5, 2, 12, 0, 0, DateTimeKind.Utc),
                        Data = { ["PublicKeyToken"] = "b03f5f7f11d50a3a" }
                    }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(error);

            // Assert
            Assert.Equal("{\"modules\":[{\"module_id\":1,\"name\":\"TestModule\",\"version\":\"1.0.0\",\"is_entry\":true,\"created_date\":\"2023-05-01T12:00:00Z\",\"modified_date\":\"2023-05-02T12:00:00Z\",\"data\":{\"PublicKeyToken\":\"b03f5f7f11d50a3a\"}}],\"message\":\"Test error message\",\"type\":\"System.Exception\",\"code\":\"1001\",\"data\":{\"@ext\":{\"OrderNumber\":10}},\"inner\":{\"message\":\"Inner error message\",\"type\":\"System.ArgumentException\",\"code\":\"2002\",\"data\":{},\"inner\":null,\"stack_trace\":[{\"file_name\":null,\"line_number\":20,\"column\":0,\"is_signature_target\":false,\"declaring_namespace\":null,\"declaring_type\":null,\"name\":\"InnerMethodName\",\"module_id\":0,\"data\":{},\"generic_arguments\":[],\"parameters\":[]}],\"target_method\":null},\"stack_trace\":[{\"file_name\":\"TestFile.cs\",\"line_number\":20,\"column\":5,\"is_signature_target\":true,\"declaring_namespace\":\"TestNamespace\",\"declaring_type\":\"TestClass\",\"name\":\"InnerMethodName\",\"module_id\":1,\"data\":{\"StackFrameKey\":\"StackFrameValue\"},\"generic_arguments\":[\"T\"],\"parameters\":[{\"name\":\"param1\",\"type\":\"System.String\",\"type_namespace\":\"System\",\"data\":{\"ParameterKey\":\"ParameterValue\"},\"generic_arguments\":[\"U\"]}]}],\"target_method\":null}", json);
        }

        [Fact]
        public void Serialize_ManualStackingInfo_IsValidJSON() {
            // Arrange
            var manualStackingInfo = new ManualStackingInfo {
                Title = "Test Title",
                SignatureData = new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(manualStackingInfo);

            // Assert
            Assert.Equal("{\"title\":\"Test Title\",\"signature_data\":{\"Key1\":\"Value1\",\"Key2\":\"Value2\"}}", json);
        }

        [Fact]
        public void Serialize_RequestInfo_IsValidJSON() {
            // Arrange
            var requestInfo = new RequestInfo {
                UserAgent = "Mozilla/5.0",
                HttpMethod = "GET",
                IsSecure = true,
                Host = "www.example.com",
                Port = 443,
                Path = "/test",
                Referrer = "https://www.google.com",
                ClientIpAddress = "192.168.1.1",
                Headers = new Dictionary<string, string[]>
                {
                    { "Content-Type", new[] { "application/json" } }
                },
                Cookies = new Dictionary<string, string>
                {
                    { "session", "abc123" }
                },
                QueryString = new Dictionary<string, string>
                {
                    { "q", "test" }
                },
                Data =
                {
                    [RequestInfo.KnownDataKeys.Browser] = "Mozilla Firefox",
                    [RequestInfo.KnownDataKeys.BrowserVersion] = "97.0",
                    [RequestInfo.KnownDataKeys.BrowserMajorVersion] = "97",
                    [RequestInfo.KnownDataKeys.Device] = "Desktop",
                    [RequestInfo.KnownDataKeys.OS] = "Windows",
                    [RequestInfo.KnownDataKeys.OSVersion] = "10.0",
                    [RequestInfo.KnownDataKeys.OSMajorVersion] = "10",
                    [RequestInfo.KnownDataKeys.IsBot] = "False"
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(requestInfo);

            // Assert
            Assert.Equal("{\"user_agent\":\"Mozilla/5.0\",\"http_method\":\"GET\",\"is_secure\":true,\"host\":\"www.example.com\",\"port\":443,\"path\":\"/test\",\"referrer\":\"https://www.google.com\",\"client_ip_address\":\"192.168.1.1\",\"headers\":{\"Content-Type\":[\"application/json\"]},\"cookies\":{\"session\":\"abc123\"},\"post_data\":null,\"query_string\":{\"q\":\"test\"},\"data\":{\"@browser\":\"Mozilla Firefox\",\"@browser_version\":\"97.0\",\"@browser_major_version\":\"97\",\"@device\":\"Desktop\",\"@os\":\"Windows\",\"@os_version\":\"10.0\",\"@os_major_version\":\"10\",\"@is_bot\":\"False\"}}", json);
        }

        [Fact]
        public void Serialize_SimpleError_IsValidJSON() {
            // Arrange
            var simpleError = new SimpleError {
                Message = "Test error message",
                Type = "System.Exception",
                StackTrace = "at TestClass.TestMethod()",
                Data =
                {
                    [SimpleError.KnownDataKeys.ExtraProperties] = new { OrderNumber = 10 }
                },
                Inner = new SimpleInnerError {
                    Message = "Inner error message",
                    Type = "System.NullReferenceException",
                    StackTrace = "at InnerTestClass.InnerTestMethod()"
                },
                Modules = new ModuleCollection
                {
                    new Module
                    {
                        ModuleId = 1,
                        Name = "TestModule",
                        Version = "1.0.0",
                        IsEntry = true,
                        CreatedDate = new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                        ModifiedDate = new DateTime(2023, 5, 2, 12, 0, 0, DateTimeKind.Utc),
                        Data = { ["PublicKeyToken"] = "b77a5c561934e089" }
                    }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(simpleError);

            // Assert
            Assert.Equal("{\"modules\":[{\"module_id\":1,\"name\":\"TestModule\",\"version\":\"1.0.0\",\"is_entry\":true,\"created_date\":\"2023-05-01T12:00:00Z\",\"modified_date\":\"2023-05-02T12:00:00Z\",\"data\":{\"PublicKeyToken\":\"b77a5c561934e089\"}}],\"message\":\"Test error message\",\"type\":\"System.Exception\",\"stack_trace\":\"at TestClass.TestMethod()\",\"data\":{\"@ext\":{\"OrderNumber\":10}},\"inner\":{\"message\":\"Inner error message\",\"type\":\"System.NullReferenceException\",\"stack_trace\":\"at InnerTestClass.InnerTestMethod()\",\"data\":{},\"inner\":null}}", json);
        }

        [Fact]
        public void Serialize_UserDescription_IsValidJSON() {
            // Arrange
            var userDescription = new UserDescription {
                EmailAddress = "test@example.com",
                Description = "Test user description"
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(userDescription);

            // Assert
            Assert.Equal("{\"email_address\":\"test@example.com\",\"description\":\"Test user description\",\"data\":{}}", json);
        }

        [Fact]
        public void Serialize_UserInfo_IsValidJSON() {
            // Arrange
            var userInfo = new UserInfo("123", "John Doe") {
                Data = {
                    { "Age", 30 },
                    { "City", "New York" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(userInfo);

            // Assert
            Assert.Equal("{\"identity\":\"123\",\"name\":\"John Doe\",\"data\":{\"Age\":30,\"City\":\"New York\"}}", json);
        }


        [Fact]
        public void Serialize_ClientConfiguration_IsValidJSON() {
            // Arrange
            var clientConfiguration = new ClientConfiguration {
                Version = 1,
                Settings =
                {
                    { "@@log:*", "Off" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(clientConfiguration);

            // Assert
            Assert.Equal("{\"version\":1,\"settings\":{\"@@log:*\":\"Off\"}}", json);
        }


        [Fact]
        public void Serialize_ModelWithExclusions_ShouldExcludeProperties() {
            // Arrange
            var data = new SampleModel {
                Date = DateTime.Now,
                Message = "Testing"
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data, new[] { nameof(SampleModel.Date), nameof(SampleModel.Number), nameof(SampleModel.Rating), nameof(SampleModel.Bool), nameof(SampleModel.DateOffset), nameof(SampleModel.Direction), nameof(SampleModel.Collection), nameof(SampleModel.Dictionary), nameof(SampleModel.Nested) });

            // Assert
            Assert.Equal("{\"Message\":\"Testing\"}", json);
        }

        [Fact]
        public void Serialize_ModelWithNestedExclusions_WillExcludeNestedProperties() {
            // Arrange
            var data = new NestedModel {
                Number = 1,
                Message = "Testing",
                Nested = new NestedModel {
                    Message = "Nested",
                    Number = 2
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data, new[] { nameof(NestedModel.Number) });

            // Assert
            Assert.Equal("{\"Message\":\"Testing\",\"Nested\":{\"Message\":\"Nested\",\"Nested\":null}}", json);
        }

        [Fact]
        public void Serialize_ModelWithNullValues_ShouldIncludeNullObjects() {
            // Arrange
            var data = new DefaultsModel();
            var serializer = GetSerializer();
            
            // Act
            string json = serializer.Serialize(data);

            // Assert
            Assert.Equal("{\"Number\":0,\"Bool\":false,\"Message\":null,\"Collection\":null,\"Dictionary\":null,\"DataDictionary\":null}", json);
        }

        [Fact]
        public void Serialize_ModelWithComplexPropertyNames_ShouldExcludeMultiWordProperties() {
            // Arrange
            var user = new User {
                FirstName = "John",
                LastName = "Doe",
                PasswordHash = "1234567890",
                Billing = new BillingInfo {
                    ExpirationMonth = 10,
                    ExpirationYear = 2020,
                    CardNumberRedacted = "1xxxxxxxx89",
                    EncryptedCardNumber = "9876543210"
                }
            };

            string[] exclusions = new[] { nameof(user.PasswordHash), nameof(user.Billing.CardNumberRedacted), nameof(user.Billing.EncryptedCardNumber) };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(user, exclusions, maxDepth: 2);

            // Assert
            Assert.Equal("{\"FirstName\":\"John\",\"LastName\":\"Doe\",\"Billing\":{\"ExpirationMonth\":10,\"ExpirationYear\":2020}}", json);
        }

        [Fact]
        public void Serialize_ModelWithDefaultValues_ShouldIncludeDefaultValues() {
            // Arrange
            var data = new SampleModel();
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data, new []{ nameof(SampleModel.Date), nameof(SampleModel.DateOffset) });

            // Assert
            Assert.Equal("{\"Number\":0,\"Rating\":0.0,\"Bool\":false,\"Direction\":\"North\",\"Message\":null,\"Dictionary\":null,\"Collection\":null,\"Nested\":null}", json);
            
            var model = serializer.Deserialize<SampleModel>(json);
            Assert.Equal(data.Number, model.Number);
            Assert.Equal(data.Bool, model.Bool);
            Assert.Equal(data.Message, model.Message);
            Assert.Equal(data.Collection, model.Collection);
            Assert.Equal(data.Dictionary, model.Dictionary);
            Assert.Equal(data.Nested, model.Nested);
        }

        [Fact]
        public void Serialize_ModelWithDataTypes_ShouldSerializeValues() {
            // Arrange
            var data = new SampleModel {
                Number = 1,
                Rating = 4.50m,
                Bool = true,
                Message = "test",
                Collection = new List<string> { "one" },
                Dictionary = new Dictionary<string, string> { { "key", "value" } },
                Date = DateTime.MaxValue,
                DateOffset = DateTimeOffset.MaxValue
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data);

            // Assert
            Assert.Equal("{\"Number\":1,\"Rating\":4.50,\"Bool\":true,\"Direction\":\"North\",\"Date\":\"9999-12-31T23:59:59.9999999\",\"Message\":\"test\",\"DateOffset\":\"9999-12-31T23:59:59.9999999+00:00\",\"Dictionary\":{\"key\":\"value\"},\"Collection\":[\"one\"],\"Nested\":null}", json);
            
            var model = serializer.Deserialize<SampleModel>(json);
            Assert.Equal(data.Number, model.Number);
            Assert.Equal(data.Bool, model.Bool);
            Assert.Equal(data.Message, model.Message);
            Assert.Equal(data.Collection, model.Collection);
            Assert.Equal(data.Dictionary, model.Dictionary);
            Assert.Equal(data.Nested, model.Nested);
        }

        [Fact]
        public void Serialize_NestedModel_ShouldRespectSetMaxDepth() {
            // Arrange
            var data = new NestedModel {
                Message = "Level 1",
                Nested = new NestedModel {
                    Message = "Level 2",
                    Nested = new NestedModel {
                        Message = "Level 3"
                    }
                }
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data, new[] { nameof(NestedModel.Number) }, maxDepth: 2);

            // Assert
            Assert.Equal("{\"Message\":\"Level 1\",\"Nested\":{\"Message\":\"Level 2\"}}", json);
        }

        [Fact]
        public void Serialize_ModelWithNullCollections_ShouldBeSerialized() {
            // Arrange
            var data = new DefaultsModel {
                Collection = null,
                Dictionary = null,
                DataDictionary = null
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data, new[] { nameof(DefaultsModel.Message), nameof(DefaultsModel.Bool), nameof(DefaultsModel.Number) });

            // Assert
            Assert.Equal("{\"Collection\":null,\"Dictionary\":null,\"DataDictionary\":null}", json);
        }

        [Fact]
        public void Serialize_ModelWithEmptyCollections_ShouldBeSerialized() {
            // Arrange
            var data = new DefaultsModel {
                Collection = new Collection<string>(),
                Dictionary = new Dictionary<string, string>(),
                DataDictionary = new DataDictionary()
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data, new[] { nameof(DefaultsModel.Message), nameof(DefaultsModel.Bool), nameof(DefaultsModel.Number) });

            // Assert
            Assert.Equal("{\"Collection\":[],\"Dictionary\":{},\"DataDictionary\":{}}", json);
        }

        [Fact]
        public void Serialize_ModelWithDictionaryValues_ShouldRespectDictionaryKeyNames() {
            // Arrange
            var data = new DefaultsModel {
                Collection = new Collection<string>() { "Collection" },
                Dictionary = new Dictionary<string, string>() { { "ItEm", "Value" } },
                DataDictionary = new DataDictionary() { { "ItEm", "Value" } }
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(data, new[] { nameof(DefaultsModel.Message), nameof(DefaultsModel.Bool), nameof(DefaultsModel.Number) });

            // Assert
            Assert.Equal("{\"Collection\":[\"Collection\"],\"Dictionary\":{\"ItEm\":\"Value\"},\"DataDictionary\":{\"ItEm\":\"Value\"}}", json);
        }

        [Fact]
        public void Serialize_ExceptionWithInnerException_ShouldSerializeDeepExceptionWithStackInformation() {
            // Arrange
            try {
                try {
                    try {
                        throw new ArgumentException("This is the innermost argument exception", "wrongArg");
                    }
                    catch (Exception e1) {
                        throw new TargetInvocationException("Target invocation exception.", e1);
                    }
                }
                catch (Exception e2) {
                    throw new TargetInvocationException("Outer Exception. This is some text of the outer exception.", e2);
                }
            }
            catch (Exception ex) {
                var client = CreateClient();
                var error = ex.ToErrorModel(client);
                var ev = new Event {
                    Data = {
                        [Event.KnownDataKeys.Error] = error
                    }
                };

                var serializer = GetSerializer();

                // Act
                string json = serializer.Serialize(ev);

                // Assert
                Assert.Contains($"\"line_number\":{error.Inner.Inner.StackTrace.Single().LineNumber}", json);
            }
        }

        [Fact]
        public void Serialize_PostDataConverter_ShouldHandleRequestInfoConverterPostDataAsJSON() {
            // Arrange
            var requestInfo = new RequestInfo {
                PostData = new { Age = 21 }
            };
            
            string[] propertiesToExclude = typeof(RequestInfo).GetProperties().Select(p => p.Name)
                .Except(new []{ nameof(RequestInfo.PostData) })
                .ToArray();

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(requestInfo, propertiesToExclude);

            // Assert
            Assert.Equal("{\"post_data\":{\"Age\":21}}", json);
        }

        [Fact]
        public void Deserialize_Event_ShouldDeserializeReferenceIds() {
            // Arrange
            var serializer = GetSerializer();

            // Act
            var ev = (Event)serializer.Deserialize(@"{""reference_id"": ""123"" }", typeof(Event));

            // Assert
            Assert.Equal("123", ev.ReferenceId);
        }

        private ExceptionlessClient CreateClient() {
            return new ExceptionlessClient(c => {
                c.UseLogger(new XunitExceptionlessLog(_writer) { MinimumLogLevel = LogLevel.Trace });
                c.ReadFromAttributes();
                c.UserAgent = "test-client/1.0.0.0";

                // Disable updating settings.
                c.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
            });
        }
    }

    public class NestedModel {
        public int Number { get; set; }
        public string Message { get; set; }
        public NestedModel Nested { get; set; }
    }

    public class SampleModel {
        public int Number { get; set; }
        public decimal Rating { get; set; }
        public bool Bool { get; set; }
        public Direction Direction { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public DateTimeOffset DateOffset { get; set; }
        public IDictionary<string, string> Dictionary { get; set; }
        public ICollection<string> Collection { get; set; }
        public SampleModel Nested { get; set; }
    }

    public class DefaultsModel {
        public int Number { get; set; }
        public bool Bool { get; set; }
        public string Message { get; set; }
        public ICollection<string> Collection { get; set; }
        public IDictionary<string, string> Dictionary { get; set; }
        public DataDictionary DataDictionary { get; set; }
    }

    public enum Direction {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public class User {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordHash { get; set; }
        public BillingInfo Billing { get; set; }
    }

    public class BillingInfo {
        public string CardNumberRedacted { get; set; }
        public string EncryptedCardNumber { get; set; }
        public int ExpirationMonth { get; set; }
        public int ExpirationYear { get; set; }
    }
}
