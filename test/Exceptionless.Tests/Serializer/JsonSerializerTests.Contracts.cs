using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Exceptionless.Models;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer {
    public partial class JsonSerializerTests {
        [Fact]
        public void Serialize_WithTypeLevelCollectionConverter_HonorsConverter() {
            // Arrange
            var serializer = new DefaultJsonSerializer(AdversarialJsonSerializerContext.Default);
            var collection = new ConvertedIntCollection { 1, 2 };

            // Act
            string json = serializer.Serialize(collection);

            // Assert
            Assert.Equal("\"converted-collection\"", json);
        }

        [Fact]
        public void Serialize_WithPropertyLevelCollectionConverter_HonorsConverter() {
            // Arrange
            var model = new PropertyConvertedCollectionContainer {
                Values = new List<int> { 1, 2 }
            };

            // Act
            string json = GetSerializer().Serialize(model);

            // Assert
            Assert.Equal("{\"values\":\"converted-property-collection\"}", json);
        }

        [Fact]
        public void Serialize_WithJsonObject_PreservesObjectShape() {
            // Arrange
            var model = new JsonObject {
                ["value"] = 42,
                ["nested"] = new JsonObject { ["enabled"] = true }
            };

            // Act
            string json = GetSerializer().Serialize(model);
            using var document = JsonDocument.Parse(json);

            // Assert
            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
            Assert.Equal(42, document.RootElement.GetProperty("value").GetInt32());
            Assert.True(document.RootElement.GetProperty("nested").GetProperty("enabled").GetBoolean());
        }

        [Fact]
        public void Serialize_WithJsonDom_AppliesNestedExclusionsAndDepth() {
            // Arrange
            var model = new JsonDomContainer {
                Payload = JsonNode.Parse(/* lang=json */ "{\"visible\":\"kept\",\"secret\":\"hidden\",\"nested\":{\"too_deep\":true}}")
            };

            // Act
            string json = GetSerializer().Serialize(model, new[] { "secret" }, maxDepth: 2);
            using var document = JsonDocument.Parse(json);
            JsonElement payload = document.RootElement.GetProperty("payload");

            // Assert
            Assert.Equal("kept", payload.GetProperty("visible").GetString());
            Assert.False(payload.TryGetProperty("secret", out _));
            Assert.False(payload.TryGetProperty("nested", out _));
        }

        [Fact]
        public void Serialize_WithGenericOnlyDictionary_PreservesDictionaryShape() {
            // Arrange
            var model = new GenericOnlyDictionary(new Dictionary<string, int> { ["value"] = 42 });
            Assert.False((object)model is System.Collections.IDictionary);

            // Act
            string json = GetSerializer().Serialize(model);
            using var document = JsonDocument.Parse(json);

            // Assert
            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
            Assert.Equal(42, document.RootElement.GetProperty("value").GetInt32());
        }

        [Fact]
        public void Serialize_WithDictionaryKeyPolicy_FiltersEmittedPropertyNames() {
            // Arrange
            var serializer = new DefaultJsonSerializer(new DefaultJsonTypeInfoResolver(), options =>
                options.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower);
            var model = new Dictionary<string, object> {
                ["VisibleValue"] = 1,
                ["SecretValue"] = 2,
                ["UnsupportedValue"] = typeof(string)
            };

            // Act
            string json = serializer.Serialize(model, new[] { "secret_value", "unsupported_value" }, continueOnSerializationError: false);

            // Assert
            Assert.Equal("{\"visible_value\":1}", json);
        }

        [Fact]
        public void Serialize_WithCustomDictionaryKeyConverter_FiltersEmittedPropertyNames() {
            // Arrange
            var serializer = new DefaultJsonSerializer(new DefaultJsonTypeInfoResolver(), options =>
                options.Converters.Add(new CustomDictionaryKeyConverter()));
            var model = new Dictionary<CustomDictionaryKey, int> {
                [new CustomDictionaryKey("visible")] = 1,
                [new CustomDictionaryKey("secret")] = 2
            };

            // Act
            string json = serializer.Serialize(model, new[] { "key-secret" });

            // Assert
            Assert.Equal("{\"key-visible\":1}", json);
        }

        [Fact]
        public void Serialize_WithCustomStringKeyConverter_FiltersEmittedPropertyNames() {
            // Arrange
            var serializer = new DefaultJsonSerializer(new DefaultJsonTypeInfoResolver(), options =>
                options.Converters.Add(new CustomStringKeyConverter()));
            var model = new Dictionary<string, int> { ["visible"] = 1, ["secret"] = 2 };

            // Act
            string json = serializer.Serialize(model, new[] { "key-secret" });

            // Assert
            Assert.Equal("{\"key-visible\":1}", json);
        }

        [Fact]
        public void Serialize_WithFailingDictionaryValue_OmitsOnlyFailedEntry() {
            // Arrange
            var model = new Dictionary<string, object> {
                ["kept"] = 1,
                ["unsupported"] = typeof(string)
            };

            // Act
            string json = GetSerializer().Serialize(model);
            System.Exception exception = Record.Exception(() =>
                GetSerializer().Serialize(model, continueOnSerializationError: false));

            // Assert
            Assert.Equal("{\"kept\":1}", json);
            Assert.NotNull(exception);
        }

        [Fact]
        public void Serialize_WithUndefinedJsonElement_OmitsFailedMember() {
            // Arrange
            var model = new UndefinedJsonElementContainer { Name = "kept" };

            // Act
            string json = GetSerializer().Serialize(model);

            // Assert
            Assert.Equal("{\"name\":\"kept\"}", json);
        }

        [Fact]
        public void Serialize_WithCustomPolymorphicCollection_UsesDeclaredElementContract() {
            // Arrange
            var model = new CustomPolymorphicCollectionContainer {
                Items = new CustomPolymorphicCollection {
                    new AdversarialDerivedModel { BaseValue = "base", DerivedValue = "derived" }
                }
            };
            var serializer = new DefaultJsonSerializer(AdversarialJsonSerializerContext.Default);

            // Act
            string json = serializer.Serialize(model);
            using var document = JsonDocument.Parse(json);
            JsonElement item = document.RootElement.GetProperty("items")[0];

            // Assert
            Assert.Equal("derived", item.GetProperty("$kind").GetString());
            Assert.Equal("base", item.GetProperty("base_value").GetString());
            Assert.Equal("derived", item.GetProperty("derived_value").GetString());
        }

        [Fact]
        public void Serialize_WithUnregisteredDerivedRuntimeType_UsesDeclaredBaseContract() {
            // Arrange
            var model = new DeclaredContractContainer {
                Payload = new UnregisteredDerivedModel {
                    BaseValue = "base",
                    DerivedValue = "derived"
                }
            };
            var serializer = new DefaultJsonSerializer(AdversarialJsonSerializerContext.Default);

            // Act
            string json = serializer.Serialize(model);

            // Assert
            Assert.Equal("{\"payload\":{\"base_value\":\"base\"}}", json);
            Assert.DoesNotContain("derived", json);
        }

        [Fact]
        public void Serialize_WithRoundTrippedRawJson_AppliesNestedExclusionsAndDepth() {
            // Arrange
            const string storedJson = /* lang=json */ """
                {"type":"log","data":{"payload":{"visible":"kept","secret":"hidden","nested":{"too_deep":true}}}}
                """;
            var serializer = GetSerializer();
            var model = (Event)serializer.Deserialize(storedJson, typeof(Event));

            // Act
            string json = serializer.Serialize(model, new[] { "secret" }, maxDepth: 3);
            using var document = JsonDocument.Parse(json);
            JsonElement payload = document.RootElement.GetProperty("data").GetProperty("payload");

            // Assert
            Assert.Equal("kept", payload.GetProperty("visible").GetString());
            Assert.False(payload.TryGetProperty("secret", out _));
            Assert.False(payload.TryGetProperty("nested", out _));
        }
    }

    [JsonConverter(typeof(ConvertedIntCollectionConverter))]
    public sealed class ConvertedIntCollection : List<int> { }

    public sealed class ConvertedIntCollectionConverter : JsonConverter<ConvertedIntCollection> {
        public override ConvertedIntCollection Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) => throw new System.NotSupportedException();
        public override void Write(Utf8JsonWriter writer, ConvertedIntCollection value, JsonSerializerOptions options) => writer.WriteStringValue("converted-collection");
    }

    public sealed class PropertyConvertedCollectionContainer {
        [JsonConverter(typeof(ConvertedIntListConverter))]
        public List<int> Values { get; set; }
    }

    public sealed class ConvertedIntListConverter : JsonConverter<List<int>> {
        public override List<int> Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) => throw new System.NotSupportedException();
        public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options) => writer.WriteStringValue("converted-property-collection");
    }

    public sealed class JsonDomContainer {
        public JsonNode Payload { get; set; }
    }

    public readonly struct CustomDictionaryKey {
        public CustomDictionaryKey(string value) {
            Value = value;
        }

        public string Value { get; }
    }

    public sealed class CustomDictionaryKeyConverter : JsonConverter<CustomDictionaryKey> {
        public override CustomDictionaryKey Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) =>
            new CustomDictionaryKey(reader.GetString());

        public override void Write(Utf8JsonWriter writer, CustomDictionaryKey value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);

        public override void WriteAsPropertyName(Utf8JsonWriter writer, CustomDictionaryKey value, JsonSerializerOptions options) =>
            writer.WritePropertyName($"key-{value.Value}");
    }

    public sealed class CustomStringKeyConverter : JsonConverter<string> {
        public override string Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) => reader.GetString();
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
        public override void WriteAsPropertyName(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
            writer.WritePropertyName($"key-{value}");
    }

    public sealed class UndefinedJsonElementContainer {
        public string Name { get; set; }
        public JsonElement Value { get; set; }
    }

    public sealed class GenericOnlyDictionary : IReadOnlyDictionary<string, int> {
        private readonly IReadOnlyDictionary<string, int> _values;

        public GenericOnlyDictionary(IReadOnlyDictionary<string, int> values) {
            _values = values;
        }

        public int this[string key] => _values[key];
        public IEnumerable<string> Keys => _values.Keys;
        public IEnumerable<int> Values => _values.Values;
        public int Count => _values.Count;
        public bool ContainsKey(string key) => _values.ContainsKey(key);
        public bool TryGetValue(string key, out int value) => _values.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _values.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class CustomPolymorphicCollectionContainer {
        public CustomPolymorphicCollection Items { get; set; }
    }

    public sealed class CustomPolymorphicCollection : List<AdversarialBaseModel> { }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    [JsonDerivedType(typeof(AdversarialDerivedModel), "derived")]
    public abstract class AdversarialBaseModel {
        public string BaseValue { get; set; }
    }

    public sealed class AdversarialDerivedModel : AdversarialBaseModel {
        public string DerivedValue { get; set; }
    }

    public sealed class DeclaredContractContainer {
        public DeclaredContractBaseModel Payload { get; set; }
    }

    public class DeclaredContractBaseModel {
        public string BaseValue { get; set; }
    }

    public sealed class UnregisteredDerivedModel : DeclaredContractBaseModel {
        public string DerivedValue { get; set; }
    }

    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Metadata,
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IncludeFields = true,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(ConvertedIntCollection))]
    [JsonSerializable(typeof(CustomPolymorphicCollectionContainer))]
    [JsonSerializable(typeof(CustomPolymorphicCollection))]
    [JsonSerializable(typeof(AdversarialBaseModel))]
    [JsonSerializable(typeof(AdversarialDerivedModel))]
    [JsonSerializable(typeof(DeclaredContractContainer))]
    [JsonSerializable(typeof(DeclaredContractBaseModel))]
    internal partial class AdversarialJsonSerializerContext : JsonSerializerContext { }
}
