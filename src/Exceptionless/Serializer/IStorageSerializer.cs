using System.IO;

namespace Exceptionless.Serializer {
    public interface IStorageSerializer{
        void Serialize<T>(T data, Stream output);
        T Deserialize<T>(Stream input);
    }
}
