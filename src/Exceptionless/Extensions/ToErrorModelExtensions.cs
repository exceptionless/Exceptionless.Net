using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly ConcurrentDictionary<string, CachedModule> _moduleCache = new ConcurrentDictionary<string, CachedModule>(StringComparer.Ordinal);
        private static readonly Queue<string> _cachedPaths = new Queue<string>();
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
            if (client == null)
                client = ExceptionlessClient.Default;

            var moduleIds = client.Configuration.IncludeModules ? new Dictionary<Assembly, int>() : null;
            return ToErrorModelInternal(exception, client, moduleIds);
        }

        private static Error ToErrorModelInternal(Exception exception, ExceptionlessClient client, Dictionary<Assembly, int> moduleIds, bool isInner = false) {
            var log = client.Configuration.Resolver.GetLog();
            Type type = exception.GetType();
            string typeName = type.GetTypeDisplayName();

            var error = new Error {
                Message = exception.GetMessage(typeName),
                Type = typeName
            };

            if (!isInner && moduleIds != null)
                error.Modules = GetLoadedModules(log, false, false, moduleIds);

            error.PopulateStackTrace(exception, log, moduleIds);

            try {
                PropertyInfo info = type.GetProperty("HResult", BindingFlags.Public | BindingFlags.Instance);
                if (info != null)
                    error.Code = info.GetValue(exception, null).ToString();
            } catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error populating HResult Code: " + ex.Message);
            }

#if NET45
            try {
                if (exception.TargetSite != null) {
                    error.TargetMethod = new Method();
                    error.TargetMethod.PopulateMethod(exception.TargetSite, moduleIds);
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

            if (exception.InnerException != null)
                error.Inner = ToErrorModelInternal(exception.InnerException, client, moduleIds, isInner: true);

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

        internal static ModuleCollection GetLoadedModules(IExceptionlessLog log, bool includeSystem = false, bool includeDynamic = false) {
            return GetLoadedModules(log, includeSystem, includeDynamic, null);
        }

        private static ModuleCollection GetLoadedModules(IExceptionlessLog log, bool includeSystem, bool includeDynamic, Dictionary<Assembly, int> moduleIds) {
            var modules = new ModuleCollection();
            try {
                int id = 1;
                var entryAssembly = Assembly.GetEntryAssembly();
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    if (!includeDynamic && assembly.IsDynamic)
                        continue;

                    string path = null;
                    try {
                        if (!includeDynamic) {
                            path = assembly.Location;
                            if (String.IsNullOrEmpty(path))
                                continue;
                        }
                    } catch (SecurityException ex) {
                        const string message = "An error occurred while getting the Assembly.Location value. This error will occur when when you are not running under full trust.";
                        log.Error(typeof(ExceptionlessClient), ex, message);
                    }

                    if (!includeSystem) {
                        try {
                            string publicKeyToken = assembly.GetAssemblyName().GetPublicKeyToken().ToHex();
                            if (_msPublicKeyTokens.Contains(publicKeyToken))
                                continue;

                            var attrs = assembly.GetCustomAttributes(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute)).ToList();
                            if (attrs.Count > 0)
                                continue;
                        } catch (Exception ex) {
                            log.Error(typeof(ExceptionlessClient), ex, "Error while checking if assembly " + assembly.FullName + " should be added to modules:" + ex.Message);
                        }
                    }

                    // A single large inventory must not evict every entry on each error.
                    var module = ToModuleInfo(assembly, path, entryAssembly, cacheMetadata: id <= MaxCachedModules);
                    module.ModuleId = id;
                    modules.Add(module);
                    moduleIds?.Add(assembly, id);

                    id++;
                }
            } catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error loading modules: " + ex.Message);
            }

            return modules;
        }

        private static void PopulateStackTrace(this Error error, Exception exception, IExceptionlessLog log, Dictionary<Assembly, int> moduleIds) {
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
                    stackFrame.PopulateMethod(frame.GetMethod(), moduleIds);
                } catch (Exception ex) {
                    log.Error(typeof(ExceptionlessClient), ex, "Error populating StackFrame method info: " + ex.Message);
                }

                error.StackTrace.Add(stackFrame);
            }
        }

        private static void PopulateMethod(this Method method, MethodBase methodBase, Dictionary<Assembly, int> moduleIds) {
            if (methodBase == null)
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

            method.ModuleId = moduleIds != null && moduleIds.TryGetValue(methodBase.Module.Assembly, out int moduleId) ? moduleId : -1;
        }

        public static Module ToModuleInfo(this System.Reflection.Module module) {
            return ToModuleInfo(module.Assembly);
        }

        public static Module ToModuleInfo(this Assembly assembly) {
            if (assembly == null)
                return null;

            return ToModuleInfo(assembly, null, Assembly.GetEntryAssembly(), cacheMetadata: true);
        }

        private static Module ToModuleInfo(Assembly assembly, string path, Assembly entryAssembly, bool cacheMetadata) {
            Guid versionId = Guid.Empty;
            if (cacheMetadata) {
                try {
                    if (path == null && !assembly.IsDynamic)
                        path = assembly.Location;
                    if (!String.IsNullOrEmpty(path))
                        versionId = assembly.ManifestModule.ModuleVersionId;
                } catch {
                    // Restricted reflection can still provide the usual module metadata.
                }
            }

            var module = versionId == Guid.Empty ? ReadModuleMetadata(assembly) : CopyCachedMetadata(assembly, path, versionId);
            module.IsEntry = assembly == entryAssembly;
            return module;
        }

        private static Module CopyCachedMetadata(Assembly assembly, string path, Guid versionId) {
            if (!_moduleCache.TryGetValue(path, out var cached) || cached.VersionId != versionId) {
                cached = new CachedModule(versionId, ReadModuleMetadata(assembly));
                lock (_cachedPaths) {
                    if (!_moduleCache.ContainsKey(path)) {
                        if (_cachedPaths.Count == MaxCachedModules)
                            _moduleCache.TryRemove(_cachedPaths.Dequeue(), out _);
                        _cachedPaths.Enqueue(path);
                    }
                    _moduleCache[path] = cached;
                }
            }

            var metadata = cached.Metadata;
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

        private static Module ReadModuleMetadata(Assembly assembly) {
            var mod = new Module();
            var name = assembly.GetAssemblyName();
            string infoVersion = assembly.GetInformationalVersion();
            string fileVersion = assembly.GetFileVersion();

            if (name != null) {
                mod.Name = name.Name;
                mod.Version = infoVersion ?? fileVersion ?? name.Version.ToString();
                byte[] pkt = name.GetPublicKeyToken();
                if (pkt.Length > 0)
                    mod.Data["PublicKeyToken"] = pkt.ToHex();

                var version = name.Version.ToString();
                if (!String.IsNullOrEmpty(version) && version != mod.Version)
                    mod.Data["Version"] = name.Version.ToString();
            }

            if (!String.IsNullOrEmpty(infoVersion) && infoVersion != mod.Version)
                mod.Data["ProductVersion"] = infoVersion;

            if (!String.IsNullOrEmpty(fileVersion) && fileVersion != mod.Version)
                mod.Data["FileVersion"] = fileVersion;

            DateTime? creationTime = assembly.GetCreationTime();
            if (creationTime.HasValue)
                mod.CreatedDate = creationTime.Value;

            DateTime? lastWriteTime = assembly.GetLastWriteTime();
            if (lastWriteTime.HasValue)
                mod.ModifiedDate = lastWriteTime.Value;

            return mod;
        }
    }
}
