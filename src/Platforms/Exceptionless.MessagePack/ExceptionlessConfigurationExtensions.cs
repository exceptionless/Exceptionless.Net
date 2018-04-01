using Exceptionless.Dependency;
using Exceptionless.Serializer;

namespace Exceptionless.MessagePack {
    public static class ExceptionlessConfigurationExtensions {
        public static void UseMessagePackSerializer(this ExceptionlessConfiguration config) {
            config.Resolver.Register<IStorageSerializer>(new MessagePackStorageSerializer(config.Resolver));
        }
    }
}
