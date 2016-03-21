using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Serializer;

namespace Exceptionless.Tests.Utility
{
    class ErrorDataReader
    {
        static IJsonSerializer GetSerializer()
        {
            return new DefaultJsonSerializer();
        }

        static Event GetEvent(string fileName)
        {
            var json = File.ReadAllText(fileName);
            var serializer = GetSerializer();
            return serializer.Deserialize<Event>(json);
        }
        public static IEnumerable<Event> GetEvents() {
            return Directory.EnumerateFiles(@"..\..\ErrorData", "*.json").Select(GetEvent);
        }
    }
}
