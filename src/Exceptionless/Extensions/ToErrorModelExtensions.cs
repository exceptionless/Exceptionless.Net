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
        private static readonly ConcurrentDictionary<string, Module> _moduleCache = new ConcurrentDictionary<string, Module>();
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

            return ToErrorModelInternal(exception, client);
        }

        private static Error ToErrorModelInternal(Exception exception, ExceptionlessClient client, bool isInner = false) {
            var log = client.Configuration.Resolver.GetLog();
            Type type = exception.GetType();

            var error = new Error {
                Message = exception.GetMessage(),
                Type = type.FullName
            };

            if (!isInner)
                error.Modules = GetLoadedModules(log);

            error.PopulateStackTrace(error, exception, log);

            try {
                PropertyInfo info = type.GetProperty("HResult", BindingFlags.NonPublic | BindingFlags.Instance);
                if (info != null)
                    error.Code = info.GetValue(exception, null).ToString();
            } catch (Exception) { }

#if NET45
            try {
                if (exception.TargetSite != null) {
                    error.TargetMethod = new Method();
                    error.TargetMethod.PopulateMethod(error, exception.TargetSite);
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
                    } catch { }
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
            } catch { }

            if (exception.InnerException != null)
                error.Inner = ToErrorModelInternal(exception.InnerException, client, true);

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

        private static string GetMessage(this Exception exception) {
            string defaultMessage = String.Format("Exception of type '{0}' was thrown.", exception.GetType().FullName);
            string message = !String.IsNullOrEmpty(exception.Message) ? exception.Message.Trim() : null;
            return !String.IsNullOrEmpty(message) ? message : defaultMessage;
        }

        internal static ModuleCollection GetLoadedModules(IExceptionlessLog log, bool includeSystem = false, bool includeDynamic = false) {
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
                        } catch {}
                    }

                    var module = assembly.ToModuleInfo();
                    module.ModuleId = id;
                    modules.Add(module);

                    id++;
                }
            } catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error loading modules: " + ex.Message);
            }

            return modules;
        }

        private static void PopulateStackTrace(this Error error, Error root, Exception exception, IExceptionlessLog log) {
            StackFrame[] frames = null;
            try {
                var st = new EnhancedStackTrace(exception);
                frames = st.GetFrames();
            } catch {}

            if (frames == null)
                return;

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
                    stackFrame.PopulateMethod(root, frame.GetMethod());
                } catch (Exception ex) {
                    log.Error(typeof(ExceptionlessClient), ex, "Error populating StackFrame method info: " + ex.Message);
                }

                error.StackTrace.Add(stackFrame);
            }
        }

        private static void PopulateMethod(this Method method, Error root, MethodBase methodBase) {
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

            method.ModuleId = GetModuleId(root, methodBase.Module);
        }

        private static int GetModuleId(Error root, System.Reflection.Module module) {
            foreach (Module mod in root.Modules) {
                if (module.Assembly.FullName.StartsWith(mod.Name, StringComparison.OrdinalIgnoreCase))
                    return mod.ModuleId;
            }

            return -1;
        }

        public static Module ToModuleInfo(this System.Reflection.Module module) {
            return ToModuleInfo(module.Assembly);
        }

        public static Module ToModuleInfo(this Assembly assembly) {
            if (assembly == null)
                return null;

            return _moduleCache.GetOrAdd(assembly.FullName, k => {
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

                if (assembly == Assembly.GetEntryAssembly())
                    mod.IsEntry = true;

                return mod;
            });
        }
    }
}