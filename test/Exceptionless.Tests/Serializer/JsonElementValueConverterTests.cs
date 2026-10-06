using System;
using System.Collections.Generic;
using System.Text.Json;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer {
    public class JsonElementValueConverterTests {
        [Fact]
        public void Convert_WithDateParsingOption_ControlsDateConversion() {
            // Arrange
            const string timestamp = "2026-07-25T12:34:56.0000000+00:00";
            using var document = JsonDocument.Parse($"\"{timestamp}\"");

            // Act
            object unparsed = JsonElementValueConverter.Convert(document.RootElement, parseDates: false);
            object parsed = JsonElementValueConverter.Convert(document.RootElement, parseDates: true);

            // Assert
            Assert.Equal(timestamp, unparsed);
            Assert.Equal(DateTimeOffset.Parse(timestamp), Assert.IsType<DateTimeOffset>(parsed));
        }

        [Fact]
        public void Convert_WithSupportedJsonValueKinds_MapsExpectedValues() {
            // Arrange
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

            // Act
            object converted = JsonElementValueConverter.Convert(document.RootElement, parseDates: false);

            // Assert
            var result = Assert.IsType<Dictionary<string, object>>(converted);
            Assert.Equal(42, Assert.IsType<int>(result["Int"]));
            Assert.Equal(2147483648L, Assert.IsType<long>(result["Long"]));
            Assert.Equal(3.14m, Assert.IsType<decimal>(result["Decimal"]));
            Assert.Equal(Double.MaxValue, Assert.IsType<double>(result["Double"]));
            Assert.True(Assert.IsType<bool>(result["True"]));
            Assert.False(Assert.IsType<bool>(result["False"]));
            Assert.Null(result["Null"]);

            var array = Assert.IsType<List<object>>(result["Array"]);
            Assert.Equal("value", array[0]);
            Assert.Equal(7, array[1]);

            var nested = Assert.IsType<Dictionary<string, object>>(result["Object"]);
            Assert.Equal("yes", nested["Nested"]);
        }

        [Fact]
        public void Convert_WithUndefinedElement_ReturnsNull() {
            // Arrange
            JsonElement element = default;

            // Act
            object result = JsonElementValueConverter.Convert(element, parseDates: false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Convert_WithCaseDistinctObjectKeys_PreservesBothKeys() {
            // Arrange
            using var document = JsonDocument.Parse(/* lang=json */ """{"A":1,"a":2}""");

            // Act
            object converted = JsonElementValueConverter.Convert(document.RootElement, parseDates: false);

            // Assert
            var result = Assert.IsType<Dictionary<string, object>>(converted);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result["A"]);
            Assert.Equal(2, result["a"]);
        }
    }
}
