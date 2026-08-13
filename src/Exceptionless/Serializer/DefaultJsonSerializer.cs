using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Exceptionless.Serializer {
    public class DefaultJsonSerializer : IJsonSerializer, IStorageSerializer {
        private readonly JsonSerializerOptions _serializerOptions;

        public DefaultJsonSerializer() : this(null) { }

        public DefaultJsonSerializer(IJsonTypeInfoResolver typeInfoResolver) {
            _serializerOptions = new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            _serializerOptions.Converters.Add(new DataDictionaryConverter());
            _serializerOptions.Converters.Add(new SettingsDictionaryConverter());

            _serializerOptions.TypeInfoResolverChain.Add(ExceptionlessJsonSerializerContext.Default);
            if (typeInfoResolver != null)
                _serializerOptions.TypeInfoResolverChain.Add(typeInfoResolver);

#if NET8_0_OR_GREATER
            if (RuntimeFeature.IsDynamicCodeSupported && JsonSerializer.IsReflectionEnabledByDefault)
#else
            if (JsonSerializer.IsReflectionEnabledByDefault)
#endif
                AddReflectionFallback();
        }

        public virtual void Serialize<T>(T data, Stream outputStream) {
            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));

            if (data == null)
                return;

            TrySerialize(data, outputStream, null, 10, true);
        }

        public virtual T Deserialize<T>(Stream inputStream) {
            return JsonSerializer.Deserialize(inputStream, GetTypeInfo<T>());
        }

        public virtual string Serialize(object model, string[] exclusions = null, int maxDepth = 10, bool continueOnSerializationError = true) {
            if (model == null)
                return null;

            using (var stream = new MemoryStream()) {
                if (!TrySerialize(model, stream, exclusions, maxDepth, continueOnSerializationError))
                    return null;

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public virtual object Deserialize(string json, Type type) {
            if (String.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize(json, GetTypeInfo(type));
        }

        private JsonTypeInfo GetTypeInfo(Type type) {
            return _serializerOptions.GetTypeInfo(type);
        }

        private JsonTypeInfo<T> GetTypeInfo<T>() {
            return (JsonTypeInfo<T>)GetTypeInfo(typeof(T));
        }

        private bool TrySerialize(object model, Stream outputStream, string[] exclusions, int maxDepth, bool continueOnSerializationError) {
            using (var writer = new Utf8JsonWriter(outputStream)) {
                var valueWriter = new JsonValueWriter(_serializerOptions, exclusions, maxDepth, continueOnSerializationError);
                return valueWriter.TryWrite(writer, model, model.GetType());
            }
        }

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "This reflection fallback is guarded by RuntimeFeature.IsDynamicCodeSupported and cannot run in NativeAOT applications.")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection is disabled by default in trimmed applications; applications that opt back in must preserve their reflected payload types.")]
        private void AddReflectionFallback() {
            _serializerOptions.Converters.Add(new JsonStringEnumConverter());
            _serializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
        }

    }
}
