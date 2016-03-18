using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Serializer;

namespace Exceptionless.Tests.Plugins {
    public class DeduplicationBenchmarks {
        private readonly List<Event> _events = new List<Event>();
        public DeduplicationBenchmarks() {
            foreach (var file in Directory.GetFiles(@"..\..\ErrorData", "*.json")) {
                _events.Add(GetEvent(file));
            }
        }
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        Event GetEvent(string fileName) {
            var json = File.ReadAllText(fileName);
            var serializer = GetSerializer();
            return serializer.Deserialize<Event>(json);
        }

        [Benchmark]
        public void TestBenchmark1() {
            var client = new ExceptionlessClient();
            var errorPlugin = new ErrorPlugin();
            var duplicateCheckerPlugin = new DuplicateCheckerPlugin();

            foreach (var ev in _events) {
                var pluginContextData = new ContextData();

                for (int index = 0; index < 2; index++) {
                    var context = new EventPluginContext(client, ev, pluginContextData);

                    errorPlugin.Run(context);
                    duplicateCheckerPlugin.Run(context);
                }
            }
        }
    }
}