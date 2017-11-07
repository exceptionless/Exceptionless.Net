using MessagePack;
using Exceptionless.Dependency;
using Exceptionless.Serializer;

namespace Exceptionless.MessagePack {
    public static class ExceptionlessConfigurationExtensions {
        public static void UseMessagePackSerializer(this ExceptionlessConfiguration config) {
            config.Resolver.Register<IFormatterResolver>(new ExceptionlessFormatterResolver(config.Resolver));
            config.Resolver.Register<IStorageSerializer, MessagePackSerializer>();
        }
    }
}
