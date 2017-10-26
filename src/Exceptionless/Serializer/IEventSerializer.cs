using System.IO;
using Exceptionless.Models;

namespace Exceptionless.Serializer {
    public interface IEventSerializer{
        void Serialize(Event model, Stream outputStream);
        Event Deserialize(Stream inputStream);
    }
}
