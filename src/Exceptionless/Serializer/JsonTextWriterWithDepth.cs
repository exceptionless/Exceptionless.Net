using System.IO;
using Exceptionless.Json;

namespace Exceptionless.Serializer {
    internal class JsonTextWriterWithDepth : JsonTextWriter {
        public JsonTextWriterWithDepth(TextWriter textWriter) : base(textWriter) {}

        public int CurrentDepth { get; private set; }

        public override void WriteStartObject() {
            CurrentDepth++;
            base.WriteStartObject();
        }

        public override void WriteEndObject() {
            CurrentDepth--;
            base.WriteEndObject();
        }
    }
}