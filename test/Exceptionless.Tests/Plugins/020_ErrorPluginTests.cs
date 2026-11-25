using System;
using System.Collections;
using System.Collections.Generic;
#if NET10_0
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
#endif
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Xunit;

namespace Exceptionless.Tests.Plugins {
    public class ErrorPluginTests : PluginTestBase {
        public ErrorPluginTests(ITestOutputHelper output) : base(output) { }

#if NET10_0
        private const string ModuleFixtureType = "ModuleIdFixture";
        private const string ModuleFixtureMethod = "Throw";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WillMapDuplicateAssemblyIdentitiesToDistinctEventModules(bool loadSameFile) {
            var directory = Directory.CreateTempSubdirectory();
            WeakReference outerContext = null;
            WeakReference innerContext = null;
            Event capturedEvent;

            try {
                capturedEvent = VerifyModuleMappings(directory.FullName, loadSameFile, out outerContext, out innerContext);
            } finally {
                for (int i = 0; i < 10 && (outerContext?.IsAlive == true || innerContext?.IsAlive == true); i++) {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                directory.Delete(recursive: true);
            }

            Assert.False(outerContext.IsAlive);
            Assert.False(innerContext.IsAlive);
            // A queued event can outlive the assemblies that produced its stack frames.
            GC.KeepAlive(capturedEvent);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Event VerifyModuleMappings(string directory, bool loadSameFile, out WeakReference outerContextReference, out WeakReference innerContextReference) {
            const string assemblyName = "Exceptionless.ModuleIdRegressionFixture";
            const string outerVersion = "1.0.1.0";
            const string innerVersion = "1.0.2.0";
            var outerContext = new AssemblyLoadContext("Outer module fixture", isCollectible: true);
            var innerContext = new AssemblyLoadContext("Inner module fixture", isCollectible: true);
            outerContextReference = new WeakReference(outerContext, trackResurrection: true);
            innerContextReference = new WeakReference(innerContext, trackResurrection: true);

            try {
                string outerPath = Path.Combine(directory, "outer.dll");
                string innerPath = loadSameFile ? outerPath : Path.Combine(directory, "inner.dll");
                WriteThrowingAssembly(outerPath, assemblyName, outerVersion);
                if (!loadSameFile)
                    WriteThrowingAssembly(innerPath, assemblyName, innerVersion);

                var outerAssembly = outerContext.LoadFromAssemblyPath(outerPath);
                var innerAssembly = innerContext.LoadFromAssemblyPath(innerPath);
                Assert.Equal(outerAssembly.FullName, innerAssembly.FullName);
                Assert.NotSame(outerAssembly, innerAssembly);

                var throwOuter = outerAssembly.GetType(ModuleFixtureType, throwOnError: true).GetMethod(ModuleFixtureMethod).CreateDelegate<Action<Exception>>();
                var throwInner = innerAssembly.GetType(ModuleFixtureType, throwOnError: true).GetMethod(ModuleFixtureMethod).CreateDelegate<Action<Exception>>();
                var inner = Record.Exception(() => throwInner(null));
                var outer = Record.Exception(() => throwOuter(inner));

                using var client = CreateClient(c => c.IncludeModules = true);
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(outer);
                new ErrorPlugin().Run(context);
                var serializer = client.Configuration.Resolver.GetJsonSerializer();
                string json = serializer.Serialize(context.Event);
                var error = serializer.Deserialize<Event>(json).GetError(serializer);

                Assert.Equal(error.Modules.Count, error.Modules.Select(m => m.ModuleId).Distinct().Count());
                var fixtureModules = error.Modules.Where(m => m.Name == assemblyName).ToList();
                Assert.Equal(2, fixtureModules.Count);
                int outerId = Assert.Single(error.StackTrace, f => f.DeclaringType == ModuleFixtureType).ModuleId;
                int innerId = Assert.Single(error.Inner.StackTrace, f => f.DeclaringType == ModuleFixtureType).ModuleId;
                Assert.True(outerId > 0);
                Assert.True(innerId > 0);
                Assert.NotEqual(outerId, innerId);
                Assert.Equal(outerVersion, Assert.Single(fixtureModules, m => m.ModuleId == outerId).Version);
                Assert.Equal(loadSameFile ? outerVersion : innerVersion, Assert.Single(fixtureModules, m => m.ModuleId == innerId).Version);

                var nextError = Record.Exception(() => throwOuter(null)).ToErrorModel(client);
                var originalModule = Assert.Single(context.Event.GetError().Modules, m => m.ModuleId == outerId);
                int nextId = Assert.Single(nextError.StackTrace, f => f.DeclaringType == ModuleFixtureType).ModuleId;
                var nextModule = Assert.Single(nextError.Modules, m => m.ModuleId == nextId);
                Assert.NotSame(originalModule, nextModule);
                Assert.NotSame(originalModule.Data, nextModule.Data);
                return context.Event;
            } finally {
                innerContext.Unload();
                outerContext.Unload();
            }
        }

        private static void WriteThrowingAssembly(string path, string name, string fileVersion) {
            var assembly = new PersistedAssemblyBuilder(new AssemblyName(name) { Version = new Version(1, 0, 0, 0) }, typeof(object).Assembly);
            var versionConstructor = typeof(AssemblyFileVersionAttribute).GetConstructor(new[] { typeof(string) });
            assembly.SetCustomAttribute(new CustomAttributeBuilder(versionConstructor, new object[] { fileVersion }));
            var type = assembly.DefineDynamicModule(name).DefineType(ModuleFixtureType, TypeAttributes.Public);
            var method = type.DefineMethod(ModuleFixtureMethod, MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(Exception) });
            method.SetImplementationFlags(MethodImplAttributes.NoInlining);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldstr, "Synthetic module exception");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string), typeof(Exception) }));
            il.Emit(OpCodes.Throw);
            type.CreateType();
            assembly.Save(path);
        }
#endif

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

        [Fact]
        public void WillUnwrapExceptionTypeName() {
            const string type = "Exceptionless.Tests.Plugins.PluginTestBase+GenericException<Exceptionless.Tests.Plugins.PluginTestBase+ErrorCategory>";

            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            var client = CreateClient();
            foreach (var plugin in errorPlugins) {
                var context = new EventPluginContext(client, new Event());
                var exception = new GenericException<ErrorCategory>(ErrorCategory.FirstErrorBucket);
                var nestedException = new GenericException<ErrorCategory>(ErrorCategory.SecondErrorBucket,exception);
                context.ContextData.SetException(nestedException);
                plugin.Run(context);
                Assert.False(context.Cancel);
                Assert.Equal(Event.KnownTypes.Error, context.Event.Type);
                var error = context.Event.GetError();
                if (error != null)
                    Assert.Equal(type, error.Type);
                else
                    Assert.Equal(type, context.Event.GetSimpleError().Type);
            }
        }

        [Fact]
        public void VerifyExceptionHResultIsMappedToErrorCode() {
            var plugin = new ErrorPlugin();
            var client = CreateClient();
            var context = new EventPluginContext(client, new Event());
            var exception = new OutOfMemoryException("Test");
            context.ContextData.SetException(exception);

            plugin.Run(context);

            Assert.False(context.Cancel);
            Assert.Equal(Event.KnownTypes.Error, context.Event.Type);

            var error = context.Event.GetError();
            Assert.NotNull(error);

            // <see href="https://docs.microsoft.com/en-us/windows/win32/seccrypto/common-hresult-values"/>
            Assert.Equal(unchecked((int)0x8007000E).ToString(), error.Code);
        }
    }
}
