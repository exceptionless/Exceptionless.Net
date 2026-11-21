using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        private static readonly ConditionalWeakTable<Assembly, Module> _moduleMetadata = new ConditionalWeakTable<Assembly, Module>();
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
                error.Modules = GetLoadedModules(log, moduleIds: moduleIds);

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

        internal static ModuleCollection GetLoadedModules(IExceptionlessLog log, bool includeSystem = false, bool includeDynamic = false, Dictionary<Assembly, int> moduleIds = null) {
            var modules = new ModuleCollection();
            try {
                int id = 1;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    if (!includeDynamic && assembly.IsDynamic)
                        continue;

                    try {
                        if (!includeDynamic && String.IsNullOrEmpty(assembly.Location))
                            continue;
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

                    var module = assembly.ToModuleInfo();
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

            var metadata = _moduleMetadata.GetValue(assembly, a => {
                var mod = new Module();
                var name = a.GetAssemblyName();
                string infoVersion = a.GetInformationalVersion();
                string fileVersion = a.GetFileVersion();

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

                DateTime? creationTime = a.GetCreationTime();
                if (creationTime.HasValue)
                    mod.CreatedDate = creationTime.Value;

                DateTime? lastWriteTime = a.GetLastWriteTime();
                if (lastWriteTime.HasValue)
                    mod.ModifiedDate = lastWriteTime.Value;

                if (a == Assembly.GetEntryAssembly())
                    mod.IsEntry = true;

                return mod;
            });

            return new Module {
                Name = metadata.Name,
                Version = metadata.Version,
                IsEntry = metadata.IsEntry,
                CreatedDate = metadata.CreatedDate,
                ModifiedDate = metadata.ModifiedDate,
                Data = new DataDictionary(metadata.Data)
            };
        }
    }
}
