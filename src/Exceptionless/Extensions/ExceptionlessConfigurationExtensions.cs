using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using Exceptionless.Configuration;
using Exceptionless.Dependency;
using Exceptionless.Plugins.Default;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Exceptionless.Storage;
using Exceptionless.Diagnostics;

#if NET45
using System.Configuration;
using Exceptionless.Extensions;
using Exceptionless.Utility;
#endif

namespace Exceptionless {
    public static class ExceptionlessConfigurationExtensions {
        private const string INSTALL_ID_KEY = "ExceptionlessInstallId";

        /// <summary>
        /// Automatically set the application version for events.
        /// </summary>
        public static void SetVersion(this ExceptionlessConfiguration config, string version) {
            if (String.IsNullOrEmpty(version))
                return;

            config.DefaultData[Event.KnownDataKeys.Version] = version;
        }

        /// <summary>
        /// Automatically set the application version for events.
        /// </summary>
        public static void SetVersion(this ExceptionlessConfiguration config, Version version) {
            if (version == null)
                return;

            config.DefaultData[Event.KnownDataKeys.Version] = version.ToString();
        }

        /// <summary>
        /// Automatically set the user identity (ie. email address, username, user id) on events.
        /// </summary>
        /// <param name="config">The configuration object</param>
        /// <param name="identity">The user's identity that the event happened to.</param>
        public static void SetUserIdentity(this ExceptionlessConfiguration config, string identity) {
            config.SetUserIdentity(identity, null);
        }

        /// <summary>
        /// Automatically set the user identity (ie. email address, username, user id) on events.
        /// </summary>
        /// <param name="config">The configuration object</param>
        /// <param name="identity">The user's identity that the event happened to.</param>
        /// <param name="name">The user's friendly name that the event happened to.</param>
        public static void SetUserIdentity(this ExceptionlessConfiguration config, string identity, string name) {
            if (String.IsNullOrWhiteSpace(identity) && String.IsNullOrWhiteSpace(name))
                return;

            config.DefaultData[Event.KnownDataKeys.UserInfo] = new UserInfo(identity, name);
        }

        /// <summary>
        /// Automatically set the user identity (ie. email address, username, user id) on events.
        /// </summary>
        /// <param name="config">The configuration object</param>
        /// <param name="userInfo">The user's identity that the event happened to.</param>
        public static void SetUserIdentity(this ExceptionlessConfiguration config, UserInfo userInfo) {
            if (userInfo == null)
                return;

            config.DefaultData[Event.KnownDataKeys.UserInfo] = userInfo;
        }

        public static string GetQueueName(this ExceptionlessConfiguration config) {
            return !String.IsNullOrEmpty(config.ApiKey) && config.ApiKey.Length > 7 ? config.ApiKey.Substring(0, 8) : null;
        }

        public static string GetInstallId(this ExceptionlessConfiguration config) {
            if (config == null)
                return null;

            var persistedClientData = config.Resolver.Resolve<PersistedDictionary>();
            if (persistedClientData == null)
                return null;

            if (!persistedClientData.ContainsKey(INSTALL_ID_KEY))
                persistedClientData[INSTALL_ID_KEY] = Guid.NewGuid().ToString("N");

            return persistedClientData[INSTALL_ID_KEY];
        }

        /// <summary>
        /// Automatically send session start, session heartbeats and session end events.
        /// </summary>
        /// <param name="config">Exceptionless configuration</param>
        /// <param name="sendHeartbeats">Controls whether heartbeat events are sent on an interval.</param>
        /// <param name="heartbeatInterval">The interval at which heartbeats are sent after the last sent event. The default is 1 minutes.</param>
        /// <param name="useSessionIdManagement">Allows you to manually control the session id. This is only recommended for single user desktop environments.</param>
        public static void UseSessions(this ExceptionlessConfiguration config, bool sendHeartbeats = true, TimeSpan? heartbeatInterval = null, bool useSessionIdManagement = false) {
            config.SessionsEnabled = true;

            if (useSessionIdManagement)
                config.AddPlugin<SessionIdManagementPlugin>();

            if (sendHeartbeats)
                config.AddPlugin(new HeartbeatPlugin(heartbeatInterval));
            else
                config.RemovePlugin<HeartbeatPlugin>();
        }

        public static void ApplySavedServerSettings(this ExceptionlessConfiguration config) {
            SettingsManager.ApplySavedServerSettings(config);
        }

        /// <summary>
        /// Automatically set a reference id for error events.
        /// </summary>
        public static void UseReferenceIds(this ExceptionlessConfiguration config) {
            config.AddPlugin<ReferenceIdPlugin>();
        }

        /// <summary>
        /// Reads the <see cref="ExceptionlessAttribute" /> and <see cref="ExceptionlessSettingAttribute" />
        /// from the passed in assembly.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the attribute settings to.</param>
        /// <param name="assemblies">The assembly that contains the Exceptionless configuration attributes.</param>
        public static void ReadFromAttributes(this ExceptionlessConfiguration config, params Assembly[] assemblies) {
            if (config == null)
                throw new ArgumentNullException("config");

            config.ReadFromAttributes(assemblies.ToList());
        }

        /// <summary>
        /// Reads the <see cref="ExceptionlessAttribute" /> and <see cref="ExceptionlessSettingAttribute" />
        /// from the passed in assemblies.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the attribute settings to.</param>
        /// <param name="assemblies">A list of assemblies that should be checked for the Exceptionless configuration attributes.</param>
        public static void ReadFromAttributes(this ExceptionlessConfiguration config, ICollection<Assembly> assemblies = null) {
            if (config == null)
                throw new ArgumentNullException("config");

            if (assemblies == null) {
                Assembly callingAssembly = null;
                try {
                    callingAssembly = Assembly.GetCallingAssembly();
                } catch (PlatformNotSupportedException) { }

                assemblies = new List<Assembly> { callingAssembly };
            }

            assemblies = assemblies.Where(a => a != null).Distinct().ToList();

            try {
                foreach (var assembly in assemblies) {
                    var attr = assembly.GetCustomAttributes(typeof(ExceptionlessAttribute)).FirstOrDefault() as ExceptionlessAttribute;
                    if (attr == null)
                        continue;

                    if (!attr.Enabled)
                        config.Enabled = false;

                    if (!String.IsNullOrEmpty(attr.ApiKey) && attr.ApiKey != "API_KEY_HERE")
                        config.ApiKey = attr.ApiKey;
                    if (!String.IsNullOrEmpty(attr.ServerUrl))
                        config.ServerUrl = attr.ServerUrl;

                    break;
                }

                foreach (var assembly in assemblies) {
                    var attributes = assembly.GetCustomAttributes(typeof(ExceptionlessSettingAttribute));
                    foreach (var attribute in attributes.OfType<ExceptionlessSettingAttribute>()) {
                        if (!String.IsNullOrEmpty(attribute.Name))
                            config.Settings[attribute.Name] = attribute.Value;
                    }
                }
            } catch (Exception ex) {
                var log = config.Resolver.GetLog();
                log.Error(ex, "Error while reading attribute configuration. Please contact support for more information.");
            }
        }

        public static void UseLogger(this ExceptionlessConfiguration config, IExceptionlessLog logger) {
            config.Resolver.Register<IExceptionlessLog>(new SafeExceptionlessLog(logger));
        }

        public static InMemoryExceptionlessLog UseInMemoryLogger(this ExceptionlessConfiguration config, LogLevel minLogLevel = null) {
            if (minLogLevel == null)
                minLogLevel = LogLevel.Info;

            var logger = new InMemoryExceptionlessLog { MinimumLogLevel = minLogLevel };
            config.Resolver.Register<IExceptionlessLog>(logger);

            return logger;
        }

        public static void UseInMemoryStorage(this ExceptionlessConfiguration config) {
            config.Resolver.Register<IObjectStorage, InMemoryObjectStorage>();
        }

        public static void UseTraceLogger(this ExceptionlessConfiguration config, LogLevel minLogLevel = null) {
            config.Resolver.Register<IExceptionlessLog>(new TraceExceptionlessLog { MinimumLogLevel = minLogLevel ?? LogLevel.Info });
        }

        public static void UseFileLogger(this ExceptionlessConfiguration config, string logPath, LogLevel minLogLevel = null) {
            var log = new SafeExceptionlessLog(new FileExceptionlessLog(logPath)) { MinimumLogLevel = minLogLevel ?? LogLevel.Info };
            config.Resolver.Register<IExceptionlessLog>(log);
        }

        public static void UseFolderStorage(this ExceptionlessConfiguration config, string folder) {
            config.Resolver.Register<IObjectStorage>(new FolderObjectStorage(config.Resolver, folder));
        }

        public static void UseTraceLogEntriesPlugin(this ExceptionlessConfiguration config, int? defaultMaxEntriesToInclude = null) {
            try {
                int maxEntriesToInclude = config.Settings.GetInt32(TraceLogPlugin.MaxEntriesToIncludeKey, defaultMaxEntriesToInclude ?? 0);

                if (!Trace.Listeners.OfType<ExceptionlessTraceListener>().Any())
                    Trace.Listeners.Add(new ExceptionlessTraceListener(maxEntriesToInclude));

                if (!config.Settings.ContainsKey(TraceLogPlugin.MaxEntriesToIncludeKey) && defaultMaxEntriesToInclude.HasValue)
                    config.Settings.Add(TraceLogPlugin.MaxEntriesToIncludeKey, maxEntriesToInclude.ToString());

                config.AddPlugin(typeof(TraceLogPlugin).Name, 70, c => new TraceLogPlugin(c));
            } catch (Exception ex) {
                config.Resolver.GetLog().Error(typeof(ExceptionlessConfigurationExtensions), ex, String.Concat("Error adding ExceptionlessTraceListener: ", ex.Message));
            }
        }

#if NET45
        public static void UseIsolatedStorageLogger(this ExceptionlessConfiguration config, LogLevel minLogLevel = null) {
            var log = new SafeExceptionlessLog(new IsolatedStorageFileExceptionlessLog("exceptionless.log")) { MinimumLogLevel = minLogLevel ?? LogLevel.Info };
            config.Resolver.Register<IExceptionlessLog>(log);
        }

        public static void UseIsolatedStorage(this ExceptionlessConfiguration config) {
            config.Resolver.Register<IObjectStorage>(new IsolatedStorageObjectStorage(config.Resolver));
        }
#endif

        public static void ReadAllConfig(this ExceptionlessConfiguration config, params Assembly[] configAttributesAssemblies) {
            if (configAttributesAssemblies == null || configAttributesAssemblies.Length == 0) {
                Assembly callingAssembly = null;
                try {
                    callingAssembly = Assembly.GetCallingAssembly();
                } catch (PlatformNotSupportedException) { }

                config.ReadFromAttributes(Assembly.GetEntryAssembly(), callingAssembly);
            } else {
                config.ReadFromAttributes(configAttributesAssemblies);
            }

#if !NETSTANDARD
            config.ReadFromConfigSection();
            config.ReadFromAppSettings();
#endif

            config.ReadFromEnvironmentalVariables();
            config.ApplySavedServerSettings();
        }

#if NET45
        /// <summary>
        /// Reads the Exceptionless configuration from the app.config or web.config files configuration section.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the attribute settings to.</param>
        public static void ReadFromConfigSection(this ExceptionlessConfiguration config) {
            ExceptionlessSection section = null;

            try {
                section = ConfigurationManager.GetSection("exceptionless") as ExceptionlessSection;
            } catch (Exception ex) {
                config.Resolver.GetLog().Error(typeof(ExceptionlessConfigurationExtensions), ex, String.Concat("Error retrieving configuration section: ", ex.Message));
            }

            if (section == null)
                return;

            if (!section.Enabled)
                config.Enabled = false;

            if (IsValidApiKey(section.ApiKey))
                config.ApiKey = section.ApiKey;

            if (!String.IsNullOrEmpty(section.ServerUrl))
                config.ServerUrl = section.ServerUrl;

            if (section.QueueMaxAge.HasValue)
                config.QueueMaxAge = section.QueueMaxAge.Value;

            if (section.QueueMaxAttempts.HasValue)
                config.QueueMaxAttempts = section.QueueMaxAttempts.Value;

            if (!String.IsNullOrEmpty(section.StoragePath))
                config.UseFolderStorage(section.StoragePath);

            if (section.StorageSerializer != null) {
                if (!typeof(IStorageSerializer).GetTypeInfo().IsAssignableFrom(section.StorageSerializer)) {
                    config.Resolver.GetLog().Error(typeof(ExceptionlessConfigurationExtensions), $"The storage serializer {section.StorageSerializer} does not implemented interface {typeof(IStorageSerializer)}.");
                }
                else {
                    config.Resolver.Register(typeof(IStorageSerializer), section.StorageSerializer);
                }
            }

            if (section.EnableLogging.HasValue && section.EnableLogging.Value) {
                if (!String.IsNullOrEmpty(section.LogPath))
                    config.UseFileLogger(section.LogPath);
                else if (!String.IsNullOrEmpty(section.StoragePath))
                    config.UseFileLogger(System.IO.Path.Combine(section.StoragePath, "exceptionless.log"));
                else if (!config.Resolver.HasRegistration<IExceptionlessLog>())
                    config.UseIsolatedStorageLogger();
            }

            if (section.IncludePrivateInformation.HasValue && !section.IncludePrivateInformation.Value)
                config.IncludePrivateInformation = false;

            foreach (var tag in section.Tags.SplitAndTrim(',').Where(tag => !String.IsNullOrEmpty(tag)))
                config.DefaultTags.Add(tag);

            if (section.ExtendedData != null) {
                foreach (NameValueConfigurationElement setting in section.ExtendedData) {
                    if (!String.IsNullOrEmpty(setting.Name))
                        config.DefaultData[setting.Name] = setting.Value;
                }
            }

            if (section.Settings != null) {
                foreach (NameValueConfigurationElement setting in section.Settings) {
                    if (!String.IsNullOrEmpty(setting.Name))
                        config.Settings[setting.Name] = setting.Value;
                }
            }

            if (section.Registrations != null && section.Registrations.Count > 0) {
                var types = AssemblyHelper.GetTypes(config.Resolver.GetLog());

                foreach (RegistrationConfigElement resolver in section.Registrations) {
                    if (String.IsNullOrEmpty(resolver.Service) || String.IsNullOrEmpty(resolver.Type))
                        continue;

                    Type resolverInterface = types.FirstOrDefault(t => t.Name.Equals(resolver.Service) || t.FullName.Equals(resolver.Service));
                    if (resolverInterface == null) {
                        config.Resolver.GetLog().Error(typeof(ExceptionlessConfigurationExtensions), String.Format("Error retrieving service type \"{0}\".", resolver.Service));
                        continue;
                    }

                    try {
                        Type implementationType = Type.GetType(resolver.Type);
                        if (!resolverInterface.IsAssignableFrom(implementationType)) {
                            config.Resolver.GetLog().Error(typeof(ExceptionlessConfigurationExtensions), String.Format("Type \"{0}\" does not implement \"{1}\".", resolver.Type, resolver.Service));
                            continue;
                        }

                        config.Resolver.Register(resolverInterface, implementationType);
                    } catch (Exception ex) {
                        config.Resolver.GetLog().Error(typeof(ExceptionlessConfigurationExtensions), ex, String.Format("An error occurred while registering service \"{0}\" implementation \"{1}\".", resolver.Service, resolver.Type));
                    }
                }
            }
        }

        /// <summary>
        /// Reads the Exceptionless configuration from the app.config or web.config files app settings.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the attribute settings to.</param>
        public static void ReadFromAppSettings(this ExceptionlessConfiguration config) {
            string apiKey = ConfigurationManager.AppSettings["Exceptionless:ApiKey"];
            if (IsValidApiKey(apiKey))
                config.ApiKey = apiKey;

            bool enabled;
            if (Boolean.TryParse(ConfigurationManager.AppSettings["Exceptionless:Enabled"], out enabled) && !enabled)
                config.Enabled = false;

            string serverUrl = ConfigurationManager.AppSettings["Exceptionless:ServerUrl"];
            if (!String.IsNullOrEmpty(serverUrl))
                config.ServerUrl = serverUrl;
        }
#endif

        /// <summary>
        /// Reads the Exceptionless configuration from Environment Variables.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the attribute settings to.</param>
        public static void ReadFromEnvironmentalVariables(this ExceptionlessConfiguration config) {
            string apiKey = GetEnvironmentalVariable("Exceptionless:ApiKey") ?? GetEnvironmentalVariable("Exceptionless__ApiKey");
            if (IsValidApiKey(apiKey))
                config.ApiKey = apiKey;

            bool enabled;
            if (Boolean.TryParse(GetEnvironmentalVariable("Exceptionless:Enabled") ?? GetEnvironmentalVariable("Exceptionless__Enabled"), out enabled) && !enabled)
                config.Enabled = false;

            bool processQueueOnCompletedRequest;
            string processQueueOnCompletedRequestValue = GetEnvironmentalVariable("Exceptionless:ProcessQueueOnCompletedRequest") ??
                GetEnvironmentalVariable("Exceptionless__ProcessQueueOnCompletedRequest");

            // if we are running in a serverless environment default this config to true
            if (String.IsNullOrEmpty(processQueueOnCompletedRequestValue)) {

                // check for AWS lambda environment
                if (!String.IsNullOrEmpty(GetEnvironmentalVariable("AWS_LAMBDA_FUNCTION_NAME ")))
                    processQueueOnCompletedRequestValue = Boolean.TrueString;

                // check for azure functions environment
                if (!String.IsNullOrEmpty(GetEnvironmentalVariable("FUNCTIONS_WORKER_RUNTIME")))
                    processQueueOnCompletedRequestValue = Boolean.TrueString;
            }

            if (Boolean.TryParse(processQueueOnCompletedRequestValue, out processQueueOnCompletedRequest) && processQueueOnCompletedRequest)
                config.ProcessQueueOnCompletedRequest = true;

            string serverUrl = GetEnvironmentalVariable("Exceptionless:ServerUrl") ?? GetEnvironmentalVariable("Exceptionless__ServerUrl");
            if (!String.IsNullOrEmpty(serverUrl))
                config.ServerUrl = serverUrl;
        }

        private static Dictionary<string, string> _environmentVariables;

        private static string GetEnvironmentalVariable(string name) {
            if (String.IsNullOrEmpty(name))
                return null;

            if (_environmentVariables == null) {
                try {
                    _environmentVariables = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().ToDictionary(e => e.Key.ToString(), e => e.Value.ToString());
                } catch (Exception) {
                    _environmentVariables = new Dictionary<string, string>();
                    return null;
                }
            }

            if (!_environmentVariables.ContainsKey(name))
                return null;

            return _environmentVariables[name];
        }

        /// <summary>
        /// Add a custom server certificate validation against the thumbprint of the server certificate.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the attribute settings to.</param>
        /// <param name="thumbprint">Thumbprint of the server certificate. <example>e.g. "86481791CDAF6D7A02BEE9A649EA9F84DE84D22C"</example></param>
        public static void TrustCertificateThumbprint(this ExceptionlessConfiguration config, string thumbprint) {
            config.ServerCertificateValidationCallback = x => {
                if (x.SslPolicyErrors == SslPolicyErrors.None) return true;
                return x.Certificate != null && thumbprint != null && thumbprint.Equals(x.Certificate.Thumbprint, StringComparison.OrdinalIgnoreCase);
            };
        }

        /// <summary>
        /// Add a custom server certificate validation against the thumbprint of any of the ca certificates.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the attribute settings to.</param>
        /// <param name="thumbprint">Thumbprint of the ca certificate. <example>e.g. "afe5d244a8d1194230ff479fe2f897bbcd7a8cb4"</example></param>
        public static void TrustCAThumbprint(this ExceptionlessConfiguration config, string thumbprint) {
            config.ServerCertificateValidationCallback = x => {
                if (x.SslPolicyErrors == SslPolicyErrors.None) return true;
                if (x.Chain == null || thumbprint == null) return false;
                foreach (var ca in x.Chain.ChainElements) {
                    if (thumbprint.Equals(ca.Certificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            };
        }

        /// <summary>
        /// Disable any certificate validation. Do not use this in production.
        /// </summary>
        /// <param name="config"></param>
        [Obsolete("This will open the client to Man-in-Middle attacks. It should never be used in production.")]
        public static void SkipCertificateValidation(this ExceptionlessConfiguration config) {
            config.ServerCertificateValidationCallback = x => true;
        }

        private static bool IsValidApiKey(string apiKey) {
            return !String.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE";
        }
    }
}