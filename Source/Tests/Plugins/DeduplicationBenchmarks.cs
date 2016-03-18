using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Serializer;

namespace Exceptionless.Tests.Plugins {
    public class DeduplicationBenchmarks {
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

            var ev = GetEvent(@"..\..\ErrorData\1.json");
            var pluginContextData = new ContextData();

            for (int index = 0; index < 2; index++) {
                var context = new EventPluginContext(client, ev, pluginContextData);

                errorPlugin.Run(context);
                duplicateCheckerPlugin.Run(context);
            }
        }

        [Benchmark]
        public void TestBenchmark2() {
            var client = new ExceptionlessClient();
            var errorPlugin = new ErrorPlugin();
            var duplicateCheckerPlugin = new DuplicateCheckerPlugin();

            var ev = GetEvent(@"..\..\ErrorData\2.json");
            var pluginContextData = new ContextData();

            for (int index = 0; index < 2; index++) {
                var context = new EventPluginContext(client, ev, pluginContextData);

                errorPlugin.Run(context);
                duplicateCheckerPlugin.Run(context);
            }
        }

        [Benchmark]
        public void TestBenchmark3() {
            var client = new ExceptionlessClient();
            var errorPlugin = new ErrorPlugin();
            var duplicateCheckerPlugin = new DuplicateCheckerPlugin();

            var ev = GetEvent(@"..\..\ErrorData\3.json");
            var pluginContextData = new ContextData();

            for (int index = 0; index < 2; index++) {
                var context = new EventPluginContext(client, ev, pluginContextData);

                errorPlugin.Run(context);
                duplicateCheckerPlugin.Run(context);
            }
        }
    }
}