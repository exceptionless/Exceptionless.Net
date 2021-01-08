using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless {
    public class ExceptionlessConfiguration {
        private const string DEFAULT_SERVER_URL = "https://collector.exceptionless.io";
        private const string DEFAULT_CONFIG_SERVER_URL = "https://config.exceptionless.io";
        private const string DEFAULT_HEARTBEAT_SERVER_URL = "https://heartbeat.exceptionless.io";
        private const string DEFAULT_USER_AGENT = "exceptionless/";
        private static readonly Lazy<string> _version = new Lazy<string>(() => {
            var assembly = Assembly.GetExecutingAssembly();
            var attribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            return attribute.Version;
        });
        private const int DEFAULT_SUBMISSION_BATCH_SIZE = 50;

        private readonly IDependencyResolver _resolver;
        private bool _configLocked;
        private string _apiKey;
        private string _configServerUrl;
        private string _heartbeatServerUrl;
        private string _serverUrl;
        private bool _includePrivateInformation;
        private int _submissionBatchSize;
        private ValidationResult _validationResult;
        private TimeSpan? _updateSettingsWhenIdleInterval;
        private readonly List<string> _exclusions = new List<string>();
        private readonly List<string> _userAgentBotPatterns = new List<string>();
        private readonly List<Func<Event, bool>> _eventExclusions = new List<Func<Event, bool>>();

        public ExceptionlessConfiguration(IDependencyResolver resolver) {
            if (resolver == null)
                throw new ArgumentNullException("resolver");

            ServerUrl = DEFAULT_SERVER_URL;
            ConfigServerUrl = DEFAULT_CONFIG_SERVER_URL;
            HeartbeatServerUrl = DEFAULT_HEARTBEAT_SERVER_URL;
            UserAgent = DEFAULT_USER_AGENT + _version.Value;
            SubmissionBatchSize = DEFAULT_SUBMISSION_BATCH_SIZE;
            Enabled = true;
            QueueMaxAge = TimeSpan.FromDays(7);
            QueueMaxAttempts = 3;
            DefaultTags = new TagSet();
            DefaultData = new DataDictionary();
            Settings = new SettingsDictionary();
            IncludePrivateInformation = true;

            _resolver = resolver;

            EventPluginManager.AddDefaultPlugins(this);
        }

        internal bool IsLocked {
            get { return _configLocked; }
        }

        internal void LockConfig() {
            if (_configLocked)
                return;

            _configLocked = true;
            Enabled = Enabled && IsValid;
        }

        /// <summary>
        /// The server url that all events will be sent to.
        /// </summary>
        public string ServerUrl {
            get { return _serverUrl; }
            set {
                if (_serverUrl == value)
                    return;

                if (_configLocked)
                    throw new ArgumentException("ServerUrl can't be changed after the client has been initialized.");

                _validationResult = null;
                _serverUrl = value;
                _configServerUrl = value;
                _heartbeatServerUrl = value;
                OnChanged();
            }
        }

        /// <summary>
        /// The server url that all configuration will be retrieved from.
        /// </summary>
        public string ConfigServerUrl {
            get { return _configServerUrl; }
            set {
                if (_configServerUrl == value)
                    return;

                if (_configLocked)
                    throw new ArgumentException("ConfigServerUrl can't be changed after the client has been initialized.");

                _validationResult = null;
                _configServerUrl = value;
                OnChanged();
            }
        }

        /// <summary>
        /// The server url that all events will be sent to.
        /// </summary>
        public string HeartbeatServerUrl {
            get { return _heartbeatServerUrl; }
            set {
                if (_heartbeatServerUrl == value)
                    return;

                if (_configLocked)
                    throw new ArgumentException("HeartbeatServerUrl can't be changed after the client has been initialized.");

                _validationResult = null;
                _heartbeatServerUrl = value;
                OnChanged();
            }
        }

        /// <summary>
        /// The API key that will be used when sending events to the server.
        /// </summary>
        public string ApiKey {
            get { return _apiKey; }
            set {
                if (_apiKey == value)
                    return;

                if (_configLocked)
                    throw new ArgumentException("ApiKey can't be changed after the client has been initialized.");

                _validationResult = null;
                _apiKey = value;
                OnChanged();
            }
        }

        /// <summary>
        /// Used to identify the client that sent the events to the server.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Ability to set a custom proxy. By default, .NET will use any system or configuration defined proxy settings.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// Whether the client is currently enabled or not. If it is disabled, submitted errors will be discarded and no data will be sent to the server.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Maximum time (provided in days) that the queue will persist events
        /// </summary>
        public TimeSpan QueueMaxAge { get; set; }

        /// <summary>
        /// Maximum number of times the client will try to upload an event in the queue
        /// </summary>
        public int QueueMaxAttempts { get; set; }

        /// <summary>
        /// A default list of tags that will automatically be added to every report submitted to the server.
        /// </summary>
        public TagSet DefaultTags { get; private set; }

        /// <summary>
        /// A default list of of extended data objects that will automatically be added to every report submitted to the server.
        /// </summary>
        public DataDictionary DefaultData { get; private set; }

        /// <summary>
        /// Contains a dictionary of custom settings that can be used to control the client and will be automatically updated from the server.
        /// </summary>
        public SettingsDictionary Settings { get; private set; }

        /// <summary>
        /// How often the client should check for updated server settings when idle. The default is every 2 minutes.
        /// </summary>
        public TimeSpan? UpdateSettingsWhenIdleInterval {
            get { return _updateSettingsWhenIdleInterval; }
            set {
                if (_updateSettingsWhenIdleInterval == value)
                    return;

                if (value.HasValue && value > TimeSpan.Zero && value < TimeSpan.FromMinutes(2))
                    _updateSettingsWhenIdleInterval = TimeSpan.FromMinutes(2);
                else if (value.HasValue && value <= TimeSpan.Zero)
                    _updateSettingsWhenIdleInterval = TimeSpan.FromMilliseconds(-1);
                else
                    _updateSettingsWhenIdleInterval = value;

                OnChanged();
            }
        }

        /// <summary>
        /// Set the default minimum log level that will be used until settings are loaded from the server.
        /// </summary>
        /// <param name="minLogLevel"></param>
        public void SetDefaultMinLogLevel(LogLevel minLogLevel) {
            if (!Settings.ContainsKey("@@log:*"))
                Settings.Add("@@log:*", minLogLevel.ToString());
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include private information about the local machine.
        /// </summary>
        /// <value>
        /// <c>true</c> to include private information about the local machine; otherwise, <c>false</c>.
        /// </value>
        public bool IncludePrivateInformation {
            get { return _includePrivateInformation; }
            set {
                _includePrivateInformation = value;
                IncludeUserName = value;
                IncludeMachineName = value;
                IncludeIpAddress = value;
                IncludeCookies = value;
                IncludePostData = value;
                IncludeQueryString = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include User Name.
        /// </summary>
        public bool IncludeUserName { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to include MachineName in MachineInfo.
        /// </summary>
        public bool IncludeMachineName { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to include Ip Addresses in MachineInfo and RequestInfo.
        /// </summary>
        public bool IncludeIpAddress { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to include Cookies.
        /// NOTE: DataExclusions are applied to all Cookie keys when enabled.
        /// </summary>
        public bool IncludeCookies { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to include Form/POST Data.
        /// NOTE: DataExclusions are only applied to Form data keys when enabled.
        /// </summary>
        public bool IncludePostData { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to include query string information.
        /// NOTE: DataExclusions are applied to all Query String keys when enabled.
        /// </summary>
        public bool IncludeQueryString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically send session start, session heartbeats and session end events.
        /// </summary>
        public bool SessionsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically process the pending event queue after each request is completed.
        /// NOTE: Only supported on AspNetCore currently.
        /// </summary>
        public bool ProcessQueueOnCompletedRequest { get; set; }
        
        internal string CurrentSessionIdentifier { get; set; }

        /// <summary>
        /// Maximum number of events that should be sent to the server together in a batch. (Defaults to 50)
        /// </summary>
        public int SubmissionBatchSize {
            get { return _submissionBatchSize; }
            set {
                if (value > 0)
                    _submissionBatchSize = value;
            }
        }

        /// <summary>
        /// Callback which is invoked to validate the exceptionless server certificate.
        /// </summary>
        public Func<CertificateData, bool> ServerCertificateValidationCallback { get; set; }

        /// <summary>
        /// A list of exclusion patterns that will automatically remove any data that matches them from any data submitted to the server.
        /// For example, entering CreditCard will remove any extended data properties, form fields, cookies and query
        /// parameters from the report.
        /// </summary>
        public IEnumerable<string> DataExclusions {
            get { return _exclusions.Union(Settings.GetStringCollection(SettingsDictionary.KnownKeys.DataExclusions)); }
        }

        /// <summary>
        /// Add items to the list of exclusion patterns that will automatically remove any data that matches them from any data submitted to the server.
        /// For example, entering CreditCard will remove any extended data properties, form fields, cookies and query
        /// parameters from the report.
        /// </summary>
        /// <param name="exclusions">The list of exclusion patterns to add.</param>
        public void AddDataExclusions(params string[] exclusions) {
            _exclusions.AddRange(exclusions);
        }

        /// <summary>
        /// Add items to the list of exclusion patterns that will automatically remove any data that matches them from any data submitted to the server.
        /// For example, entering CreditCard will remove any extended data properties, form fields, cookies and query
        /// parameters from the report.
        /// </summary>
        /// <param name="exclusions">The list of exclusion patterns to add.</param>
        public void AddDataExclusions(IEnumerable<string> exclusions) {
            _exclusions.AddRange(exclusions);
        }

        /// <summary>
        /// A list of user agent patterns that will cause any event with a matching user agent to not be submitted.
        /// For example, entering *Bot* will cause any events that contains a user agent of Bot will not be submitted.
        /// </summary>
        public IEnumerable<string> UserAgentBotPatterns {
            get { return _userAgentBotPatterns.Union(Settings.GetStringCollection(SettingsDictionary.KnownKeys.UserAgentBotPatterns)); }
        }

        /// <summary>
        /// Add items to the list of user agent patterns that will cause any event with a matching user agent to not be submitted.
        /// For example, entering *Bot* will cause any events that contains a user agent of Bot will not be submitted.
        /// </summary>
        /// <param name="userAgentBotPatterns">The list of user agent bot patterns to add.</param>
        public void AddUserAgentBotPatterns(params string[] userAgentBotPatterns) {
            _userAgentBotPatterns.AddRange(userAgentBotPatterns);
        }

        /// <summary>
        /// Add items to the list of user agent patterns that will cause any event with a matching user agent to not be submitted.
        /// For example, entering *Bot* will cause any events that contains a user agent of Bot will not be submitted.
        /// </summary>
        /// <param name="userAgentBotPatterns">The list of user agent bot patterns to add.</param>
        public void AddUserAgentBotPatterns(IEnumerable<string> userAgentBotPatterns) {
            _userAgentBotPatterns.AddRange(userAgentBotPatterns);
        }

        /// <summary>
        /// A list of event exclusion callbacks that will keep events from being submitted.
        /// </summary>
        public IEnumerable<Func<Event, bool>> EventExclusions {
            get { return _eventExclusions; }
        }

        /// <summary>
        /// Add an event exclusion callback. If the function returns false for a specific event, that event will not be submitted.
        /// </summary>
        /// <param name="eventExclusionCallback">The callback to call to check to see if an event should be excluded.</param>
        public void AddEventExclusion(Func<Event, bool> eventExclusionCallback) {
            if (eventExclusionCallback != null)
                _eventExclusions.Add(eventExclusionCallback);
        }

        /// <summary>
        /// The dependency resolver to use for this configuration.
        /// </summary>
        public IDependencyResolver Resolver { get { return _resolver; } }

        #region Plugins

        private readonly ConcurrentDictionary<string, PluginRegistration> _plugins = new ConcurrentDictionary<string, PluginRegistration>();

        /// <summary>
        /// The list of plugins that will be used in this configuration.
        /// </summary>
        public IEnumerable<PluginRegistration> Plugins {
            get { return _plugins.Values.ToList().OrderBy(e => e.Priority); }
        }

        /// <summary>
        /// Register an plugin to be used in this configuration.
        /// </summary>
        /// <typeparam name="T">The plugin type to be added.</typeparam>
        /// <param name="plugin">The plugin instance to be added.</param>
        public void AddPlugin<T>(T plugin) where T : IEventPlugin {
            string key = typeof(T).FullName;
            RemovePlugin(key);

            if (!_plugins.TryAdd(key, new PluginRegistration(key, GetPriority(typeof(T)), new Lazy<IEventPlugin>(() => plugin))))
                Resolver.GetLog().Error(String.Format("Unable to add plugin: {0}", key));
        }

        /// <summary>
        /// Register an plugin to be used in this configuration.
        /// </summary>
        /// <typeparam name="T">The plugin type to be added.</typeparam>
        public void AddPlugin<T>() where T : IEventPlugin {
            AddPlugin(typeof(T).FullName, typeof(T));
        }

        /// <summary>
        /// Register an plugin to be used in this configuration.
        /// </summary>
        /// <param name="key">The key used to identify the plugin.</param>
        /// <param name="pluginType">The plugin type to be added.</param>
        public void AddPlugin(string key, Type pluginType) {
            RemovePlugin(key);

            var plugin = new PluginRegistration(key, GetPriority(pluginType), new Lazy<IEventPlugin>(() => Resolver.Resolve(pluginType) as IEventPlugin));
            if (!_plugins.TryAdd(key, plugin))
                Resolver.GetLog().Error(String.Format("Unable to add plugin: {0}", key));
        }

        /// <summary>
        /// Register an plugin to be used in this configuration.
        /// </summary>
        /// <param name="key">The key used to identify the plugin.</param>
        /// <param name="factory">A factory method to create the plugin.</param>
        public void AddPlugin(string key, Func<ExceptionlessConfiguration, IEventPlugin> factory) {
            AddPlugin(key, 0, factory);
        }

        /// <summary>
        /// Register an plugin to be used in this configuration.
        /// </summary>
        /// <param name="key">The key used to identify the plugin.</param>
        /// <param name="priority">Used to determine plugins priority.</param>
        /// <param name="factory">A factory method to create the plugin.</param>
        public void AddPlugin(string key, int priority, Func<ExceptionlessConfiguration, IEventPlugin> factory) {
            RemovePlugin(key);

            var plugin = new PluginRegistration(key, priority, new Lazy<IEventPlugin>(() => factory(this)));
            if (!_plugins.TryAdd(key, plugin))
                Resolver.GetLog().Error(String.Format("Unable to add plugin: {0}", key));
        }

        /// <summary>
        /// Register an plugin to be used in this configuration.
        /// </summary>
        /// <param name="pluginAction">The plugin action to run.</param>
        public void AddPlugin(Action<EventPluginContext> pluginAction) {
            AddPlugin(Guid.NewGuid().ToString(), pluginAction);
        }

        /// <summary>
        /// Register an plugin to be used in this configuration.
        /// </summary>
        /// <param name="key">The key used to identify the plugin.</param>
        /// <param name="pluginAction">The plugin action to run.</param>
        public void AddPlugin(string key, Action<EventPluginContext> pluginAction) {
            AddPlugin(key, 0, pluginAction);
        }

        /// <summary>
        /// Register an plugin to be used in this configuration.
        /// </summary>
        /// <param name="key">The key used to identify the plugin.</param>
        /// <param name="priority">Used to determine plugins priority.</param>
        /// <param name="pluginAction">The plugin action to run.</param>
        public void AddPlugin(string key, int priority, Action<EventPluginContext> pluginAction) {
            RemovePlugin(key);

            var plugin = new PluginRegistration(key, priority, new Lazy<IEventPlugin>(() => new ActionPlugin(pluginAction)));
            if (!_plugins.TryAdd(key, plugin))
                Resolver.GetLog().Error(String.Format("Unable to add plugin: {0}", key));
        }

        /// <summary>
        /// Remove an plugin from this configuration.
        /// </summary>
        /// <typeparam name="T">The plugin type to be added.</typeparam>
        public void RemovePlugin<T>() where T : IEventPlugin {
            RemovePlugin(typeof(T).FullName);
        }

        /// <summary>
        /// Remove an plugin by key from this configuration.
        /// </summary>
        /// <param name="key">The key for the plugin to be removed.</param>
        public void RemovePlugin(string key) {
            PluginRegistration plugin;
            if (_plugins.TryRemove(key, out plugin))
                plugin.Dispose();
        }

        private int GetPriority(Type type) {
            if (type == null)
                return 0;

            try {
                var priorityAttribute = type.GetTypeInfo().GetCustomAttributes(typeof(PriorityAttribute), true).FirstOrDefault() as PriorityAttribute;
                return priorityAttribute != null ? priorityAttribute.Priority : 0;
            } catch (Exception ex) {
                Resolver.GetLog().Error(typeof(ExceptionlessConfiguration), ex, "An error occurred while getting the priority for type: " + type.FullName);
            }

            return 0;
        }

        #endregion

        public bool IsValid {
            get {
                if (_validationResult == null)
                    _validationResult = Validate();

                return _validationResult.IsValid;
            }
        }

        public ValidationResult Validate() {
            if (_validationResult != null)
                return _validationResult;

            var result = new ValidationResult();

            string key = ApiKey != null ? ApiKey.Trim() : null;
            if (String.IsNullOrEmpty(key) || String.Equals(key, "API_KEY_HERE", StringComparison.OrdinalIgnoreCase))
                result.Messages.Add("ApiKey is not set.");

            if (key != null && (key.Length < 10 || key.Contains(" ")))
                result.Messages.Add(String.Format("ApiKey \"{0}\" is not valid.", key));

            if (String.IsNullOrEmpty(ServerUrl))
                result.Messages.Add("ServerUrl is not set.");

            if (String.IsNullOrEmpty(ConfigServerUrl))
                result.Messages.Add("ConfigServerUrl is not set.");

            if (String.IsNullOrEmpty(HeartbeatServerUrl))
                result.Messages.Add("HeartbeatServerUrl is not set.");

            return result;
        }

        public class ValidationResult {
            public ValidationResult() {
                Messages = new List<string>();
            }

            public bool IsValid { get { return Messages.Count == 0; } }
            public ICollection<string> Messages { get; private set; }
        }

        [DebuggerDisplay("Key: {Key}, Priority: {Priority}")]
        public class PluginRegistration : IDisposable {
            private readonly Lazy<IEventPlugin> _plugin;
            public PluginRegistration(string key, int priority, Lazy<IEventPlugin> plugin) {
                Key = key;
                Priority = priority;
                _plugin = plugin;
            }

            public int Priority { get; private set; }

            public string Key { get; private set; }

            public IEventPlugin Plugin {
                get { return _plugin.Value; }
            }

            public override string ToString() {
                return String.Format("Key: {0}, Priority: {1}", Key, Priority);
            }

            public void Dispose() {
                if (!_plugin.IsValueCreated)
                    return;

                var disposable = _plugin.Value as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        public event EventHandler Changed;

        protected virtual void OnChanged() {
            try {
            if (Changed != null)
                Changed.Invoke(this, EventArgs.Empty);
            } catch (Exception ex) {
                Resolver.GetLog().Error(typeof(ExceptionlessConfiguration), ex, "Error while calling OnChanged event handlers.");
            }
        }
    }
}
