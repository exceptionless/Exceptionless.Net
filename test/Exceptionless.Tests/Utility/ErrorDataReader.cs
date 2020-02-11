using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;

namespace Exceptionless.Tests.Utility {
    public class ErrorDataReader {
        private static IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        private static Event GetEvent(string fileName) {
            string json = File.ReadAllText(fileName);
            var serializer = GetSerializer();
            var ev = serializer.Deserialize<Event>(json);
            ev.Data[Event.KnownDataKeys.Error] = ev.Data.GetValue<Error>(Event.KnownDataKeys.Error);
            return ev;
        }
        
        public static IEnumerable<Event> GetEvents() {
            string path = Path.GetFullPath(Path.Combine("..", "..", "..", "ErrorData"));
            if (!Directory.Exists(path))
                path = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", "..", "..", "ErrorData"));
            
            return Directory.EnumerateFiles(path, "*.json").Select(GetEvent);
        }
    }
}
