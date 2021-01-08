using System;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Configuration;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Queue;
using Exceptionless.Submission;

namespace Exceptionless {
    public class ExceptionlessClient : IDisposable {
        private readonly Timer _updateSettingsTimer;
        private readonly Lazy<IExceptionlessLog> _log;
        private readonly Lazy<IEventQueue> _queue;
        private readonly Lazy<ISubmissionClient> _submissionClient;
        private readonly Lazy<ILastReferenceIdManager> _lastReferenceIdManager;

        public ExceptionlessClient() : this(new ExceptionlessConfiguration(DependencyResolver.CreateDefault())) { }

        public ExceptionlessClient(string apiKey) : this(new ExceptionlessConfiguration(DependencyResolver.CreateDefault())) {
            Configuration.ApiKey = apiKey;
        }

        public ExceptionlessClient(Action<ExceptionlessConfiguration> configure) : this(new ExceptionlessConfiguration(DependencyResolver.CreateDefault())) {
            if (configure != null)
                configure(Configuration);
        }

        public ExceptionlessClient(ExceptionlessConfiguration configuration) {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            Configuration = configuration;
            Configuration.Changed += OnConfigurationChanged;
            Configuration.Resolver.Register(typeof(ExceptionlessConfiguration), () => Configuration);

            _log = new Lazy<IExceptionlessLog>(() => Configuration.Resolver.GetLog());
            _queue = new Lazy<IEventQueue>(() => {
                // config can't be changed after the queue starts up.
                Configuration.LockConfig();

                var q = Configuration.Resolver.GetEventQueue();
                q.EventsPosted += OnQueueEventsPosted;
                return q;
            });

            _submissionClient = new Lazy<ISubmissionClient>(() => Configuration.Resolver.GetSubmissionClient());
            _lastReferenceIdManager = new Lazy<ILastReferenceIdManager>(() => Configuration.Resolver.GetLastReferenceIdManager());
            _updateSettingsTimer = new Timer(OnUpdateSettings, null, GetInitialSettingsDelay(), Configuration.UpdateSettingsWhenIdleInterval ?? TimeSpan.FromMilliseconds(-1));
        }

        private TimeSpan GetInitialSettingsDelay() {
            return Configuration.UpdateSettingsWhenIdleInterval != null && Configuration.UpdateSettingsWhenIdleInterval > TimeSpan.Zero ? TimeSpan.FromSeconds(5) : TimeSpan.FromMilliseconds(-1);
        }

        private void OnQueueEventsPosted(object sender, EventsPostedEventArgs args) {
            var interval = Configuration.UpdateSettingsWhenIdleInterval ?? TimeSpan.FromMilliseconds(-1);
            _updateSettingsTimer.Change(interval, interval);
        }

        private void OnConfigurationChanged(object sender, EventArgs e) {
            var interval = Configuration.UpdateSettingsWhenIdleInterval ?? TimeSpan.FromMilliseconds(-1);
            _updateSettingsTimer.Change(!_queue.IsValueCreated ? GetInitialSettingsDelay() : interval, interval);
        }

        private bool _isUpdatingSettings;
        private void OnUpdateSettings(object state) {
            if (_isUpdatingSettings || !Configuration.IsValid)
                return;

            _isUpdatingSettings = true;
            SettingsManager.UpdateSettings(Configuration);
            _isUpdatingSettings = false;
        }

        public ExceptionlessConfiguration Configuration { get; private set; }

        /// <summary>
        /// Updates the user's email address and description of an event for the specified reference id.
        /// </summary>
        /// <param name="referenceId">The reference id of the event to update.</param>
        /// <param name="email">The user's email address to set on the event.</param>
        /// <param name="description">The user's description of the event.</param>
        public bool UpdateUserEmailAndDescription(string referenceId, string email, string description) {
            if (String.IsNullOrEmpty(referenceId))
                throw new ArgumentNullException("referenceId");

            if (String.IsNullOrEmpty(email) && String.IsNullOrEmpty(description))
                return true;

            if (!Configuration.Enabled) {
                _log.Value.Info(typeof(ExceptionlessClient), "Configuration is disabled. The event will not be updated with the user email and description.");
                return false;
            }

            if (!Configuration.IsValid) {
                _log.Value.FormattedInfo(typeof(ExceptionlessClient), "Configuration is invalid: {0}. The event will not be updated with the user email and description.", String.Join(", ", Configuration.Validate().Messages));
                return false;
            }

            if (!Configuration.IsLocked) {
                Configuration.LockConfig();
                if (!Configuration.IsValid) {
                    _log.Value.FormattedError(typeof(ExceptionlessClient), "Disabling client due to invalid configuration: {0}", String.Join(", ", Configuration.Validate().Messages));
                    return false;
                }
            }

            try {
                var response = _submissionClient.Value.PostUserDescription(referenceId, new UserDescription(email, description), Configuration, Configuration.Resolver.GetJsonSerializer());
                if (!response.Success)
                    _log.Value.FormattedError(typeof(ExceptionlessClient), response.Exception, "Failed to submit user email and description for event '{0}': {1} {2}", referenceId, response.StatusCode, response.Message);

                return response.Success;
            } catch (Exception ex) {
                _log.Value.FormattedError(typeof(ExceptionlessClient), ex, "An error occurred while updating the user email and description for event: {0}.", referenceId);
                return false;
            }
        }

        /// <summary>
        /// Start processing the queue asynchronously.
        /// </summary>
        public Task ProcessQueueAsync() {
            if (!Configuration.Enabled) {
                _log.Value.Info(typeof(ExceptionlessClient), "Configuration is disabled. The queue will not be processed.");
                return Task.FromResult(0);
            }

            if (!Configuration.IsValid) {
                _log.Value.FormattedInfo(typeof(ExceptionlessClient), "Configuration is invalid: {0}. The queue will not be processed.", String.Join(", ", Configuration.Validate().Messages));
                return Task.FromResult(0);
            }

            if (!Configuration.IsLocked) {
                Configuration.LockConfig();
                if (!Configuration.IsValid) {
                    _log.Value.FormattedError(typeof(ExceptionlessClient), "Disabling client due to invalid configuration: {0}", String.Join(", ", Configuration.Validate().Messages));
                    return Task.FromResult(0);
                }
            }

            return _queue.Value.ProcessAsync();
        }

        /// <summary>
        /// Process the queue.
        /// </summary>
        public void ProcessQueue() {
            ProcessQueueAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets a disposable object that when disposed will trigger the client queue to be processed.
        /// <code>using var _ = client.ProcessQueueDeferred();</code>
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> that when disposed will trigger the client queue to be processed.</returns>
        public IDisposable ProcessQueueDeferred() {
            return new ProcessQueueScope(this);
        }

        /// <summary>
        /// Submits the event to be sent to the server.
        /// </summary>
        /// <param name="ev">The event data.</param>
        /// <param name="pluginContextData">
        /// Any contextual data objects to be used by Exceptionless plugins to gather default
        /// information for inclusion in the report information.
        /// </param>
        public void SubmitEvent(Event ev, ContextData pluginContextData = null) {
            if (ev == null)
                throw new ArgumentNullException("ev");

            if (!Configuration.Enabled) {
                _log.Value.Info(typeof(ExceptionlessClient), "Configuration is disabled. The event will not be submitted.");
                return;
            }

            if (!Configuration.IsValid) {
                _log.Value.FormattedInfo(typeof(ExceptionlessClient), "Configuration is invalid: {0}. The event will not be submitted.", String.Join(", ", Configuration.Validate().Messages));
                return;
            }

            if (!Configuration.IsLocked) {
                Configuration.LockConfig();
                if (!Configuration.IsValid) {
                    _log.Value.FormattedError(typeof(ExceptionlessClient), "Disabling client due to invalid configuration: {0}", String.Join(", ", Configuration.Validate().Messages));
                    return;
                }
            }

            var context = new EventPluginContext(this, ev, pluginContextData);
            EventPluginManager.Run(context);
            if (context.Cancel) {
                _log.Value.FormattedInfo(typeof(ExceptionlessClient), "Event submission cancelled by event pipeline: refid={0} type={1} message={2}", ev.ReferenceId, ev.Type, ev.Message);
                return;
            }

            // ensure all required data
            if (String.IsNullOrEmpty(ev.Type))
                ev.Type = Event.KnownTypes.Log;
            if (ev.Date == DateTimeOffset.MinValue)
                ev.Date = DateTimeOffset.Now;

            if (!OnSubmittingEvent(ev, pluginContextData)) {
                _log.Value.FormattedInfo(typeof(ExceptionlessClient), "Event submission cancelled by event handler: refid={0} type={1} message={2}", ev.ReferenceId, ev.Type, ev.Message);
                return;
            }

            _log.Value.FormattedTrace(typeof(ExceptionlessClient), "Submitting event: type={0}{1}", ev.Type, !String.IsNullOrEmpty(ev.ReferenceId) ? " refid=" + ev.ReferenceId : String.Empty);
            _queue.Value.Enqueue(ev);

            if (!String.IsNullOrEmpty(ev.ReferenceId)) {
                _log.Value.FormattedTrace(typeof(ExceptionlessClient), "Setting last reference id: {0}", ev.ReferenceId);
                _lastReferenceIdManager.Value.SetLast(ev.ReferenceId);
            }

            OnSubmittedEvent(new EventSubmittedEventArgs(this, ev, pluginContextData));
        }

        /// <summary>Creates a new instance of <see cref="Event" />.</summary>
        /// <param name="pluginContextData">
        /// Any contextual data objects to be used by Exceptionless plugins to gather default
        /// information to add to the event data.
        /// </param>
        /// <returns>A new instance of <see cref="EventBuilder" />.</returns>
        public EventBuilder CreateEvent(ContextData pluginContextData = null) {
            return new EventBuilder(new Event { Date = DateTimeOffset.Now }, this, pluginContextData);
        }

        /// <summary>
        /// Gets the last event client id that was submitted to the server.
        /// </summary>
        /// <returns>The event client id.</returns>
        public string GetLastReferenceId() {
            return _lastReferenceIdManager.Value.GetLast();
        }

        /// <summary>
        /// Occurs when the event is being submitted.
        /// </summary>
        public event EventHandler<EventSubmittingEventArgs> SubmittingEvent;

        protected internal bool OnSubmittingEvent(Event ev, ContextData pluginContextData) {
            var args = new EventSubmittingEventArgs(this, ev, pluginContextData);
            OnSubmittingEvent(args);
            return !args.Cancel;
        }

        /// <summary>
        /// Raises the <see cref="SubmittingEvent" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventSubmittingEventArgs" /> instance containing the event data.</param>
        protected internal void OnSubmittingEvent(EventSubmittingEventArgs e) {
            if (SubmittingEvent == null)
                return;

            var handlers = SubmittingEvent.GetInvocationList();
            foreach (var handler in handlers) {
                try {
                    handler.DynamicInvoke(this, e);
                    if (e.Cancel)
                        return;
                } catch (Exception ex) {
                    _log.Value.FormattedError(typeof(ExceptionlessClient), ex, "Error while invoking SubmittingEvent handler: {0}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Occurs when the event has been submitted.
        /// </summary>
        public event EventHandler<EventSubmittedEventArgs> SubmittedEvent;

        /// <summary>
        /// Raises the <see cref="SubmittedEvent" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventSubmittedEventArgs" /> instance containing the event data.</param>
        protected internal void OnSubmittedEvent(EventSubmittedEventArgs e) {
            if (SubmittedEvent == null)
                return;

            var handlers = SubmittedEvent.GetInvocationList();
            foreach (var handler in handlers) {
                try {
                    handler.DynamicInvoke(this, e);
                } catch (Exception ex) {
                    _log.Value.FormattedError(typeof(ExceptionlessClient), ex, "Error while invoking SubmittedEvent handler: {0}", ex.Message);
                }
            }
        }

        void IDisposable.Dispose() {
            Configuration.Changed -= OnConfigurationChanged;
            if (_queue.IsValueCreated)
                _queue.Value.EventsPosted -= OnQueueEventsPosted;

            _updateSettingsTimer.Dispose();
            Configuration.Resolver.Dispose();
        }

        #region Default

        private static readonly Lazy<ExceptionlessClient> _defaultClient = new Lazy<ExceptionlessClient>(() => new ExceptionlessClient());

        public static ExceptionlessClient Default {
            get { return _defaultClient.Value; }
        }

        #endregion
    }
}
