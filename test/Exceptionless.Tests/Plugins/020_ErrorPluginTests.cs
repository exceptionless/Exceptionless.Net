using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
#if NET10_0
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
#endif
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Xunit;

namespace Exceptionless.Tests.Plugins {
    public class ErrorPluginTests : PluginTestBase {
        public ErrorPluginTests(ITestOutputHelper output) : base(output) { }

        private const string FixtureAssemblyName = "Exceptionless.ModuleIdRegressionFixture";
        private const string ModuleFixtureType = "ModuleIdFixture";
        private const string ModuleFixtureMethod = "Throw";

#if NET10_0
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WillMapDuplicateAssemblyIdentitiesToDistinctEventModules(bool loadSameFile) {
            var directory = Directory.CreateTempSubdirectory();
            try {
                // Rebuild at the same paths after unloading. Each generation has new file versions.
                for (int generation = 1; generation <= 2; generation++) {
                    WeakReference outerContext = null;
                    WeakReference innerContext = null;
                    Event capturedEvent;
                    try {
                        capturedEvent = VerifyModuleMappings(directory.FullName, loadSameFile, generation, out outerContext, out innerContext);
                    } finally {
                        for (int i = 0; i < 10 && (outerContext?.IsAlive == true || innerContext?.IsAlive == true); i++) {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }

                    Assert.False(outerContext.IsAlive);
                    Assert.False(innerContext.IsAlive);
                    GC.KeepAlive(capturedEvent);
                }
            } finally {
                directory.Delete(recursive: true);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Event VerifyModuleMappings(string directory, bool loadSameFile, int generation, out WeakReference outerReference, out WeakReference innerReference) {
            var outerContext = new AssemblyLoadContext("Outer module fixture", isCollectible: true);
            var innerContext = new AssemblyLoadContext("Inner module fixture", isCollectible: true);
            outerReference = new WeakReference(outerContext, trackResurrection: true);
            innerReference = new WeakReference(innerContext, trackResurrection: true);
            try {
                string outerVersion = $"{generation}.0.1.0";
                string innerVersion = loadSameFile ? outerVersion : $"{generation}.0.2.0";
                string outerPath = Path.Combine(directory, "outer.dll");
                string innerPath = loadSameFile ? outerPath : Path.Combine(directory, "inner.dll");
                WriteThrowingAssembly(outerPath, FixtureAssemblyName, outerVersion);
                if (!loadSameFile)
                    WriteThrowingAssembly(innerPath, FixtureAssemblyName, innerVersion);

                return VerifyCapturedModules(outerContext.LoadFromAssemblyPath(outerPath), innerContext.LoadFromAssemblyPath(innerPath), outerVersion, innerVersion);
            } finally {
                innerContext.Unload();
                outerContext.Unload();
            }
        }
#else
        [Fact]
        public void WillMapFileLoadsInAnUnloadableAppDomain() {
            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            try {
                var fixtureType = typeof(ModuleDomainFixture);
                var setup = new AppDomainSetup {
                    ApplicationBase = Path.GetDirectoryName(fixtureType.Assembly.Location),
                    ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
                };
                var domain = AppDomain.CreateDomain("Module ID fixture", null, setup);
                try {
                    var fixture = (ModuleDomainFixture)domain.CreateInstanceAndUnwrap(fixtureType.Assembly.FullName, fixtureType.FullName);
                    fixture.Verify(directory.FullName);
                } finally {
                    AppDomain.Unload(domain);
                }
            } finally {
                directory.Delete(recursive: true);
            }
        }

        public sealed class ModuleDomainFixture : MarshalByRefObject {
            public void Verify(string directory) {
                string outerPath = Path.Combine(directory, "outer.dll");
                string innerPath = Path.Combine(directory, "inner.dll");
                WriteThrowingAssembly(outerPath, FixtureAssemblyName, "1.0.1.0");
                WriteThrowingAssembly(innerPath, FixtureAssemblyName, "1.0.2.0");
                VerifyCapturedModules(Assembly.LoadFile(outerPath), Assembly.LoadFile(innerPath), "1.0.1.0", "1.0.2.0");
            }
        }
#endif

        private static Event VerifyCapturedModules(Assembly outerAssembly, Assembly innerAssembly, string outerVersion, string innerVersion) {
            Assert.Equal(outerAssembly.FullName, innerAssembly.FullName);
            Assert.NotSame(outerAssembly, innerAssembly);

            var throwOuter = (Action<Exception>)outerAssembly.GetType(ModuleFixtureType, throwOnError: true).GetMethod(ModuleFixtureMethod).CreateDelegate(typeof(Action<Exception>));
            var throwInner = (Action<Exception>)innerAssembly.GetType(ModuleFixtureType, throwOnError: true).GetMethod(ModuleFixtureMethod).CreateDelegate(typeof(Action<Exception>));
            using var client = new ExceptionlessClient(c => {
                c.IncludeModules = true;
                c.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
            });
            var events = new Event[4];
            Parallel.For(0, events.Length, i => {
                var inner = Record.Exception(() => throwInner(null));
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(Record.Exception(() => throwOuter(inner)));
                new ErrorPlugin().Run(context);
                events[i] = context.Event;
            });

            var serializer = client.Configuration.Resolver.GetJsonSerializer();
            foreach (var captured in events) {
                var error = serializer.Deserialize<Event>(serializer.Serialize(captured)).GetError(serializer);
                Assert.Equal(Enumerable.Range(1, error.Modules.Count), error.Modules.Select(m => m.ModuleId));
                var fixtureModules = error.Modules.Where(m => m.Name == FixtureAssemblyName).ToList();
                Assert.Equal(2, fixtureModules.Count);
                int outerId = Assert.Single(error.StackTrace, f => f.DeclaringType == ModuleFixtureType).ModuleId;
                int innerId = Assert.Single(error.Inner.StackTrace, f => f.DeclaringType == ModuleFixtureType).ModuleId;
                Assert.NotEqual(outerId, innerId);
                Assert.Equal(outerVersion, Assert.Single(fixtureModules, m => m.ModuleId == outerId).Version);
                Assert.Equal(innerVersion, Assert.Single(fixtureModules, m => m.ModuleId == innerId).Version);
#if NET45
                Assert.Equal(outerId, error.TargetMethod.ModuleId);
                Assert.Equal(innerId, error.Inner.TargetMethod.ModuleId);
#endif
                var module = Assert.Single(captured.GetError().Modules, m => m.ModuleId == outerId);
                Assert.Equal(File.GetCreationTimeUtc(outerAssembly.Location), module.CreatedDate);
                Assert.Equal(File.GetLastWriteTimeUtc(outerAssembly.Location), module.ModifiedDate);
                Assert.Equal("1.0.0.0", module.Data["Version"]);
                module.Data["Version"] = "changed on the event";
            }

            var nextInner = Record.Exception(() => throwInner(null));
            var outer = Record.Exception(() => throwOuter(nextInner));
            var nextError = outer.ToErrorModel(client);
            int nextId = Assert.Single(nextError.StackTrace, f => f.DeclaringType == ModuleFixtureType).ModuleId;
            var nextModule = Assert.Single(nextError.Modules, m => m.ModuleId == nextId);
            Assert.Equal("1.0.0.0", nextModule.Data["Version"]);
            var filtered = Record.Exception(() => Enumerable.Empty<int>().First()).ToErrorModel(client);
            var frameworkFrames = filtered.StackTrace.Where(f => f.DeclaringNamespace == typeof(Enumerable).Namespace).ToList();
            Assert.NotEmpty(frameworkFrames);
            Assert.All(frameworkFrames, frame => Assert.Equal(-1, frame.ModuleId));
            var simple = outer.ToSimpleErrorModel(client);
            Assert.Equal(2, simple.Modules.Where(m => m.Name == FixtureAssemblyName).Select(m => m.ModuleId).Distinct().Count());

            client.Configuration.IncludeModules = false;
            var withoutModules = outer.ToErrorModel(client);
            Assert.Empty(withoutModules.Modules);
            Assert.Equal(-1, Assert.Single(withoutModules.Inner.StackTrace, f => f.DeclaringType == ModuleFixtureType).ModuleId);
            return events[0];
        }

        private static void WriteThrowingAssembly(string path, string name, string fileVersion, bool ambiguousInformationalVersion = false) {
            var identity = new AssemblyName(name) { Version = new Version(1, 0, 0, 0) };
#if NET10_0
            var assembly = new PersistedAssemblyBuilder(identity, typeof(object).Assembly);
            var module = assembly.DefineDynamicModule(name);
#else
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(identity, AssemblyBuilderAccess.Save, Path.GetDirectoryName(path));
            var module = assembly.DefineDynamicModule(name, Path.GetFileName(path));
#endif
            var versionConstructor = typeof(AssemblyFileVersionAttribute).GetConstructor(new[] { typeof(string) });
            assembly.SetCustomAttribute(new CustomAttributeBuilder(versionConstructor, new object[] { fileVersion }));
            if (ambiguousInformationalVersion) {
                var constructor = typeof(AssemblyInformationalVersionAttribute).GetConstructor(new[] { typeof(string) });
                assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { "first" }));
                assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { "second" }));
            }
            var type = module.DefineType(ModuleFixtureType, TypeAttributes.Public);
            var method = type.DefineMethod(ModuleFixtureMethod, MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(Exception) });
            method.SetImplementationFlags(MethodImplAttributes.NoInlining);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldstr, "Synthetic module exception");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string), typeof(Exception) }));
            il.Emit(OpCodes.Throw);
            type.CreateType();
#if NET10_0
            assembly.Save(path);
#else
            assembly.Save(Path.GetFileName(path));
#endif
        }

        [Fact]
        public void WillHonorDynamicAndLocationlessModuleFiltering() {
            var name = new AssemblyName("Synthetic.Dynamic" + Guid.NewGuid().ToString("N")) { Version = new Version(2, 3, 4, 5) };
#if NET10_0
            var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
#else
            var dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
#endif
            var dynamicModule = dynamicAssembly.DefineDynamicModule(name.Name);
            Assert.Equal("2.3.4.5", dynamicModule.ToModuleInfo().Version);

            string memoryName = "Synthetic.InMemory" + Guid.NewGuid().ToString("N");
            string path = Path.Combine(Path.GetTempPath(), memoryName + ".dll");
            try {
                WriteThrowingAssembly(path, memoryName, "1.2.3.4", ambiguousInformationalVersion: true);
                var memoryAssembly = Assembly.Load(File.ReadAllBytes(path));
                Assert.Empty(memoryAssembly.Location);
                Assert.Equal("1.2.3.4", memoryAssembly.ToModuleInfo().Version);

                var log = new Logging.NullExceptionlessLog();
                var regular = ToErrorModelExtensions.GetLoadedModules(log);
                Assert.DoesNotContain(regular, m => m.Name == name.Name || m.Name == memoryName);
                var system = ToErrorModelExtensions.GetLoadedModules(log, includeSystem: true);
                Assert.Contains(system, m => m.Name == typeof(object).Assembly.GetName().Name);
                string linqAssembly = typeof(Enumerable).Assembly.GetName().Name;
                Assert.Contains(system, m => m.Name == linqAssembly);
                var all = ToErrorModelExtensions.GetLoadedModules(log, includeDynamic: true);
                Assert.DoesNotContain(all, m => m.Name == linqAssembly);
                Assert.Contains(all, m => m.Name == name.Name && m.Version == "2.3.4.5");
                Assert.Contains(all, m => m.Name == memoryName && m.Version == "1.2.3.4");

                var generated = typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).GetConstructor(new[] { typeof(string), typeof(string) });
                dynamicAssembly.SetCustomAttribute(new CustomAttributeBuilder(generated, new object[] { "Synthetic", "1" }));
                Assert.DoesNotContain(ToErrorModelExtensions.GetLoadedModules(log, includeDynamic: true), m => m.Name == name.Name);

                // A malformed attribute on one assembly must not interrupt the inventory.
                var fileVersion = typeof(AssemblyFileVersionAttribute).GetConstructor(new[] { typeof(string) });
                dynamicAssembly.SetCustomAttribute(fileVersion, new byte[] { 0, 0 });
                Assert.Null(dynamicModule.ToModuleInfo());
                var remaining = ToErrorModelExtensions.GetLoadedModules(log, includeSystem: true, includeDynamic: true);
                Assert.DoesNotContain(remaining, m => m.Name == name.Name);
                Assert.Contains(remaining, m => m.Name == memoryName && m.Version == "1.2.3.4");
            } finally {
                File.Delete(path);
            }
        }

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
