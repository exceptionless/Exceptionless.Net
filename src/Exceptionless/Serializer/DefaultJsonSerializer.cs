using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.IO;

namespace Exceptionless.Serializer {
    public class DefaultJsonSerializer : IJsonSerializer, IStorageSerializer {
        private const int DefaultMaxDepth = 10;
        internal const int MaxDepthBuffer = 2;

        private static readonly MaxDepthJsonConverterFactory s_maxDepthJsonConverterFactory = new MaxDepthJsonConverterFactory();
        private static readonly DictionaryConverterFactory s_dictionaryConverterFactory = new DictionaryConverterFactory();

        private static readonly JsonWriterOptions s_writerOptions = new JsonWriterOptions() {
            SkipValidation = true
        };

        private static readonly JsonSerializerOptions s_deserializeOptions = new JsonSerializerOptions() {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = new SnakeCaseNamingPolicy()
        };

        private static readonly JsonSerializerOptions s_serializeOptions = new JsonSerializerOptions() {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNameCaseInsensitive = true,
            MaxDepth = DefaultMaxDepth + MaxDepthBuffer,
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            Converters = { s_dictionaryConverterFactory, s_maxDepthJsonConverterFactory }
        };

        private static readonly JsonSerializerOptions s_serializeOptionsStrict = new JsonSerializerOptions() {
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            NumberHandling = JsonNumberHandling.Strict,
            PropertyNameCaseInsensitive = false,
            MaxDepth = DefaultMaxDepth + MaxDepthBuffer,
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            Converters = { s_dictionaryConverterFactory, s_maxDepthJsonConverterFactory }
        };

        private static readonly RecyclableMemoryStreamManager manager = new RecyclableMemoryStreamManager();

        public DefaultJsonSerializer() {
        }

        public virtual void Serialize<T>(T data, Stream outputStream) {
            using var stream = manager.GetStream(); 
            var writer = GetWriter(outputStream);
            try {
                JsonSerializer.Serialize(writer, data, s_serializeOptions);
                writer.Flush();
            }
            finally {
                ReleaseWriter(writer);
            }

            stream.Position = 0;
            stream.CopyTo(outputStream);
        }

        public virtual T Deserialize<T>(Stream inputStream) {
            // Revist when sync with Stream is supported https://github.com/dotnet/runtime/issues/1574
            using (var stream = manager.GetStream()) {
                inputStream.CopyTo(stream);
                stream.Position = 0;
                return JsonSerializer.Deserialize<T>(new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int)stream.Length), s_serializeOptions);
            }
        }

        public virtual string Serialize(object model, string[] exclusions = null, int maxDepth = DefaultMaxDepth, bool continueOnSerializationError = true) {
            if (model == null)
                return null;

            var serializeOptions = continueOnSerializationError ? s_serializeOptions : s_serializeOptionsStrict;
            if (maxDepth != DefaultMaxDepth || (exclusions != null && exclusions.Length > 0)) {

                maxDepth += MaxDepthBuffer;
                maxDepth = (maxDepth < (1 + MaxDepthBuffer) ? Int32.MaxValue : maxDepth);
                serializeOptions = new JsonSerializerOptions(serializeOptions) { MaxDepth = maxDepth };

                if (exclusions != null && exclusions.Length > 0) {
                    serializeOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy(exclusions);
                }
            }

            return JsonSerializer.Serialize(model, serializeOptions);
        }

        public virtual object Deserialize(string json, Type type) {
            if (String.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize(json, type, s_deserializeOptions);
        }

        [ThreadStatic] private static Utf8JsonWriter s_writer;
        private static Utf8JsonWriter GetWriter(Stream outputStream) {
            var writer = s_writer;
            if (writer is null) {
                s_writer = writer = new Utf8JsonWriter(outputStream, s_writerOptions);
            }
            else {
                writer.Reset(outputStream);
            }

            return writer;
        }

        private static void ReleaseWriter(Utf8JsonWriter writer) {
            writer.Reset(Stream.Null);
        }
    }
}