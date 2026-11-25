using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Module = Exceptionless.Models.Data.Module;
using StackFrame = System.Diagnostics.StackFrame;

namespace Exceptionless {
    internal static class ToErrorModelExtensions {
        private const int MaxCachedModules = 1024;
        private static readonly ConcurrentDictionary<string, CachedModule> _moduleCache = new();
        private static readonly Queue<string> _cachedPaths = new();
        private static readonly string[] _exceptionExclusions = {
            "@exceptionless", "Data", "HelpLink", "ExceptionContext", "InnerExceptions", "InnerException", "Errors", "Types",
            "Message", "Source", "StackTrace", "TargetSite", "HResult",
            "Entries", "StateEntries",  "PersistedState", "Results"
        };

        /// <summary>
        /// Sets the properties from an exception.
        /// </summary>
        /// <param name="exception">The exception to populate properties from.</param>
        /// <param name="client">
        /// The ExceptionlessClient instance used for configuration. If a client is not specified, it will use
        /// ExceptionlessClient.Default.
        /// </param>
        public static Error ToErrorModel(this Exception exception, ExceptionlessClient client = null) {
            if (client is null)
                client = ExceptionlessClient.Default;

            return ToErrorModelInternal(exception, client);
        }

        private static Error ToErrorModelInternal(Exception exception, ExceptionlessClient client, List<Assembly> moduleAssemblies = null, bool isInner = false) {
            var log = client.Configuration.Resolver.GetLog();
            Type type = exception.GetType();
            string typeName = type.GetTypeDisplayName();

            var error = new Error {
                Message = exception.GetMessage(typeName),
                Type = typeName
            };

            if (!isInner && client.Configuration.IncludeModules) {
                // Rebuild the frame lookup once per exception chain because assemblies can load or unload.
                // Only file-backed module metadata is reused across errors.
                moduleAssemblies = new List<Assembly>();
                error.Modules = GetLoadedModules(log, moduleAssemblies: moduleAssemblies);
            }

            error.PopulateStackTrace(moduleAssemblies, exception, log);

            try {
                PropertyInfo info = type.GetProperty("HResult", BindingFlags.Public | BindingFlags.Instance);
                if (info != null)
                    error.Code = info.GetValue(exception, null).ToString();
            } catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error populating HResult Code: " + ex.Message);
            }

#if NET45
            try {
                if (exception.TargetSite is not null) {
                    error.TargetMethod = new Method();
                    error.TargetMethod.PopulateMethod(moduleAssemblies, exception.TargetSite);
                }
            } catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error populating TargetMethod: " + ex.Message);
            }
#endif

            var exclusions = _exceptionExclusions.Union(client.Configuration.DataExclusions).ToList();
            try {
                if (exception.Data != null) {
                    foreach (object k in exception.Data.Keys) {
                        string key = k != null ? k.ToString() : null;
                        if (String.IsNullOrEmpty(key) || key.AnyWildcardMatches(exclusions, true))
                            continue;

                        var item = exception.Data[k];
                        if (item == null)
                            continue;

                        error.Data[key] = item;
                    }
                }
            } catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error populating Data: " + ex.Message);
            }

            try {
                var extraProperties = type.GetPublicProperties().Where(p => !p.Name.AnyWildcardMatches(exclusions, true)).ToDictionary(p => p.Name, p => {
                    try {
                        return p.GetValue(exception, null);
                    } catch (Exception ex) {
                        log.Error(typeof(ExceptionlessClient), ex, String.Format("Error getting extra exception property {0} value: {1}", p.Name, ex.Message));
                    }
                    return null;
                });

                extraProperties = extraProperties.Where(kvp => !ValueIsEmpty(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (extraProperties.Count > 0 && !error.Data.ContainsKey(Error.KnownDataKeys.ExtraProperties)) {
                    error.AddObject(new ExtendedDataInfo {
                        Data = extraProperties,
                        Name = Error.KnownDataKeys.ExtraProperties,
                        IgnoreSerializationErrors = true,
                        MaxDepthToSerialize = 5
                    }, client);
                }
            } catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error populating extra exception properties: " + ex.Message);
            }

            if (exception.InnerException is not null)
                error.Inner = ToErrorModelInternal(exception.InnerException, client, moduleAssemblies, isInner: true);

            return error;
        }

        private static bool ValueIsEmpty(object value) {
            if (value == null)
                return true;

            if (value is IEnumerable) {
                if (!(value as IEnumerable).Cast<Object>().Any())
                    return true;
            }

            return false;
        }

        private static readonly List<string> _msPublicKeyTokens = new List<string> {
            "b77a5c561934e089",
            "b03f5f7f11d50a3a",
            "31bf3856ad364e35"
        };

        private static string GetMessage(this Exception exception, string typeName) {
            string defaultMessage = $"Exception of type '{typeName}' was thrown.";
            string message = !String.IsNullOrEmpty(exception.Message) ? exception.Message.Trim() : null;
            return !String.IsNullOrEmpty(message) ? message : defaultMessage;
        }

        internal static ModuleCollection GetLoadedModules(IExceptionlessLog log, bool includeSystem = false, bool includeDynamic = false, List<Assembly> moduleAssemblies = null) {
            var modules = new ModuleCollection();
            try {
                var entryAssembly = Assembly.GetEntryAssembly();
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    // A single large inventory must not evict every entry on each error.
                    bool cacheMetadata = modules.Count < MaxCachedModules;
                    var module = TryGetModuleInfo(assembly, entryAssembly, log, includeSystem, includeDynamic, cacheMetadata);
                    if (module is null)
                        continue;

                    module.ModuleId = modules.Count + 1;
                    modules.Add(module);
                    moduleAssemblies?.Add(assembly);
                }
            } catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error loading modules: " + ex.Message);
            }

            return modules;
        }

        private static void PopulateStackTrace(this Error error, List<Assembly> moduleAssemblies, Exception exception, IExceptionlessLog log) {
            StackFrame[] frames = null;
            try {
                var st = new EnhancedStackTrace(exception);
                frames = st.GetFrames();
            }
            catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error getting stack frames: " + ex.Message);
            }

            if (frames == null || frames.Length == 0) {
                log.Debug(typeof(ExceptionlessClient), "Error " + error.Message + " contained no stack frames");
                return;
            }

            foreach (StackFrame frame in frames) {
                var stackFrame = new Models.Data.StackFrame {
                    LineNumber = frame.GetFileLineNumber(),
                    Column = frame.GetFileColumnNumber(),
                    FileName = frame.GetFileName()
                };

                try {
                    stackFrame.Data["ILOffset"] = frame.GetILOffset();
#if NET45
                    stackFrame.Data["NativeOffset"] = frame.GetNativeOffset();
#endif
                } catch (Exception ex) {
                    log.Error(typeof(ExceptionlessClient), ex, "Error populating StackFrame offset info: " + ex.Message);
                }

                try {
                    stackFrame.PopulateMethod(moduleAssemblies, frame.GetMethod());
                } catch (Exception ex) {
                    log.Error(typeof(ExceptionlessClient), ex, "Error populating StackFrame method info: " + ex.Message);
                }

                error.StackTrace.Add(stackFrame);
            }
        }

        private static void PopulateMethod(this Method method, List<Assembly> moduleAssemblies, MethodBase methodBase) {
            if (methodBase is null)
                return;

            method.Name = methodBase.Name;
            if (methodBase.DeclaringType != null) {
                method.DeclaringNamespace = methodBase.DeclaringType.Namespace;
                if (methodBase.DeclaringType.GetTypeInfo().MemberType == MemberTypes.NestedType && methodBase.DeclaringType.DeclaringType != null)
                    method.DeclaringType = methodBase.DeclaringType.DeclaringType.Name + "+" + methodBase.DeclaringType.Name;
                else
                    method.DeclaringType = methodBase.DeclaringType.Name;
            }

            //method.Data["Attributes"] = (int)methodBase.Attributes;
            if (methodBase.IsGenericMethod) {
                foreach (Type type in methodBase.GetGenericArguments())
                    method.GenericArguments.Add(type.Name);
            }

            foreach (ParameterInfo parameter in methodBase.GetParameters()) {
                var parm = new Parameter {
                    Name = parameter.Name,
                    Type = parameter.ParameterType.Name,
                    TypeNamespace = parameter.ParameterType.Namespace
                };

                parm.Data["IsIn"] = parameter.IsIn;
                parm.Data["IsOut"] = parameter.IsOut;
                parm.Data["IsOptional"] = parameter.IsOptional;

                if (parameter.ParameterType.IsGenericParameter) {
                    foreach (Type type in parameter.ParameterType.GetGenericArguments())
                        parm.GenericArguments.Add(type.Name);
                }

                method.Parameters.Add(parm);
            }

            method.ModuleId = GetModuleId(moduleAssemblies, methodBase);
        }

        private static int GetModuleId(List<Assembly> moduleAssemblies, MethodBase method) {
            if (moduleAssemblies is null)
                return -1;

            var assembly = method.Module.Assembly;
            for (int i = 0; i < moduleAssemblies.Count; i++) {
                if (ReferenceEquals(moduleAssemblies[i], assembly))
                    return i + 1;
            }

            return -1;
        }

        public static Module ToModuleInfo(this System.Reflection.Module module) {
            return ToModuleInfo(module.Assembly);
        }

        public static Module ToModuleInfo(this Assembly assembly) {
            if (assembly is null)
                return null;

            return TryGetModuleInfo(assembly, Assembly.GetEntryAssembly(), log: null, includeSystem: true, includeDynamic: true);
        }

        private static Module TryGetModuleInfo(Assembly assembly, Assembly entryAssembly, IExceptionlessLog log,
            bool includeSystem, bool includeDynamic, bool cacheMetadata = true) {
            try {
                string path = null;
                try {
                    if (!assembly.IsDynamic)
                        path = assembly.Location;
                    if (!includeDynamic && String.IsNullOrEmpty(path))
                        return null;
                } catch (Exception ex) when (includeDynamic || ex is SecurityException) {
                    log?.Error(typeof(ExceptionlessClient), ex, "Assembly location is unavailable.");
                }

                var module = GetModuleMetadata(assembly, path, cacheMetadata, includeSystem, log);
                if (module is not null)
                    module.IsEntry = ReferenceEquals(assembly, entryAssembly);
                return module;
            } catch (Exception ex) {
                log?.Error(typeof(ExceptionlessClient), ex, "Error reading module metadata.");
                return null;
            }
        }

        private static Module GetModuleMetadata(Assembly assembly, string path, bool cacheMetadata, bool includeSystem, IExceptionlessLog log) {
            Guid versionId = Guid.Empty;
            if (cacheMetadata && !String.IsNullOrEmpty(path)) {
                try {
                    versionId = assembly.ManifestModule.ModuleVersionId;
                } catch {
                    // Restricted reflection can still provide the usual module metadata.
                }
            }

            Module metadata = null;
            if (versionId != Guid.Empty && _moduleCache.TryGetValue(path, out var cached) && cached.VersionId == versionId)
                metadata = cached.Metadata;

            AssemblyName name = null;
            string publicKeyToken = null;
            if (metadata is null) {
                name = assembly.GetAssemblyName();
                if (name?.GetPublicKeyToken() is { Length: > 0 } token)
                    publicKeyToken = token.ToHex();
            } else {
                publicKeyToken = metadata.Data.GetString("PublicKeyToken");
            }

            if (!includeSystem) {
                try {
                    if (_msPublicKeyTokens.Contains(publicKeyToken) || assembly.IsDefined(typeof(GeneratedCodeAttribute), inherit: false))
                        return null;
                } catch (Exception ex) {
                    log.Error(typeof(ExceptionlessClient), ex, "Error while checking if assembly " + assembly.FullName + " should be added to modules:" + ex.Message);
                }
            }

            if (metadata is null) {
                metadata = ReadModuleMetadata(assembly, name, publicKeyToken, path);
                if (versionId == Guid.Empty)
                    return metadata;

                // Publish and evict under the same lock so each cached path is queued exactly once.
                lock (_cachedPaths) {
                    if (!_moduleCache.ContainsKey(path)) {
                        if (_cachedPaths.Count >= MaxCachedModules)
                            _moduleCache.TryRemove(_cachedPaths.Dequeue(), out _);
                        _cachedPaths.Enqueue(path);
                    }
                    _moduleCache[path] = new CachedModule(versionId, metadata);
                }
            }

            var module = new Module {
                Name = metadata.Name,
                Version = metadata.Version,
                CreatedDate = metadata.CreatedDate,
                ModifiedDate = metadata.ModifiedDate
            };
            foreach (var entry in metadata.Data)
                module.Data.Add(entry.Key, entry.Value);
            return module;
        }

        private sealed class CachedModule {
            public CachedModule(Guid versionId, Module metadata) {
                VersionId = versionId;
                Metadata = metadata;
            }

            public Guid VersionId { get; }
            public Module Metadata { get; }
        }

        private static Module ReadModuleMetadata(Assembly assembly, AssemblyName name, string publicKeyToken, string path) {
            var mod = new Module();
            string infoVersion = assembly.GetInformationalVersion();
            string fileVersion = assembly.GetFileVersion();

            if (name is not null) {
                mod.Name = name.Name;
                string version = name.Version?.ToString();
                mod.Version = infoVersion ?? fileVersion ?? version;
                if (publicKeyToken is not null)
                    mod.Data["PublicKeyToken"] = publicKeyToken;

                if (!String.IsNullOrEmpty(version) && version != mod.Version)
                    mod.Data["Version"] = version;
            }

            if (!String.IsNullOrEmpty(infoVersion) && infoVersion != mod.Version)
                mod.Data["ProductVersion"] = infoVersion;

            if (!String.IsNullOrEmpty(fileVersion) && fileVersion != mod.Version)
                mod.Data["FileVersion"] = fileVersion;

            if (!String.IsNullOrEmpty(path)) {
                try {
                    var file = new FileInfo(path);
                    mod.CreatedDate = file.CreationTimeUtc;
                    mod.ModifiedDate = file.LastWriteTimeUtc;
                    return mod;
                } catch {
                    // Try each timestamp independently on restricted or unavailable files.
                }
            }

            mod.CreatedDate = assembly.GetCreationTime().GetValueOrDefault();
            mod.ModifiedDate = assembly.GetLastWriteTime().GetValueOrDefault();

            return mod;
        }
    }
}
