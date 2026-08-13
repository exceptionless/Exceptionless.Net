using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Exceptionless.Serializer {
    /// <summary>
    /// Converts JSON DOM values into the primitive, dictionary, and list values
    /// expected by the MessagePack storage serializer.
    /// </summary>
    internal static class JsonElementValueConverter {
        internal static object Convert(JsonElement element, bool parseDates) {
            switch (element.ValueKind) {
                case JsonValueKind.Object:
                    var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (JsonProperty property in element.EnumerateObject())
                        dictionary[property.Name] = Convert(property.Value, parseDates);
                    return dictionary;
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (JsonElement item in element.EnumerateArray())
                        list.Add(Convert(item, parseDates));
                    return list;
                case JsonValueKind.String:
                    if (parseDates && element.TryGetDateTimeOffset(out DateTimeOffset date))
                        return date;
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    if (element.TryGetDecimal(out decimal decimalValue))
                        return decimalValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    throw new JsonException($"Unexpected JSON value kind: {element.ValueKind}");
            }
        }
    }
}
