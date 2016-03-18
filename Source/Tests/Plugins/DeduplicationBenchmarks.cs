using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Exceptionless.Models;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Tests.Utility;

namespace Exceptionless.Tests.Plugins {
    public class DeduplicationBenchmarks {
        private readonly List<Event> _events;
        public DeduplicationBenchmarks() {
            _events = ErrorDataReader.GetEvents().ToList();
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