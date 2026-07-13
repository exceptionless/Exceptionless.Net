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
    [JsonSerializable(typeof(EnvironmentInfo))]
    [JsonSerializable(typeof(Error))]
    [JsonSerializable(typeof(Event))]
    [JsonSerializable(typeof(IEnumerable<Event>))]
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
    internal partial class ExceptionlessJsonSerializerContext : JsonSerializerContext { }
}
