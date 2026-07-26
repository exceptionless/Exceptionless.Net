using System;
using System.Collections.Generic;
using System.Text.Json;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer {
    public class JsonElementValueConverterTests {
        [Fact]
        public void Convert_MapsEverySupportedJsonValueKind() {
            const string json = """
                {
                  "Int": 42,
                  "Long": 2147483648,
                  "Decimal": 3.14,
                  "Double": 1.7976931348623157E+308,
                  "True": true,
                  "False": false,
                  "Null": null,
                  "Array": ["value", 7],
                  "Object": { "Nested": "yes" }
                }
                """;

            using var document = JsonDocument.Parse(json);
            var result = Assert.IsType<Dictionary<string, object>>(
                JsonElementValueConverter.Convert(document.RootElement, parseDates: false));

            Assert.Equal(42, Assert.IsType<int>(result["int"]));
            Assert.Equal(2147483648L, Assert.IsType<long>(result["long"]));
            Assert.Equal(3.14m, Assert.IsType<decimal>(result["decimal"]));
            Assert.Equal(Double.MaxValue, Assert.IsType<double>(result["double"]));
            Assert.True(Assert.IsType<bool>(result["true"]));
            Assert.False(Assert.IsType<bool>(result["false"]));
            Assert.Null(result["null"]);

            var array = Assert.IsType<List<object>>(result["array"]);
            Assert.Equal("value", array[0]);
            Assert.Equal(7, array[1]);

            var nested = Assert.IsType<Dictionary<string, object>>(result["object"]);
            Assert.Equal("yes", nested["nested"]);
        }

        [Fact]
        public void Convert_DateParsingIsExplicit() {
            const string timestamp = "2026-07-25T12:34:56.0000000+00:00";
            using var document = JsonDocument.Parse($"\"{timestamp}\"");

            Assert.Equal(timestamp, JsonElementValueConverter.Convert(document.RootElement, parseDates: false));
            Assert.Equal(
                DateTimeOffset.Parse(timestamp),
                Assert.IsType<DateTimeOffset>(JsonElementValueConverter.Convert(document.RootElement, parseDates: true)));
        }

        [Fact]
        public void Convert_UndefinedElementBecomesNull() {
            Assert.Null(JsonElementValueConverter.Convert(default, parseDates: false));
        }
    }
}
