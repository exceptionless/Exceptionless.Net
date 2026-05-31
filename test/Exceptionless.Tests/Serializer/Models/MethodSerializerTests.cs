using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class MethodSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteMethod_ProducesSnakeCaseJson() {
            // Arrange
            var method = new Method {
                IsSignatureTarget = true,
                DeclaringNamespace = "MyApp.Services",
                DeclaringType = "UserService",
                Name = "GetUserAsync",
                ModuleId = 3,
                Data = { ["ILOffset"] = 42 },
                GenericArguments = new GenericArguments { "TUser", "TResult" },
                Parameters = new ParameterCollection {
                    new Parameter { Name = "userId", Type = "System.Int32", TypeNamespace = "System" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(method);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "is_signature_target", "declaring_namespace", "declaring_type",
                "name", "module_id", "data", "generic_arguments", "parameters");
            SerializerContractAssertions.ExcludesProperties(json,
                "IsSignatureTarget", "DeclaringNamespace", "DeclaringType", "ModuleId",
                "GenericArguments", "Parameters");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Method {
                IsSignatureTarget = true,
                DeclaringNamespace = "App.Controllers",
                DeclaringType = "ApiController",
                Name = "ProcessRequest",
                ModuleId = 1,
                GenericArguments = new GenericArguments { "T" },
                Parameters = new ParameterCollection {
                    new Parameter {
                        Name = "request",
                        Type = "HttpRequest",
                        TypeNamespace = "Microsoft.AspNetCore.Http"
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Method)serializer.Deserialize(json, typeof(Method));

            // Assert
            Assert.True(deserialized.IsSignatureTarget);
            Assert.Equal("App.Controllers", deserialized.DeclaringNamespace);
            Assert.Equal("ApiController", deserialized.DeclaringType);
            Assert.Equal("ProcessRequest", deserialized.Name);
            Assert.Equal(1, deserialized.ModuleId);
            Assert.Single(deserialized.GenericArguments);
            Assert.Equal("T", deserialized.GenericArguments[0]);
            Assert.Single(deserialized.Parameters);
            Assert.Equal("request", deserialized.Parameters[0].Name);
        }

        [Fact]
        public void Deserialize_WithMultipleGenericArguments_PreservesAll() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Method {
                Name = "ConvertAll",
                GenericArguments = new GenericArguments { "TInput", "TOutput", "TContext" }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Method)serializer.Deserialize(json, typeof(Method));

            // Assert
            Assert.Equal(3, deserialized.GenericArguments.Count);
            Assert.Equal("TInput", deserialized.GenericArguments[0]);
            Assert.Equal("TOutput", deserialized.GenericArguments[1]);
            Assert.Equal("TContext", deserialized.GenericArguments[2]);
        }

        [Fact]
        public void Deserialize_WithMultipleParameters_PreservesAll() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Method {
                Name = "Execute",
                Parameters = new ParameterCollection {
                    new Parameter { Name = "input", Type = "String", TypeNamespace = "System" },
                    new Parameter { Name = "options", Type = "Options", TypeNamespace = "App" },
                    new Parameter { Name = "cancellationToken", Type = "CancellationToken", TypeNamespace = "System.Threading" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Method)serializer.Deserialize(json, typeof(Method));

            // Assert
            Assert.Equal(3, deserialized.Parameters.Count);
            Assert.Equal("input", deserialized.Parameters[0].Name);
            Assert.Equal("options", deserialized.Parameters[1].Name);
            Assert.Equal("cancellationToken", deserialized.Parameters[2].Name);
        }

        [Fact]
        public void Deserialize_MinimalMethod_PreservesBasicProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Method { Name = "SimpleMethod" };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Method)serializer.Deserialize(json, typeof(Method));

            // Assert
            Assert.Equal("SimpleMethod", deserialized.Name);
            Assert.False(deserialized.IsSignatureTarget);
            Assert.Null(deserialized.DeclaringNamespace);
            Assert.Null(deserialized.DeclaringType);
        }

        [Fact]
        public void Deserialize_WithData_PreservesDataDictionary() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Method {
                Name = "Test",
                Data = {
                    ["ILOffset"] = 128,
                    ["NativeOffset"] = 256
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Method)serializer.Deserialize(json, typeof(Method));

            // Assert
            Assert.NotNull(deserialized.Data);
            Assert.Equal(2, deserialized.Data.Count);
        }
    }
}
