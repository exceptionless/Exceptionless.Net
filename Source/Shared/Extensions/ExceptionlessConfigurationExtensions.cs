using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Exceptionless.Configuration;
using Exceptionless.Dependency;
using Exceptionless.Plugins.Default;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Plugins;
using Exceptionless.Storage;

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

        public static InMemoryExceptionlessLog UseInMemoryLogger(this ExceptionlessConfiguration config, LogLevel minLogLevel = LogLevel.Info) {
            var logger = new InMemoryExceptionlessLog { MinimumLogLevel = minLogLevel };
            config.Resolver.Register<IExceptionlessLog>(logger);

            return logger;
        }

        public static void UseLogger(this ExceptionlessConfiguration config, IExceptionlessLog logger) {
            config.Resolver.Register<IExceptionlessLog>(new SafeExceptionlessLog(logger));
        }

        public static void ApplySavedServerSettings(this ExceptionlessConfiguration config) {
            SettingsManager.ApplySavedServerSettings(config);
        }

        public static void UseInMemoryStorage(this ExceptionlessConfiguration config) {
            config.Resolver.Register<IObjectStorage, InMemoryObjectStorage>();
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

            if (assemblies == null)
                assemblies = new List<Assembly> { Assembly.GetCallingAssembly() };

            assemblies = assemblies.Where(a => a != null).Distinct().ToList();

            try {
                foreach (var assembly in assemblies) {
                    object[] attributes = assembly.GetCustomAttributes(typeof(ExceptionlessAttribute), false);
                    if (attributes.Length <= 0 || !(attributes[0] is ExceptionlessAttribute))
                        continue;

                    var attr = attributes[0] as ExceptionlessAttribute;

                    config.Enabled = attr.Enabled;

                    if (!String.IsNullOrEmpty(attr.ApiKey) && attr.ApiKey != "API_KEY_HERE")
                        config.ApiKey = attr.ApiKey;
                    if (!String.IsNullOrEmpty(attr.ServerUrl))
                        config.ServerUrl = attr.ServerUrl;

                    break;
                }

                foreach (var assembly in assemblies) {
                    object[] attributes = assembly.GetCustomAttributes(typeof(ExceptionlessSettingAttribute), false);
                    foreach (ExceptionlessSettingAttribute attribute in attributes.OfType<ExceptionlessSettingAttribute>()) {
                        if (!String.IsNullOrEmpty(attribute.Name))
                            config.Settings[attribute.Name] = attribute.Value;
                    }
                }
            } catch (Exception ex) {
                var log = config.Resolver.GetLog();
                log.Error(ex, "Error while reading attribute configuration. Please contact support for more information.");
            }
        }
    }
}

namespace Exceptionless.Extensions {
    public static class ExceptionlessConfigurationExtensions {
        public static Uri GetServiceEndPoint(this ExceptionlessConfiguration config) {
            var builder = new UriBuilder(config.ServerUrl);
            builder.Path += builder.Path.EndsWith("/") ? "api/v2" : "/api/v2";

            // EnableSSL
            if (builder.Scheme == "https" && builder.Port == 80 && !builder.Host.Contains("local"))
                builder.Port = 443;

            return builder.Uri;
        }
        
        public static Uri GetHeartbeatServiceEndPoint(this ExceptionlessConfiguration config) {
            var builder = new UriBuilder(config.HeartbeatServerUrl);
            builder.Path += builder.Path.EndsWith("/") ? "api/v2" : "/api/v2";

            // EnableSSL
            if (builder.Scheme == "https" && builder.Port == 80 && !builder.Host.Contains("local"))
                builder.Port = 443;

            return builder.Uri;
        }
    }
}