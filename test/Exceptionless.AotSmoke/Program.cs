using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Exceptionless;
using Exceptionless.Dependency;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Exceptionless.Submission;
using Microsoft.Extensions.DependencyInjection;

using (var defaultClient = new ExceptionlessClient(configuration => {
    configuration.ApiKey = "00000000000000000000000000000000";
    configuration.IncludeModules = false;
    configuration.IncludePrivateInformation = false;
    configuration.UpdateSettingsWhenIdleInterval = System.TimeSpan.Zero;
    configuration.UseInMemoryStorage();
})) {
    var defaultJsonSerializer = defaultClient.Configuration.Resolver.GetJsonSerializer();
    var defaultStorageSerializer = defaultClient.Configuration.Resolver.GetStorageSerializer();
    var builtInEvent = new Event {
        Type = Event.KnownTypes.Log,
        Source = "aot-default-services",
        Message = "NativeAOT default DI and serializer",
        Data = {
            ["answer"] = 42,
            ["enabled"] = true
        }
    };
    builtInEvent.SetProperty("byte", (byte)1, client: defaultClient);
    builtInEvent.SetProperty("sbyte", (sbyte)-2, client: defaultClient);
    builtInEvent.SetProperty("short", (short)-3, client: defaultClient);
    builtInEvent.SetProperty("ushort", (ushort)4, client: defaultClient);
    builtInEvent.SetProperty("uint", System.UInt32.MaxValue, client: defaultClient);
    builtInEvent.SetProperty("long", System.Int64.MinValue, client: defaultClient);
    builtInEvent.SetProperty("ulong", System.UInt64.MaxValue, client: defaultClient);
    builtInEvent.SetProperty("float", 1.25f, client: defaultClient);
    builtInEvent.SetProperty("double", 2.5d, client: defaultClient);
    builtInEvent.SetProperty("decimal", 3.75m, client: defaultClient);
    builtInEvent.Data["char"] = 'x';
    builtInEvent.Data["date_time"] = new System.DateTime(2026, 8, 12, 21, 0, 0, System.DateTimeKind.Utc);
    builtInEvent.Data["date_time_offset"] = new System.DateTimeOffset(2026, 8, 12, 21, 0, 0, System.TimeSpan.Zero);
    builtInEvent.Data["guid"] = System.Guid.Parse("fb10e03c-51bf-49b9-be3d-1b537e64f00a");
    builtInEvent.Data["time_span"] = System.TimeSpan.FromSeconds(2);
    builtInEvent.Data["uri"] = new System.Uri("https://exceptionless.com/aot");
    builtInEvent.Data["bytes"] = new byte[] { 1, 2, 3 };

    string builtInJson = defaultJsonSerializer.Serialize(builtInEvent);
    Assert(builtInJson.Contains("\"source\":\"aot-default-services\""), "Default serializer lost a built-in event.");
    using (var builtInDocument = System.Text.Json.JsonDocument.Parse(builtInJson)) {
        var data = builtInDocument.RootElement.GetProperty("data");
        Assert(data.GetProperty("byte").GetByte() == 1, "Default serializer lost byte event data.");
        Assert(data.GetProperty("sbyte").GetSByte() == -2, "Default serializer lost sbyte event data.");
        Assert(data.GetProperty("short").GetInt16() == -3, "Default serializer lost short event data.");
        Assert(data.GetProperty("ushort").GetUInt16() == 4, "Default serializer lost ushort event data.");
        Assert(data.GetProperty("uint").GetUInt32() == System.UInt32.MaxValue, "Default serializer lost uint event data.");
        Assert(data.GetProperty("long").GetInt64() == System.Int64.MinValue, "Default serializer lost long event data.");
        Assert(data.GetProperty("ulong").GetUInt64() == System.UInt64.MaxValue, "Default serializer lost ulong event data.");
        Assert(data.GetProperty("float").GetSingle() == 1.25f, "Default serializer lost float event data.");
        Assert(data.GetProperty("double").GetDouble() == 2.5d, "Default serializer lost double event data.");
        Assert(data.GetProperty("decimal").GetDecimal() == 3.75m, "Default serializer lost decimal event data.");
        Assert(data.GetProperty("char").GetString() == "x", "Default serializer lost char event data.");
        Assert(data.GetProperty("date_time").GetDateTime() == new System.DateTime(2026, 8, 12, 21, 0, 0, System.DateTimeKind.Utc), "Default serializer lost DateTime event data.");
        Assert(data.GetProperty("date_time_offset").GetDateTimeOffset() == new System.DateTimeOffset(2026, 8, 12, 21, 0, 0, System.TimeSpan.Zero), "Default serializer lost DateTimeOffset event data.");
        Assert(data.GetProperty("guid").GetGuid() == System.Guid.Parse("fb10e03c-51bf-49b9-be3d-1b537e64f00a"), "Default serializer lost Guid event data.");
        Assert(System.TimeSpan.Parse(data.GetProperty("time_span").GetString(), System.Globalization.CultureInfo.InvariantCulture) == System.TimeSpan.FromSeconds(2), "Default serializer lost TimeSpan event data.");
        Assert(data.GetProperty("uri").GetString() == "https://exceptionless.com/aot", "Default serializer lost Uri event data.");
        Assert(data.GetProperty("bytes").GetBytesFromBase64().SequenceEqual(new byte[] { 1, 2, 3 }), "Default serializer lost byte-array event data.");
    }

    using var defaultStream = new MemoryStream();
    defaultStorageSerializer.Serialize(builtInEvent, defaultStream);
    defaultStream.Position = 0;
    var defaultRoundTrip = defaultStorageSerializer.Deserialize<Event>(defaultStream);
    Assert(defaultRoundTrip.Source == builtInEvent.Source, "Default storage serializer lost a built-in event.");
    Assert(System.Convert.ToInt32(defaultRoundTrip.Data["answer"]) == 42, "Default storage serializer lost primitive event data.");

    var clientConfiguration = (ClientConfiguration)defaultJsonSerializer.Deserialize(
        "{\"version\":1,\"settings\":{\"@@log:*\":\"Off\"}}",
        typeof(ClientConfiguration));
    Assert(clientConfiguration.Version == 1, "Default serializer lost the client configuration version.");
    Assert(clientConfiguration.Settings["@@log:*"] == "Off", "Default serializer did not populate client configuration settings.");
}

var submissionClient = new CapturingSubmissionClient();
var jsonSerializer = new DefaultJsonSerializer(AotSmokeJsonSerializerContext.Default);
string declaredContractJson = jsonSerializer.Serialize(new AotDeclaredContractContainer {
    Payload = new AotUnregisteredDerivedPayload {
        BaseValue = "base-contract",
        DerivedValue = "must-not-require-runtime-metadata"
    }
});
Assert(declaredContractJson.Contains("\"base_value\":\"base-contract\""), "Declared source-generated contract lost its base member.");
Assert(!declaredContractJson.Contains("derived_value", System.StringComparison.Ordinal), "Declared source-generated contract leaked unregistered runtime members.");

var services = new ServiceCollection();
services.AddSingleton<ISubmissionClient>(submissionClient);
services.AddSingleton<IJsonSerializer>(jsonSerializer);
services.AddSingleton<IStorageSerializer>(jsonSerializer);

using var client = new ExceptionlessClient(services, configuration => {
    configuration.ApiKey = "00000000000000000000000000000000";
    configuration.IncludePrivateInformation = false;
    configuration.UpdateSettingsWhenIdleInterval = System.TimeSpan.Zero;
    configuration.UseInMemoryStorage();
});

var serializer = client.Configuration.Resolver.GetJsonSerializer();
var storageSerializer = client.Configuration.Resolver.GetStorageSerializer();
var original = new Event {
    Type = Event.KnownTypes.Log,
    Source = "aot-smoke",
    Message = "NativeAOT serialization",
    Date = new System.DateTimeOffset(2026, 7, 12, 18, 0, 0, System.TimeSpan.Zero),
    Data = {
        ["payload"] = new SmokePayload { Id = 42, Name = "trim-safe", PublicField = "field-safe" }
    }
};

string json = serializer.Serialize(original);
Assert(json.Contains("\"source\":\"aot-smoke\""), "Event serialization lost source.");
Assert(json.Contains("\"id\":42"), "Arbitrary payload serialization lost data.");
Assert(json.Contains("\"public_field\":\"field-safe\""), "Arbitrary payload serialization lost a public field.");
Assert(json.Contains("\"converted\":\"aot:contract-safe\""), "Arbitrary payload serialization ignored a source-generated property converter.");
Assert(!json.Contains("ignored", System.StringComparison.OrdinalIgnoreCase), "Arbitrary payload serialization included compatibility-ignored data.");
string batchJson = serializer.Serialize(new List<Event> { original });
Assert(batchJson.Contains("\"source\":\"aot-smoke\""), "Submission batch serialization lost the event.");

using var stream = new MemoryStream();
storageSerializer.Serialize(original, stream);
stream.Position = 0;
var roundTripped = storageSerializer.Deserialize<Event>(stream);
Assert(roundTripped.Source == original.Source, "Storage round-trip lost source.");

client.SubmitEvent(original);
await client.ProcessQueueAsync();
Assert(submissionClient.Events.Count == 1, "Queue did not submit exactly one event.");
string submittedJson = serializer.Serialize(submissionClient.Events[0]);
Assert(submittedJson.Contains("\"id\":42"), "Queue round-trip lost custom payload metadata.");

string rawExceptionStack = null;
try {
    ThrowNestedException();
}
catch (System.Exception ex) {
    rawExceptionStack = ex.ToString();
    client.SubmitException(ex);
}

await client.ProcessQueueAsync();
Assert(submissionClient.Events.Count == 2, "Exception event was not submitted.");
Assert(submissionClient.Events[1].Type == Event.KnownTypes.Error, "Exception event lost its type.");
Assert(submissionClient.Events[1].Data.ContainsKey(Event.KnownDataKeys.Error), "Exception event lost its error model.");
object submittedErrorData = submissionClient.Events[1].Data[Event.KnownDataKeys.Error];
var submittedError = submittedErrorData as Exceptionless.Models.Data.Error
    ?? (Exceptionless.Models.Data.Error)serializer.Deserialize((string)submittedErrorData, typeof(Exceptionless.Models.Data.Error));
Assert(submittedError.StackTrace.Count > 0, "NativeAOT exception capture produced no stack frames.");
Assert(
    submittedError.StackTrace.All(frame => !System.String.IsNullOrEmpty(frame.Name)),
    "NativeAOT exception frames lost method identity."
        + System.Environment.NewLine + rawExceptionStack
        + System.Environment.NewLine + System.String.Join(
            System.Environment.NewLine,
            submittedError.StackTrace.Select(frame => $"{frame.DeclaringNamespace}.{frame.DeclaringType}.{frame.Name}")));

var customDataException = new SmokeException();
client.SubmitException(customDataException);
await client.ProcessQueueAsync();
Assert(customDataException.Data.Contains("@exceptionless"), "A value-type exception Data dictionary could not be marked as processed.");
Assert(submissionClient.Events.Count == 3, "Custom exception event was not submitted.");

System.Console.WriteLine("AOT_SMOKE_OK");

static void Assert(bool condition, string message) {
    if (!condition)
        throw new System.InvalidOperationException(message);
}

[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
static void ThrowNestedException() {
    try {
        ThrowOriginalException();
    }
    catch (System.Exception exception) {
        ExceptionDispatchInfo.Capture(exception).Throw();
        throw;
    }
}

[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
static void ThrowOriginalException() {
    throw new System.InvalidOperationException("NativeAOT exception capture", new System.ArgumentException("inner"));
}

internal sealed class SmokePayload {
    public int Id { get; set; }
    public string Name { get; set; }

    [JsonConverter(typeof(SmokePrefixConverter))]
    public string Converted { get; set; } = "contract-safe";

    [Exceptionless.Json.ExceptionlessIgnore]
    public string Ignored { get; set; } = "ignored-property";

    public string PublicField;

    [Exceptionless.Json.ExceptionlessIgnore]
    public string IgnoredField = "ignored-field";
}

internal sealed class AotDeclaredContractContainer {
    public AotDeclaredContractPayload Payload { get; set; }
}

internal class AotDeclaredContractPayload {
    public string BaseValue { get; set; }
}

internal sealed class AotUnregisteredDerivedPayload : AotDeclaredContractPayload {
    public string DerivedValue { get; set; }
}

internal sealed class SmokePrefixConverter : JsonConverter<string> {
    public override string Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) => reader.GetString();
    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options) => writer.WriteStringValue($"aot:{value}");
}

internal readonly struct SmokeValue { }

internal sealed class SmokeException : System.Exception {
    private readonly IDictionary _data = new Dictionary<string, SmokeValue>();

    public SmokeException() : base("NativeAOT custom exception data") { }

    public override IDictionary Data => _data;
}

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    IncludeFields = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(SmokePayload))]
[JsonSerializable(typeof(AotDeclaredContractContainer))]
[JsonSerializable(typeof(AotDeclaredContractPayload))]
internal partial class AotSmokeJsonSerializerContext : JsonSerializerContext { }

internal sealed class CapturingSubmissionClient : ISubmissionClient {
    public List<Event> Events { get; } = new List<Event>();

    public Task<SubmissionResponse> PostEventsAsync(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
        Events.AddRange(events);
        return Task.FromResult(new SubmissionResponse(202, "Accepted"));
    }

    public Task<SubmissionResponse> PostUserDescriptionAsync(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
        return Task.FromResult(new SubmissionResponse(202, "Accepted"));
    }

    public Task<SettingsResponse> GetSettingsAsync(ExceptionlessConfiguration config, int version, IJsonSerializer serializer) {
        return Task.FromResult(new SettingsResponse(false));
    }

    public Task SendHeartbeatAsync(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config) {
        return Task.CompletedTask;
    }
}
