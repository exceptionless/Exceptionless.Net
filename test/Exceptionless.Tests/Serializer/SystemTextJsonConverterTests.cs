using System;
using System.Collections.Generic;
using System.Text.Json;
using Exceptionless.Models;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer {
    public class SystemTextJsonConverterTests {
        [Fact]
        public void DataDictionaryConverter_ReadsEveryPrimitiveNumberAndRawValueKind() {
            const string json = """
                {
                  "String": "value",
                  "Int": 42,
                  "Long": 2147483648,
                  "Decimal": 3.14,
                  "Double": 1.7976931348623157E+308,
                  "True": true,
                  "False": false,
                  "Null": null,
                  "Object": { "nested": true },
                  "Array": [1, 2]
                }
                """;

            DataDictionary result = JsonSerializer.Deserialize<DataDictionary>(json, CreateDataDictionaryOptions());

            Assert.Equal("value", result["String"]);
            Assert.Equal(42, Assert.IsType<int>(result["Int"]));
            Assert.Equal(2147483648L, Assert.IsType<long>(result["Long"]));
            Assert.Equal(3.14m, Assert.IsType<decimal>(result["Decimal"]));
            Assert.Equal(Double.MaxValue, Assert.IsType<double>(result["Double"]));
            Assert.True(Assert.IsType<bool>(result["True"]));
            Assert.False(Assert.IsType<bool>(result["False"]));
            Assert.Null(result["Null"]);
            using var objectDocument = JsonDocument.Parse(Assert.IsType<string>(result["Object"]));
            using var arrayDocument = JsonDocument.Parse(Assert.IsType<string>(result["Array"]));
            Assert.True(objectDocument.RootElement.GetProperty("nested").GetBoolean());
            Assert.Equal(2, arrayDocument.RootElement.GetArrayLength());
        }

        [Fact]
        public void DataDictionaryConverter_WritesRawLiteralInvalidAndCyclicValues() {
            var data = new DataDictionary {
                ["literal"] = /* lang=json */ "{\"literal\":true}",
                ["null"] = null
            };
            data.SetRawJson("raw", /* lang=json */ "{\"raw\":true}");
            data.SetRawJson("invalid", /* lang=json */ "{invalid");
            data["self"] = data;

            string json = JsonSerializer.Serialize(data, CreateDataDictionaryOptions());

            using var document = JsonDocument.Parse(json);
            Assert.Equal(JsonValueKind.String, document.RootElement.GetProperty("literal").ValueKind);
            Assert.Equal(JsonValueKind.Object, document.RootElement.GetProperty("raw").ValueKind);
            Assert.Equal("{invalid", document.RootElement.GetProperty("invalid").GetString());
            Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("null").ValueKind);
            Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("self").ValueKind);
        }

        [Fact]
        public void DataDictionaryConverter_RejectsNonObjectJson() {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<DataDictionary>("[]", CreateDataDictionaryOptions()));
        }

        [Fact]
        public void SettingsDictionaryConverter_ReadsAndWritesEverySupportedValueKind() {
            const string json = """
                {
                  "String": "value",
                  "Null": null,
                  "Number": 42,
                  "Boolean": true,
                  "Object": { "nested": true },
                  "Array": [1, 2]
                }
                """;
            JsonSerializerOptions options = CreateSettingsDictionaryOptions();

            SettingsDictionary result = JsonSerializer.Deserialize<SettingsDictionary>(json, options);
            Assert.Equal("value", result["String"]);
            Assert.Null(result["Null"]);
            Assert.Equal("42", result["Number"]);
            Assert.Equal("true", result["Boolean"]);
            using var objectDocument = JsonDocument.Parse(result["Object"]);
            using var arrayDocument = JsonDocument.Parse(result["Array"]);
            Assert.True(objectDocument.RootElement.GetProperty("nested").GetBoolean());
            Assert.Equal(2, arrayDocument.RootElement.GetArrayLength());

            string serialized = JsonSerializer.Serialize(result, options);
            using var document = JsonDocument.Parse(serialized);
            Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("Null").ValueKind);
            Assert.Equal("42", document.RootElement.GetProperty("Number").GetString());
        }

        [Fact]
        public void SettingsDictionaryConverter_RejectsNonObjectJson() {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<SettingsDictionary>("[]", CreateSettingsDictionaryOptions()));
        }

        [Fact]
        public void PostDataConverter_ReadsEverySupportedValueKind() {
            JsonSerializerOptions options = CreatePostDataOptions();

            Assert.Equal("value", JsonSerializer.Deserialize<object>("\"value\"", options));
            Assert.Equal(42L, Assert.IsType<long>(JsonSerializer.Deserialize<object>("42", options)));
            Assert.Equal(Double.MaxValue, Assert.IsType<double>(
                JsonSerializer.Deserialize<object>("1.7976931348623157E+308", options)));
            Assert.True(Assert.IsType<bool>(JsonSerializer.Deserialize<object>("true", options)));
            Assert.False(Assert.IsType<bool>(JsonSerializer.Deserialize<object>("false", options)));
            Assert.Null(JsonSerializer.Deserialize<object>("null", options));

            string objectJson = Assert.IsType<string>(
                JsonSerializer.Deserialize<object>("{" + "\"value\":true}", options));
            string arrayJson = Assert.IsType<string>(
                JsonSerializer.Deserialize<object>("[1,2]", options));
            Assert.Contains(Environment.NewLine, objectJson);
            Assert.Contains(Environment.NewLine, arrayJson);
            Assert.True(JsonDocument.Parse(objectJson).RootElement.GetProperty("value").GetBoolean());
        }

        [Fact]
        public void PostDataConverter_WritesTheRuntimeValue() {
            JsonSerializerOptions options = CreatePostDataOptions();
            object value = new Dictionary<string, object> {
                ["message"] = "hello",
                ["count"] = 42
            };

            string json = JsonSerializer.Serialize(value, options);

            using var document = JsonDocument.Parse(json);
            Assert.Equal("hello", document.RootElement.GetProperty("message").GetString());
            Assert.Equal(42, document.RootElement.GetProperty("count").GetInt32());
        }

        private static JsonSerializerOptions CreateDataDictionaryOptions() {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new DataDictionaryConverter());
            return options;
        }

        private static JsonSerializerOptions CreateSettingsDictionaryOptions() {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new SettingsDictionaryConverter());
            return options;
        }

        private static JsonSerializerOptions CreatePostDataOptions() {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PostDataConverter());
            return options;
        }
    }
}
