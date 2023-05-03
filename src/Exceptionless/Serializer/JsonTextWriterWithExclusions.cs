using System.IO;
using Exceptionless.Extensions;

namespace Exceptionless.Serializer
{
    internal sealed class JsonTextWriterWithExclusions : JsonTextWriterWithDepth {
        private readonly string[] _excludedPropertyNames;
        
        public JsonTextWriterWithExclusions(TextWriter textWriter, string[] excludedPropertyNames) : base(textWriter) {
            _excludedPropertyNames = excludedPropertyNames;
        }
        
        public override bool ShouldWriteProperty(string name) {
            var exclusions = _excludedPropertyNames;
            return exclusions == null || exclusions.Length == 0 || !name.AnyWildcardMatches(exclusions, ignoreCase: true);
        }
    }
}