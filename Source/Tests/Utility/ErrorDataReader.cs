using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Models.Data;
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
            var ev = serializer.Deserialize<Event>(json);
            ev.Data[Event.KnownDataKeys.Error] = ev.Data.GetValue<Error>(Event.KnownDataKeys.Error);
            return ev;
        }
        public static IEnumerable<Event> GetEvents() {
            return Directory.EnumerateFiles(@"..\..\ErrorData", "*.json").Select(GetEvent);
        }
    }
}
