using System;
using System.Reflection;
using Exceptionless.Dependency;
using Exceptionless.Plugins.Default;
using Exceptionless.Logging;
using Exceptionless.Storage;

#if !PORTABLE && !NETSTANDARD1_2
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Exceptionless.Diagnostics;
#endif

#if NET45
using System.Configuration;
using Exceptionless.Extensions;
using Exceptionless.Utility;
#endif

namespace Exceptionless {
    public static class ExceptionlessExtraConfigurationExtensions {
#if !PORTABLE && !NETSTANDARD1_2
        /// <summary>
        /// Reads the Exceptionless configuration from the app.config or web.config file.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the attribute settings to.</param>
        public static void UseErrorPlugin(this ExceptionlessConfiguration config) {
            config.RemovePlugin<SimpleErrorPlugin>();
            config.AddPlugin<Plugins.ErrorPlugin>();
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
            int maxEntriesToInclude = config.Settings.GetInt32(TraceLogPlugin.MaxEntriesToIncludeKey, defaultMaxEntriesToInclude ?? 0);

            if (!Trace.Listeners.OfType<ExceptionlessTraceListener>().Any())
                Trace.Listeners.Add(new ExceptionlessTraceListener(maxEntriesToInclude));

            if (!config.Settings.ContainsKey(TraceLogPlugin.MaxEntriesToIncludeKey) && defaultMaxEntriesToInclude.HasValue)
                config.Settings.Add(TraceLogPlugin.MaxEntriesToIncludeKey, maxEntriesToInclude.ToString());

            config.AddPlugin(typeof(TraceLogPlugin).Name, 70, c => new TraceLogPlugin(c));
        }
#endif

#if NET45 || NETSTANDARD1_4 || NETSTANDARD1_5
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
#if NETSTANDARD1_5
                config.ReadFromAttributes(Assembly.GetEntryAssembly());
#elif NET45
                config.ReadFromAttributes(Assembly.GetEntryAssembly(), Assembly.GetCallingAssembly());
#endif
            } else {
                config.ReadFromAttributes(configAttributesAssemblies);
            }

#if !PORTABLE && !NETSTANDARD
            config.ReadFromConfigSection();
            config.ReadFromAppSettings();
#endif

#if !PORTABLE && !NETSTANDARD1_2
            config.ReadFromEnvironmentalVariables();
#endif
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
                config.Resolver.GetLog().Error(typeof(ExceptionlessExtraConfigurationExtensions), ex, String.Concat("An error occurred while retrieving the configuration section. Exception: ", ex.Message));
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

            if (section.EnableLogging.HasValue && section.EnableLogging.Value) {
                if (!String.IsNullOrEmpty(section.LogPath))
                    config.UseFileLogger(section.LogPath);
                else if (!String.IsNullOrEmpty(section.StoragePath))
                    config.UseFileLogger(System.IO.Path.Combine(section.StoragePath, "exceptionless.log"));
                else if (!config.Resolver.HasRegistration<IExceptionlessLog>())
                    config.UseIsolatedStorageLogger();
            }

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
                        config.Resolver.GetLog().Error(typeof(ExceptionlessExtraConfigurationExtensions), String.Format("An error occurred while retrieving service type \"{0}\".", resolver.Service));
                        continue;
                    }

                    try {
                        Type implementationType = Type.GetType(resolver.Type);
                        if (!resolverInterface.IsAssignableFrom(implementationType)) {
                            config.Resolver.GetLog().Error(typeof(ExceptionlessExtraConfigurationExtensions), String.Format("Type \"{0}\" does not implement \"{1}\".", resolver.Type, resolver.Service));
                            continue;
                        }

                        config.Resolver.Register(resolverInterface, implementationType);
                    } catch (Exception ex) {
                        config.Resolver.GetLog().Error(typeof(ExceptionlessExtraConfigurationExtensions), ex, String.Format("An error occurred while registering service \"{0}\" implementation \"{1}\".", resolver.Service, resolver.Type));
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

#if !PORTABLE && !NETSTANDARD1_2
        /// <summary>
        /// Reads the Exceptionless configuration from Environment Variables.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the attribute settings to.</param>
        public static void ReadFromEnvironmentalVariables(this ExceptionlessConfiguration config) {
            string apiKey = GetEnvironmentalVariable("Exceptionless:ApiKey");
            if (IsValidApiKey(apiKey))
                config.ApiKey = apiKey;

            bool enabled;
            if (Boolean.TryParse(GetEnvironmentalVariable("Exceptionless:Enabled"), out enabled) && !enabled)
                config.Enabled = false;
            
            string serverUrl = GetEnvironmentalVariable("Exceptionless:ServerUrl");
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
#endif

        private static bool IsValidApiKey(string apiKey) {
            return !String.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE";
        }
    }
}
