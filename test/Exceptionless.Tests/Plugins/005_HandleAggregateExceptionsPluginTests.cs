using System;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Exceptionless.Submission;
using Exceptionless.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class HandleAggregateExceptionsPluginTests : PluginTestBase {
        public HandleAggregateExceptionsPluginTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SingleInnerException() {
            var client = CreateClient();
            var plugin = new HandleAggregateExceptionsPlugin();

            var exceptionOne = new Exception("one");
            var exceptionTwo = new Exception("two");

            var context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(exceptionOne);
            plugin.Run(context);
            Assert.False(context.Cancel);

            context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(new AggregateException(exceptionOne));
            plugin.Run(context);
            Assert.False(context.Cancel);
            Assert.Equal(exceptionOne, context.ContextData.GetException());

            context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(new AggregateException(exceptionOne, exceptionTwo));
            plugin.Run(context);
            Assert.True(context.Cancel);
        }

        [Fact]
        public void MultipleInnerException() {
            var submissionClient = new InMemorySubmissionClient();
            var client = new ExceptionlessClient("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw");
            client.Configuration.Resolver.Register<ISubmissionClient>(submissionClient);

            var plugin = new HandleAggregateExceptionsPlugin();
            var exceptionOne = new Exception("one");
            var exceptionTwo = new Exception("two");

            var context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(new AggregateException(exceptionOne, exceptionTwo));
            plugin.Run(context);
            Assert.True(context.Cancel);

            client.ProcessQueue();
            Assert.Equal(2, submissionClient.Events.Count);
        }
    }
}