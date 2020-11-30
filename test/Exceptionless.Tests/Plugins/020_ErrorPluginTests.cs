using System;
using System.Collections;
using System.Collections.Generic;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class ErrorPluginTests : PluginTestBase {
        public ErrorPluginTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void WillPreserveLineBreaks() {
            const string message = "Test\r\nLine\r\n\tBreaks";

            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            var client = CreateClient();
            foreach (var plugin in errorPlugins) {
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(new NotSupportedException(message));
                plugin.Run(context);
                Assert.False(context.Cancel);

                Assert.Equal(Event.KnownTypes.Error, context.Event.Type);
                var error = context.Event.GetError();
                if (error != null)
                    Assert.Equal(message, error.Message);
                else
                    Assert.Equal(message, context.Event.GetSimpleError().Message);
            }
        }


        [Fact]
        public void WillPreserveEventType() {
            const string message = "Testing";

            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            var client = CreateClient();
            foreach (var plugin in errorPlugins) {
                var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.Log });
                context.ContextData.SetException(new NotSupportedException(message));
                plugin.Run(context);
                Assert.False(context.Cancel);

                Assert.Equal(Event.KnownTypes.Log, context.Event.Type);
                var error = context.Event.GetError();
                if (error != null)
                    Assert.Equal(message, error.Message);
                else
                    Assert.Equal(message, context.Event.GetSimpleError().Message);
            }
        }

        [Fact (Skip = "There is a bug in the .NET Framework where non thrown exceptions with non custom stack traces cannot be computed #116")]
        public void CanHandleExceptionWithOverriddenStackTrace() {
            var client = CreateClient();
            var plugin = new ErrorPlugin();

            var context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(GetExceptionWithOverriddenStackTrace());
            plugin.Run(context);
            Assert.False(context.Cancel);

            var error = context.Event.GetError();
            Assert.True(error.StackTrace.Count > 0);

            context.ContextData.SetException(new ExceptionWithOverriddenStackTrace("test"));
            plugin.Run(context);
            Assert.False(context.Cancel);

            error = context.Event.GetError();
            Assert.True(error.StackTrace.Count > 0);
        }

        [Fact]
        public void DiscardDuplicates() {
            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            foreach (var plugin in errorPlugins) {
                var exception = new Exception("Nested", new MyApplicationException("Test") {
                    IgnoredProperty = "Test",
                    RandomValue = "Test"
                });

                var client = CreateClient();
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);
                plugin.Run(context);
                Assert.False(context.Cancel);

                var error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);

                context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);
                plugin.Run(context);
                Assert.True(context.Cancel);

                error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.Null(error);
            }
        }

        public static IEnumerable<object[]> DifferentExceptionDataDictionaryTypes {
            get {
                return new[] {
                    new object[] { null, false, 0 },
                    new object[] { new Dictionary<object, object> { { (object)1, (object)1 } }, true, 1 },
                    new object[] { new Dictionary<PriorityAttribute, PriorityAttribute>() { { new PriorityAttribute(1), new PriorityAttribute(1) } }, false, 1 },
                    new object[] { new Dictionary<int, int> { { 1, 1 } }, false, 1 },
                    new object[] { new Dictionary<bool, bool> { { false, false } }, false, 1 },
                    new object[] { new Dictionary<Guid, Guid> { { Guid.Empty, Guid.Empty } }, false, 1 },
                    new object[] { new Dictionary<IData, IData> { { new SimpleError(), new SimpleError() } }, false, 1 },
                    new object[] { new Dictionary<TestEnum, TestEnum> { { TestEnum.None, TestEnum.None } }, false, 1 },
                    new object[] { new Dictionary<TestStruct, TestStruct> { { new TestStruct(), new TestStruct() } }, false, 1 },
                    new object[] { new Dictionary<string, string> { { "test", "string" } }, true, 1 },
                    new object[] { new Dictionary<string, object> { { "test", "object" } }, true, 1 },
                    new object[] { new Dictionary<string, PriorityAttribute> { { "test", new PriorityAttribute(1) } }, true, 1 },
                    new object[] { new Dictionary<string, Guid> { { "test", Guid.Empty } }, true, 1 },
                    new object[] { new Dictionary<string, IData> { { "test", new SimpleError() } }, true, 1 },
                    new object[] { new Dictionary<string, TestEnum> { { "test", TestEnum.None } }, true, 1 },
                    new object[] { new Dictionary<string, TestStruct> { { "test", new TestStruct() } }, true, 1 },
                    new object[] { new Dictionary<string, int> { { "test", 1 } }, true, 1 },
                    new object[] { new Dictionary<string, bool> { { "test", false } }, true, 1 }
                };
            }
        }

        [Theory]
        [MemberData(nameof(DifferentExceptionDataDictionaryTypes))]
        public void CanProcessDifferentExceptionDataDictionaryTypes(IDictionary data, bool canMarkAsProcessed, int processedDataItemCount) {
            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            foreach (var plugin in errorPlugins) {
                if (data != null && data.Contains("@exceptionless"))
                    data.Remove("@exceptionless");

                var exception = new MyApplicationException("Test") { SetsDataProperty = data };
                var client = CreateClient();
                client.Configuration.AddDataExclusions("SetsDataProperty");
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);
                plugin.Run(context);
                Assert.False(context.Cancel);

                Assert.Equal(canMarkAsProcessed, exception.Data != null && exception.Data.Contains("@exceptionless"));

                var error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);
                Assert.Equal(processedDataItemCount, error.Data.Count);
            }
        }

        [Fact]
        public void CopyExceptionDataToRootErrorData() {
            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            foreach (var plugin in errorPlugins) {
                var exception = new MyApplicationException("Test") {
                    RandomValue = "Test",
                    SetsDataProperty = new Dictionary<object, string> {
                        { 1, 1.GetType().Name },
                        { "test", "test".GetType().Name },
                        { Guid.NewGuid(), typeof(Guid).Name },
                        { false, typeof(bool).Name }
                    }
                };

                var client = CreateClient();
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);
                plugin.Run(context);
                Assert.False(context.Cancel);

                var error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);
                Assert.Equal(5, error.Data.Count);
            }
        }

        [Fact]
        public void CopyExceptionData() {
            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            foreach (var plugin in errorPlugins) {
                var exception = new Exception("Test") {
                    Data = { { "Test", "Test" } }
                };

                var client = CreateClient();
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);
                plugin.Run(context);
                Assert.False(context.Cancel);

                var error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);
                Assert.Single(error.Data);
                Assert.Equal("Test", error.Data.GetString("Test"));
            }
        }

        [Fact]
        public void IgnoredProperties() {
            var exception = new MyApplicationException("Test") {
                IgnoredProperty = "Test",
                RandomValue = "Test"
            };

            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            foreach (var plugin in errorPlugins) {
                var client = CreateClient();
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);

                plugin.Run(context);
                var error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);
                Assert.True(error.Data.ContainsKey(Error.KnownDataKeys.ExtraProperties));
                string json = error.Data[Error.KnownDataKeys.ExtraProperties] as string;
                Assert.Equal("{\"IgnoredProperty\":\"Test\",\"RandomValue\":\"Test\"}", json);

                client.Configuration.AddDataExclusions("Ignore*");
                context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);

                plugin.Run(context);
                error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);
                Assert.True(error.Data.ContainsKey(Error.KnownDataKeys.ExtraProperties));
                json = error.Data[Error.KnownDataKeys.ExtraProperties] as string;
                Assert.Equal("{\"RandomValue\":\"Test\"}", json);
            }
        }
    }
}