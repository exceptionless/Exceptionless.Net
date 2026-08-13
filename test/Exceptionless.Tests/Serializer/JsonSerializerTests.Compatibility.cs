using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;
using Exceptionless.Json;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;

namespace Exceptionless.Tests.Serializer {
    public partial class JsonSerializerTests {
        [Fact]
        public void Serialize_WithConverterAndExtensionData_WritesExpectedJson() {
            // Arrange
            var model = new ExtensionDataModel {
                Version = new Version(1, 2, 3),
                ExtensionData = new System.Text.Json.Nodes.JsonObject {
                    ["node-key"] = 5
                }
            };

            // Act
            string json = GetSerializer().Serialize(model);

            // Assert
            Assert.Equal("{\"version\":\"1.2.3\",\"node-key\":5}", json);
        }

        [Fact]
        public void Serialize_WithExceptionlessIgnoreAttribute_OmitsDecoratedMember() {
            // Arrange
            var model = new CompatibilityIgnoreModel { Name = "Ada", Secret = "do-not-send" };

            // Act
            string json = GetSerializer().Serialize(model);

            // Assert
            Assert.Contains("\"name\":\"Ada\"", json);
            Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("do-not-send", json);
        }

        [Fact]
        public void Serialize_WithFailingEnumerator_ClosesCollectionAndKeepsWrittenItems() {
            // Arrange
            var model = new PartiallyEnumerableModel {
                Name = "kept",
                Values = new ThrowingEnumerable()
            };

            // Act
            string json = GetSerializer().Serialize(model);

            using var document = System.Text.Json.JsonDocument.Parse(json);

            // Assert
            Assert.Equal("kept", document.RootElement.GetProperty("name").GetString());
            Assert.Equal(1, document.RootElement.GetProperty("values").GetArrayLength());
            Assert.Equal(1, document.RootElement.GetProperty("values")[0].GetInt32());
        }

        [Fact]
        public void Serialize_WithGuardedPropertyConverter_AppliesDepthAndCycleChecks() {
            // Arrange
            var depthModel = new GuardedConverterModel { Value = new object() };
            var cycleModel = new GuardedConverterModel();
            cycleModel.Value = cycleModel;

            // Act
            string depthJson = GetSerializer().Serialize(depthModel, maxDepth: 1, continueOnSerializationError: false);
            string cycleJson = GetSerializer().Serialize(cycleModel, continueOnSerializationError: false);

            // Assert
            Assert.Equal("{}", depthJson);
            Assert.Equal("{}", cycleJson);
        }

        [Fact]
        public void Serialize_WithInitializedSourceGeneratedContext_OmitsCompatibilityMembers() {
            // Arrange
            var model = new CompatibilityIgnoreModel {
                Name = "Ada",
                Secret = "property-secret",
                SecretField = "field-secret"
            };
            var context = CompatibilityJsonSerializerContext.Default;
            string contextJson = System.Text.Json.JsonSerializer.Serialize(model, context.CompatibilityIgnoreModel);
            var serializer = new DefaultJsonSerializer(context);

            // Act
            string json = null;
            Exception exception = Record.Exception(() => json = serializer.Serialize(model));

            // Assert
            Assert.Contains("property-secret", contextJson);
            Assert.Contains("field-secret", contextJson);
            Assert.Null(exception);
            Assert.Equal("{\"name\":\"Ada\"}", json);
        }

        [Fact]
        public void Serialize_WithNamedFloatingPointValues_PreservesValues() {
            // Arrange
            var model = new FloatingPointModel {
                NotANumber = Double.NaN,
                PositiveInfinity = Double.PositiveInfinity,
                NegativeInfinity = Double.NegativeInfinity
            };

            // Act
            string json = GetSerializer().Serialize(model);

            using var document = System.Text.Json.JsonDocument.Parse(json);

            // Assert
            Assert.Equal("NaN", document.RootElement.GetProperty("not_a_number").GetString());
            Assert.Equal("Infinity", document.RootElement.GetProperty("positive_infinity").GetString());
            Assert.Equal("-Infinity", document.RootElement.GetProperty("negative_infinity").GetString());
        }

        [Fact]
        public void Serialize_WithNestedDictionaryAtDepthLimit_ProducesValidJson() {
            // Arrange
            // Regression test: When a dictionary contains a nested complex object and
            // the depth limit is reached, WriteValue returned without writing a value
            // after the property name was already written. The error was silently swallowed
            // by continueOnSerializationError, falling back to full serialization (violating
            // the depth limit). This means depth limits don't work for dictionaries.
            var serializer = GetSerializer();
            var dict = new Dictionary<string, object> {
                { "simple", "hello" },
                { "nested", new Dictionary<string, object> { { "deep", "value" } } }
            };

            // Act
            string json = serializer.Serialize(dict, null, maxDepth: 1);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"simple\":\"hello\"", json);
            // The nested dictionary should NOT appear at depth (depth limit should be respected)
            Assert.DoesNotContain("\"deep\"", json);
        }

        [Fact]
        public void Serialize_WithNestedExclusions_AppliesExclusionsInsideCollections() {
            // Arrange
            var data = new DataDictionary {
                ["users"] = new List<SensitiveModel> {
                    new SensitiveModel { Name = "Ada", Secret = "do-not-send" }
                },
                ["secret"] = "also-do-not-send"
            };

            // Act
            string json = GetSerializer().Serialize(data, new[] { "secret" });

            // Assert
            Assert.Contains("\"name\":\"Ada\"", json);
            Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("do-not-send", json);
        }

        [Fact]
        public void Serialize_WithNonNullValuesAtDepthLimit_UsesDeclaredTypes() {
            // Arrange
            var model = new DeclaredDepthModel {
                Message = "kept",
                BoxedMessage = "omitted"
            };

            // Act
            string json = GetSerializer().Serialize(model, maxDepth: 1);

            // Assert
            Assert.Equal("{\"message\":\"kept\"}", json);
        }

        [Fact]
        public void Serialize_WithNullExtensionAndSettingsValues_WritesValidJson() {
            // Arrange
            var extensionData = new ExtensionDataModel {
                Version = new Version(1, 2, 3)
            };
            var settings = new SettingsDictionary {
                ["null-value"] = null,
                ["value"] = "kept"
            };

            // Act
            var extensionDataJson = GetSerializer().Serialize(extensionData);
            var settingsJson = GetSerializer().Serialize(settings);

            // Assert
            Assert.Equal("{\"version\":\"1.2.3\"}", extensionDataJson);
            Assert.Equal("{\"value\":\"kept\",\"null-value\":null}", settingsJson);
        }

        [Fact]
        public void Serialize_WithNullValuesAtDepthLimit_UsesDeclaredTypes() {
            // Arrange
            var model = new NullableDepthModel();

            // Act
            string json = GetSerializer().Serialize(model, maxDepth: 1);

            // Assert
            Assert.Equal("{\"message\":null}", json);
        }

        [Fact]
        public void Serialize_WithPrimitiveAtDepthLimit_IncludesValue() {
            // Arrange
            var model = new UriModel { Address = new Uri("https://exceptionless.com") };

            // Act
            string json = GetSerializer().Serialize(model, maxDepth: 1);

            // Assert
            Assert.Equal("{\"address\":\"https://exceptionless.com\"}", json);
        }

        [Fact]
        public void Serialize_WithPublicField_IncludesField() {
            // Arrange
            var model = new PublicFieldModel { Name = "Ada" };

            // Act
            string json = GetSerializer().Serialize(model);

            // Assert
            Assert.Equal("{\"name\":\"Ada\"}", json);
        }

        [Fact]
        public void Serialize_WithReferenceLoop_IgnoresLoopAndPreservesEvent() {
            // Arrange
            var ev = new Event { Type = Event.KnownTypes.Log, Source = "cycle-test" };
            ev.Data["cycle"] = ev;
            ev.Data["items"] = new List<object> { "kept", ev };

            // Act
            string json = GetSerializer().Serialize(ev);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"source\":\"cycle-test\"", json);
            Assert.Contains("\"kept\"", json);
            Assert.DoesNotContain("\"cycle\"", json);
        }

        [Fact]
        public void Serialize_WithRoundTrippedDataDictionary_PreservesObjectStructure() {
            // Arrange
            // Regression test: After roundtripping through storage (serialize → deserialize),
            // complex objects in Data become JSON strings. When re-serialized for API submission,
            // they must be emitted as JSON objects (not escaped strings).
            var serializer = GetSerializer();

            // Simulate what plugins do: store an object directly in Data
            var ev = new Event {
                Type = Event.KnownTypes.Error,
                Data = {
                    [Event.KnownDataKeys.Error] = new Error {
                        Message = "Test error",
                        Type = "System.Exception"
                    },
                    [Event.KnownDataKeys.EnvironmentInfo] = new EnvironmentInfo {
                        ProcessorCount = 8,
                        OSName = "Windows",
                        OSVersion = "10.0"
                    }
                }
            };

            // Act
            string storageJson = serializer.Serialize(ev);
            var deserialized = (Event)serializer.Deserialize(storageJson, typeof(Event));
            string apiJson = serializer.Serialize(deserialized);

            // Assert
            Assert.Contains("\"@error\":", storageJson);
            Assert.DoesNotContain("\"@error\":\"", storageJson);
            Assert.IsType<string>(deserialized.Data[Event.KnownDataKeys.Error]);
            Assert.IsType<string>(deserialized.Data[Event.KnownDataKeys.EnvironmentInfo]);
            Assert.DoesNotContain("\"@error\":\"", apiJson); // Must NOT be an escaped string
            Assert.DoesNotContain("\"@environment\":\"", apiJson);
            // Verify roundtripped JSON preserves the object structure
            Assert.Contains("\"message\":\"Test error\"", apiJson);
            Assert.Contains("\"o_s_name\":\"Windows\"", apiJson);
        }

        [Fact]
        public void Serialize_WithSettingsDictionary_AppliesKeyExclusions() {
            // Arrange
            var settings = new SettingsDictionary {
                ["visible"] = "kept",
                ["secret"] = "do-not-send"
            };

            // Act
            string json = GetSerializer().Serialize(settings, new[] { "secret" });

            // Assert
            Assert.Equal("{\"visible\":\"kept\"}", json);
        }

        [Fact]
        public void Serialize_WithStopOnError_PropagatesUnsupportedMemberError() {
            // Arrange
            var model = new PartiallyUnsupportedModel {
                Name = "Ada",
                UnsupportedType = typeof(PartiallyUnsupportedModel)
            };

            // Act
            Exception exception = Record.Exception(() => GetSerializer().Serialize(model, continueOnSerializationError: false));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void Serialize_WithSystemTextJsonPropertyContracts_HonorsContracts() {
            // Arrange
            var model = new SystemTextJsonContractModel {
                Name = "Ada",
                Converted = "value",
                Number = 42,
                ExtensionData = new Dictionary<string, object> {
                    ["extension-key"] = true
                }
            };

            // Act
            string json = GetSerializer().Serialize(model);

            // Assert
            Assert.Equal("{\"renamed\":\"Ada\",\"converted\":\"prefix:value\",\"number\":\"42\",\"extension-key\":true}", json);
        }

        [Fact]
        public void Serialize_WithThrowingGetterOrPredicate_OmitsFailedMembers() {
            // Arrange
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(typeInfo => {
                if (typeInfo.Type != typeof(ThrowingContractModel))
                    return;

                typeInfo.Properties.Single(property => property.Name == "conditional").ShouldSerialize =
                    (_, _) => throw new InvalidOperationException("Predicate failed.");
            });
            var serializer = new DefaultJsonSerializer(resolver);

            // Act
            string json = serializer.Serialize(new ThrowingContractModel());
            Exception exception = Record.Exception(() =>
                serializer.Serialize(new ThrowingContractModel(), continueOnSerializationError: false));

            // Assert
            Assert.Equal("{\"name\":\"kept\"}", json);
            Assert.NotNull(exception);
        }

        [Fact]
        public void Serialize_WithThrowingPropertyConverter_OmitsFailedMember() {
            // Arrange
            var model = new ThrowingConverterModel {
                Name = "kept",
                Converted = "throws"
            };

            // Act
            string json = GetSerializer().Serialize(model);
            Exception exception = Record.Exception(() => GetSerializer().Serialize(model, continueOnSerializationError: false));

            // Assert
            Assert.Equal("{\"name\":\"kept\"}", json);
            Assert.NotNull(exception);
        }

        [Fact]
        public void Serialize_WithUnsupportedMember_SkipsMemberAndPreservesExclusions() {
            // Arrange
            var model = new PartiallyUnsupportedModel {
                Name = "Ada",
                Secret = "do-not-send",
                UnsupportedType = typeof(PartiallyUnsupportedModel)
            };

            // Act
            string json = GetSerializer().Serialize(model, new[] { nameof(PartiallyUnsupportedModel.Secret) });

            // Assert
            Assert.Equal("{\"name\":\"Ada\"}", json);
        }

        [Fact]
        public void SerializeStream_WithDefaultDepthLimit_TruncatesNestedModel() {
            // Arrange
            var root = new NestedModel { Message = "Level 1" };
            var current = root;
            for (int level = 2; level <= 12; level++) {
                current.Nested = new NestedModel { Message = $"Level {level}" };
                current = current.Nested;
            }

            // Act
            using var stream = new System.IO.MemoryStream();
            ((IStorageSerializer)GetSerializer()).Serialize(root, stream);
            stream.Position = 0;
            using var document = System.Text.Json.JsonDocument.Parse(stream);

            int serializedLevels = 1;
            var serializedLevel = document.RootElement;
            while (serializedLevel.TryGetProperty("nested", out var nested)) {
                serializedLevels++;
                serializedLevel = nested;
            }

            // Assert
            Assert.Equal(10, serializedLevels);
        }

        [Fact]
        public void SerializeStream_WithReferenceLoop_CompletesWithoutRecursion() {
            // Arrange
            var ev = new Event { Type = Event.KnownTypes.Log, Source = "stream-cycle-test" };
            ev.Data["cycle"] = ev;

            // Act
            using var stream = new System.IO.MemoryStream();
            new DefaultJsonSerializer().Serialize(ev, stream);
            stream.Position = 0;

            using var document = System.Text.Json.JsonDocument.Parse(stream);

            // Assert
            Assert.Equal("stream-cycle-test", document.RootElement.GetProperty("source").GetString());
        }
    }

    public class SensitiveModel {
        public string Name { get; set; }
        public string Secret { get; set; }
    }

    public class CompatibilityIgnoreModel {
        public string Name { get; set; }

        [ExceptionlessIgnore]
        public string Secret { get; set; }

        [ExceptionlessIgnore]
        public string SecretField;
    }

    public class PublicFieldModel {
        public string Name;
    }

    public class FloatingPointModel {
        public double NotANumber { get; set; }
        public double PositiveInfinity { get; set; }
        public double NegativeInfinity { get; set; }
    }

    public class PartiallyUnsupportedModel {
        public string Name { get; set; }
        public string Secret { get; set; }
        public Type UnsupportedType { get; set; }
    }

    public class UriModel {
        public Uri Address { get; set; }
    }

    public class NullableDepthModel {
        public string Message { get; set; }
        public NestedModel Nested { get; set; }
    }

    public class DeclaredDepthModel {
        public string Message { get; set; }
        public object BoxedMessage { get; set; }
    }

    public class SystemTextJsonContractModel {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string IgnoredNull { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int IgnoredDefault { get; set; }

        [JsonPropertyName("renamed")]
        public string Name { get; set; }

        [JsonConverter(typeof(PrefixStringConverter))]
        public string Converted { get; set; }

        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        public int Number { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }

    public class ThrowingConverterModel {
        public string Name { get; set; }

        [JsonConverter(typeof(ThrowingStringConverter))]
        public string Converted { get; set; }
    }

    public class ThrowingContractModel {
        public string Name { get; set; } = "kept";
        public string ThrowingGetter => throw new InvalidOperationException("Getter failed.");
        public string Conditional { get; set; } = "omitted";
    }

    public class ExtensionDataModel {
        public Version Version { get; set; }

        [JsonExtensionData]
        public System.Text.Json.Nodes.JsonObject ExtensionData { get; set; }
    }

    public class GuardedConverterModel {
        [JsonConverter(typeof(ThrowingObjectConverter))]
        public object Value { get; set; }
    }

    public sealed class PrefixStringConverter : JsonConverter<string> {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString();
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue($"prefix:{value}");
    }

    public sealed class ThrowingStringConverter : JsonConverter<string> {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString();
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => throw new InvalidOperationException("Converter failed.");
    }

    public sealed class ThrowingObjectConverter : JsonConverter<object> {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) => throw new InvalidOperationException("Converter should not run.");
    }

    public class PartiallyEnumerableModel {
        public string Name { get; set; }
        public IEnumerable<int> Values { get; set; }
    }

    public class ThrowingEnumerable : IEnumerable<int> {
        public IEnumerator<int> GetEnumerator() {
            yield return 1;
            throw new InvalidOperationException("Enumeration failed.");
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Metadata,
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IncludeFields = true,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(CompatibilityIgnoreModel))]
    internal partial class CompatibilityJsonSerializerContext : JsonSerializerContext { }
}
