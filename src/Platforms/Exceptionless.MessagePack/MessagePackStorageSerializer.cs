using System.IO;
using Exceptionless.Dependency;
using Exceptionless.Serializer;
using MessagePack;

namespace Exceptionless.MessagePack {
    public class MessagePackStorageSerializer : IStorageSerializer {
        private readonly MessagePackSerializerOptions _options;

        public MessagePackStorageSerializer(IDependencyResolver resolver) {
            _options = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4BlockArray)
                .WithResolver(new ExceptionlessFormatterResolver(resolver));
        }

        public void Serialize<T>(T data, Stream output) {
            MessagePackSerializer.Serialize(output, data, _options);
        }

        public T Deserialize<T>(Stream input) {
            return MessagePackSerializer.Deserialize<T>(input, _options);
        }
    }
}
