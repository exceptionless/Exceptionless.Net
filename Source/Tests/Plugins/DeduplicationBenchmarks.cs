using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Exceptionless.Models;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Tests.Utility;
using Xunit;

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
        [Fact]
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

        [Benchmark]
        [Fact]
        public void RandomExceptions() {
            var builder = GetException(Guid.NewGuid().ToString()).ToExceptionless();
            var context = new EventPluginContext(_client, builder.Target, builder.PluginContextData);

            _errorPlugin.Run(context);
            _duplicateCheckerPlugin.Run(context);
        }

        [Benchmark]
        [Fact]
        public void IdenticalExceptions()
        {
            var builder = GetException().ToExceptionless();
            var context = new EventPluginContext(_client, builder.Target, builder.PluginContextData);

            _errorPlugin.Run(context);
            _duplicateCheckerPlugin.Run(context);
        }

        private Exception GetException(string message = "Test")
        {
            try
            {
                throw new Exception(message);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}