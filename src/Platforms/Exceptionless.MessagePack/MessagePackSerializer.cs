using System;
using System.IO;
using Exceptionless.Serializer;
using MessagePack;

namespace Exceptionless.MessagePack {
    public class MessagePackSerializer : IStorageSerializer {
        private readonly IFormatterResolver _formatterResolver;

        public MessagePackSerializer(IFormatterResolver formatterResolver) {
            _formatterResolver = formatterResolver;
        }

        public void Serialize<T>(T data, Stream output) {
            global::MessagePack.MessagePackSerializer.Serialize(output, data, _formatterResolver);
        }

        public T Deserialize<T>(Stream input) {
            return global::MessagePack.MessagePackSerializer.Deserialize<T>(input, _formatterResolver);
        }
    }
}
