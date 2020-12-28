using System;
using System.Collections.Generic;
using System.IO;
using Exceptionless.Extensions;

namespace Exceptionless.Serializer
{
    internal class JsonTextWriterWithExclusions : JsonTextWriterWithDepth {
        private readonly ISet<string> _excludedPropertyNames;
        
        public JsonTextWriterWithExclusions(TextWriter textWriter, ISet<string> excludedPropertyNames) : base(textWriter) {
            _excludedPropertyNames = excludedPropertyNames;
        }
        
        public override bool ShouldWriteProperty(string name) {
            return !name.AnyWildcardMatches(_excludedPropertyNames, true);
        }
    }
}