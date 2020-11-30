using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class PluginTests : PluginTestBase {
        public PluginTests(ITestOutputHelper output) : base(output) { }
        
        [Fact]
        public void CanAddPluginConcurrently() {
            var client = CreateClient();
            foreach (var plugin in client.Configuration.Plugins)
                client.Configuration.RemovePlugin(plugin.Key);

            Assert.Empty(client.Configuration.Plugins);

            Parallel.For(0, 1000, i => {
                client.Configuration.AddPlugin<EnvironmentInfoPlugin>();
            });

            Assert.Single(client.Configuration.Plugins);
            client.Configuration.RemovePlugin<EnvironmentInfoPlugin>();
            Assert.Empty(client.Configuration.Plugins);
        }

        [Fact]
        public void CanCancel() {
            var client = CreateClient();
            foreach (var plugin in client.Configuration.Plugins)
                client.Configuration.RemovePlugin(plugin.Key);

            client.Configuration.AddPlugin("cancel", 1, ctx => ctx.Cancel = true);
            client.Configuration.AddPlugin("add-tag", 2, ctx => ctx.Event.Tags.Add("Was Not Canceled"));

            var context = new EventPluginContext(client, new Event());
            EventPluginManager.Run(context);
            Assert.True(context.Cancel);
            Assert.Empty(context.Event.Tags);
        }

        [Fact]
        public void LazyLoadAndRemovePlugin() {
            var configuration = new ExceptionlessConfiguration(DependencyResolver.Default);
            foreach (var plugin in configuration.Plugins)
                configuration.RemovePlugin(plugin.Key);

            configuration.AddPlugin<ThrowIfInitializedTestPlugin>();
            configuration.RemovePlugin<ThrowIfInitializedTestPlugin>();
        }

        private class ThrowIfInitializedTestPlugin : IEventPlugin, IDisposable {
            public ThrowIfInitializedTestPlugin() {
                throw new Exception("Plugin shouldn't be constructed");
            }

            public void Run(EventPluginContext context) {}

            public void Dispose() {
                throw new Exception("Plugin shouldn't be created or disposed");
            }
        }

        [Fact]
        public void CanDisposePlugin() {
            var configuration = new ExceptionlessConfiguration(DependencyResolver.Default);
            foreach (var plugin in configuration.Plugins)
                configuration.RemovePlugin(plugin.Key);

            Assert.Equal(0, CounterTestPlugin.ConstructorCount);
            Assert.Equal(0, CounterTestPlugin.RunCount);
            Assert.Equal(0, CounterTestPlugin.DisposeCount);

            configuration.AddPlugin<CounterTestPlugin>();
            configuration.AddPlugin<CounterTestPlugin>();

            for (int i = 0; i < 2; i++) {
                foreach (var pluginRegistration in configuration.Plugins)
                    pluginRegistration.Plugin.Run(new EventPluginContext(CreateClient(), new Event()));
            }

            configuration.RemovePlugin<CounterTestPlugin>();
            configuration.RemovePlugin<CounterTestPlugin>();


            Assert.Equal(1, CounterTestPlugin.ConstructorCount);
            Assert.Equal(2, CounterTestPlugin.RunCount);
            Assert.Equal(1, CounterTestPlugin.DisposeCount);
        }

        private class CounterTestPlugin : IEventPlugin, IDisposable {
            public static byte ConstructorCount = 0;
            public static byte RunCount = 0;
            public static byte DisposeCount = 0;

            public CounterTestPlugin() {
                ConstructorCount++;
            }

            public void Run(EventPluginContext context) {
                RunCount++;
            }

            public void Dispose() {
                DisposeCount++;
            }
        }

        [Fact]
        public void VerifyPriority() {
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());
            foreach (var plugin in config.Plugins)
                config.RemovePlugin(plugin.Key);

            Assert.Empty(config.Plugins);
            config.AddPlugin<EnvironmentInfoPlugin>();
            config.AddPlugin<PluginWithPriority11>();
            config.AddPlugin(new PluginWithNoPriority());
            config.AddPlugin("version", 1, ctx => ctx.Event.SetVersion("1.0.0.0"));
            config.AddPlugin("version2", 2, ctx => ctx.Event.SetVersion("1.0.0.0"));
            config.AddPlugin("version3", 3, ctx => ctx.Event.SetVersion("1.0.0.0"));

            var plugins = config.Plugins.ToArray();
            Assert.Equal(typeof(PluginWithNoPriority), plugins[0].Plugin.GetType());
            Assert.Equal("version", plugins[1].Key);
            Assert.Equal("version2", plugins[2].Key);
            Assert.Equal("version3", plugins[3].Key);
            Assert.Equal(typeof(PluginWithPriority11), plugins[4].Plugin.GetType());
            Assert.Equal(typeof(EnvironmentInfoPlugin), plugins[5].Plugin.GetType());
        }

        [Fact]
        public void ViewPriority() {
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());
            foreach (var plugin in config.Plugins)
                Writer.WriteLine(plugin);
        }

        [Fact(Skip="Skip until we report benchmark results and look at them over time")]
        public void RunBenchmark() {
            var summary = BenchmarkRunner.Run<DeduplicationBenchmarks>();

            foreach (var report in summary.Reports) {
                Writer.WriteLine(report.ToString());

                double benchmarkMedianMilliseconds = report.ResultStatistics != null ? report.ResultStatistics.Median / 1000000 : 0;
                Writer.WriteLine($"{report.BenchmarkCase.DisplayInfo} - {benchmarkMedianMilliseconds:0.00}ms");
            }
        }

        private class PluginWithNoPriority : IEventPlugin {
            public void Run(EventPluginContext context) {}
        }

        [Priority(11)]
        private class PluginWithPriority11 : IEventPlugin {
            public void Run(EventPluginContext context) {}
        }
    }
}