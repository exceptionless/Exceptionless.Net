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
        private readonly ExceptionlessClient _client;
        private readonly ErrorPlugin _errorPlugin;
        private readonly DuplicateCheckerPlugin _duplicateCheckerPlugin;

        public DeduplicationBenchmarks() {
            _events = ErrorDataReader.GetEvents().ToList();
            _client = new ExceptionlessClient();
            _errorPlugin = new ErrorPlugin();
            _duplicateCheckerPlugin = new DuplicateCheckerPlugin();
        }

        [Benchmark]
        public void TestBenchmark1() {
            foreach (var ev in _events) {
                var pluginContextData = new ContextData();

                for (int index = 0; index < 2; index++) {
                    var context = new EventPluginContext(_client, ev, pluginContextData);

                    _errorPlugin.Run(context);
                    _duplicateCheckerPlugin.Run(context);
                }
            }
        }
    }
}