using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Exceptionless.Tests.Utility;
using Xunit;
using Xunit.Abstractions;
using Exceptionless.Json;
using Exceptionless.Extensions;

namespace Exceptionless.Tests.Plugins {
    public class DuplicateCheckerPluginTests : PluginTestBase {
        public DuplicateCheckerPluginTests(ITestOutputHelper output) : base(output) { }
        
        [Fact]
        public void CanRemoveDuplicateExceptions() {
            var client = CreateClient();
            var errorPlugin = new SimpleErrorPlugin();

            EventPluginContext mergedContext = null;
            using (var duplicateCheckerPlugin = new DuplicateCheckerPlugin(TimeSpan.FromSeconds(1))) {
                for (int index = 0; index < 10; index++) {
                    var builder = GetException().ToExceptionless();
                    var context = new EventPluginContext(client, builder.Target, builder.PluginContextData);

                    errorPlugin.Run(context);
                    duplicateCheckerPlugin.Run(context);

                    if (index == 0) {
                        Assert.False(context.Cancel);
                        Assert.Null(context.Event.Count);
                    } else {
                        Assert.True(context.Cancel);
                        if (index == 1)
                            mergedContext = context;
                    }
                }
            }

            Assert.Equal(9, mergedContext.Event.Count.GetValueOrDefault());
        }

        [Fact]
        public void WillCallSubmittingHandler() {
            var client = CreateClient();
            foreach (var plugin in client.Configuration.Plugins)
                client.Configuration.RemovePlugin(plugin.Key);

            int submittingEventHandlerCalls = 0;
            using (var duplicateCheckerPlugin = new DuplicateCheckerPlugin(TimeSpan.FromSeconds(1))) {
                client.Configuration.AddPlugin(duplicateCheckerPlugin);

                client.SubmittingEvent += (sender, args) => {
                    Interlocked.Increment(ref submittingEventHandlerCalls);
                };

                for (int index = 0; index < 3; index++) {
                    client.SubmitLog("test");
                    if (index > 0)
                        continue;

                    Assert.Equal(1, submittingEventHandlerCalls);
                }
            }

            Assert.Equal(2, submittingEventHandlerCalls);
        }

        [Fact]
        public void CanHandleConcurrency() {
            var client = CreateClient();
            // TODO: We need to look into why the ErrorPlugin causes data to sometimes calculate invalid hashcodes
            var simpleError = GetException().ToSimpleErrorModel(client);

            var contexts = new ConcurrentBag<EventPluginContext>();
            using (var duplicateCheckerPlugin = new DuplicateCheckerPlugin(TimeSpan.FromSeconds(60))) {
                int hashCode = 0;
                var result = Parallel.For(0, 10, index => {
                    var builder = client.CreateEvent();
                    builder.SetType(Event.KnownTypes.Error);
                    builder.Target.Data[Event.KnownDataKeys.SimpleError] = simpleError;

                    var context = new EventPluginContext(client, builder.Target, builder.PluginContextData);
                    contexts.Add(context);

                    duplicateCheckerPlugin.Run(context);

                    if (hashCode == 0)
                        hashCode = builder.Target.GetHashCode();
                    else if (hashCode != builder.Target.GetHashCode())
                        throw new ApplicationException();
                });

                while (!result.IsCompleted) {
                    Thread.Yield();
                }
            }

            var nonCancelled = contexts.Where(c => !c.Cancel).Select(c => (Context: c, Event: c.Event.GetHashCode(), Json: JsonConvert.SerializeObject(c.Event))).ToList();
            var all = contexts.Select(c => (Context: c, Event: c.Event.GetHashCode())).ToList();

            Assert.Equal(1, contexts.Count(c => !c.Cancel));
            Assert.Equal(9, contexts.Count(c => c.Cancel));
            Assert.Equal(9, contexts.Sum(c => c.Event.Count.GetValueOrDefault()));
        }

        [Fact]
        public void VerifyDeduplicationFromFiles() {
            var client = CreateClient();
            var errorPlugin = new ErrorPlugin();

            foreach (var ev in ErrorDataReader.GetEvents()) {
                using (var duplicateCheckerPlugin = new DuplicateCheckerPlugin(TimeSpan.FromSeconds(1))) {
                    for (int index = 0; index < 2; index++) {
                        var contextData = new ContextData();
                        var context = new EventPluginContext(client, ev, contextData);

                        errorPlugin.Run(context);
                        duplicateCheckerPlugin.Run(context);

                        if (index == 0) {
                            Assert.False(context.Cancel);
                            Assert.Null(context.Event.Count);
                        } else {
                            Assert.True(context.Cancel);
                            
                            // There is only two executions, so dispose to trigger submitting.
                            duplicateCheckerPlugin.Dispose();
                            Assert.Equal(1, context.Event.Count);
                        }
                    }
                }
            }
        }
    }
}