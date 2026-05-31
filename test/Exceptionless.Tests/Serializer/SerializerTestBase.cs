using Exceptionless.Serializer;

namespace Exceptionless.Tests.Serializer {
    public abstract class SerializerTestBase {
        protected IJsonSerializer Serializer { get; } = new DefaultJsonSerializer();

        protected string Serialize(object model, string[] exclusions = null, int maxDepth = 10) => Serializer.Serialize(model, exclusions, maxDepth);

        protected T Deserialize<T>(string json) => (T)Serializer.Deserialize(json, typeof(T));

        protected T RoundTrip<T>(T model) => Deserialize<T>(Serialize(model));
    }
}
