using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Exceptionless.Models;
using Exceptionless.Models.Data;

namespace Exceptionless.Serializer {
    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Metadata,
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IncludeFields = true,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(ClientConfiguration))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(byte))]
    [JsonSerializable(typeof(byte[]))]
    [JsonSerializable(typeof(decimal))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(char))]
    [JsonSerializable(typeof(System.DateTime))]
    [JsonSerializable(typeof(System.DateTimeOffset))]
    [JsonSerializable(typeof(EnvironmentInfo))]
    [JsonSerializable(typeof(Error))]
    [JsonSerializable(typeof(Event))]
    [JsonSerializable(typeof(IEnumerable<Event>))]
    [JsonSerializable(typeof(List<Event>))]
    [JsonSerializable(typeof(InnerError))]
    [JsonSerializable(typeof(ManualStackingInfo))]
    [JsonSerializable(typeof(Method))]
    [JsonSerializable(typeof(Module))]
    [JsonSerializable(typeof(Parameter))]
    [JsonSerializable(typeof(RequestInfo))]
    [JsonSerializable(typeof(SimpleError))]
    [JsonSerializable(typeof(SimpleInnerError))]
    [JsonSerializable(typeof(StackFrame))]
    [JsonSerializable(typeof(UserDescription))]
    [JsonSerializable(typeof(UserInfo))]
    [JsonSerializable(typeof(DataDictionary))]
    [JsonSerializable(typeof(SettingsDictionary))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(System.Guid))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(sbyte))]
    [JsonSerializable(typeof(short))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(System.TimeSpan))]
    [JsonSerializable(typeof(System.Uri))]
    [JsonSerializable(typeof(uint))]
    [JsonSerializable(typeof(ulong))]
    [JsonSerializable(typeof(ushort))]
    internal partial class ExceptionlessJsonSerializerContext : JsonSerializerContext { }
}
