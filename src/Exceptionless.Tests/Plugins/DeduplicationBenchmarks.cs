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
        public void LargeEventsFromFiles() {
            foreach (var ev in _events) {
                var pluginContextData = new ContextData();

                for (int index = 0; index < 2; index++) {
                    var context = new EventPluginContext(_client, ev, pluginContextData);

                    _errorPlugin.Run(context);
                    _duplicateCheckerPlugin.Run(context);
                }
            }
        }

        private EventBuilder _randomEventBuilder;
        private EventBuilder _fixedEventBuilder;

        [Setup]
        public void Setup() {
            _randomEventBuilder = GetException(Guid.NewGuid().ToString()).ToExceptionless();
            _fixedEventBuilder = GetException().ToExceptionless();
        }

        [Benchmark]
        public void RandomExceptions() {
            var context = new EventPluginContext(_client, _randomEventBuilder.Target, _randomEventBuilder.PluginContextData);

            _errorPlugin.Run(context);
            _duplicateCheckerPlugin.Run(context);
        }

        [Benchmark]
        public void IdenticalExceptions() {
            var context = new EventPluginContext(_client, _fixedEventBuilder.Target, _fixedEventBuilder.PluginContextData);

            _errorPlugin.Run(context);
            _duplicateCheckerPlugin.Run(context);
        }

        private Exception GetException(string message = "Test") {
            try {
                throw new Exception(message);
            } catch (Exception ex) {
                return ex;
            }
        }
    }
}