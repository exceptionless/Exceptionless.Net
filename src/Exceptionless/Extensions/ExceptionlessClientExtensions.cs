using System;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Plugins;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless {
    public static class ExceptionlessClientExtensions {
        /// <summary>
        /// Reads configuration settings, configures various plugins and wires up to platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        /// <param name="apiKey">The API key that will be used when sending events to the server.</param>
        public static void Startup(this ExceptionlessClient client, string apiKey = null) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (!String.IsNullOrEmpty(apiKey))
                client.Configuration.ApiKey = apiKey;

            client.Configuration.ReadAllConfig();
            client.Configuration.UseTraceLogEntriesPlugin();

            if (client.Configuration.UpdateSettingsWhenIdleInterval == null)
                client.Configuration.UpdateSettingsWhenIdleInterval = TimeSpan.FromMinutes(2);

            client.RegisterAppDomainUnhandledExceptionHandler();

            // make sure that queued events are sent when the app exits
            client.RegisterOnProcessExitHandler();
            client.RegisterTaskSchedulerUnobservedTaskExceptionHandler();

            if (client.Configuration.SessionsEnabled)
                client.SubmitSessionStart();
        }

        /// <summary>
        /// Unregisters platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        public static void Shutdown(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.UnregisterAppDomainUnhandledExceptionHandler();
            client.UnregisterOnProcessExitHandler();
            client.UnregisterTaskSchedulerUnobservedTaskExceptionHandler();

            client.ProcessQueue();
            if (client.Configuration.SessionsEnabled)
                client.SubmitSessionEnd();
        }

#region Submission Extensions

        /// <summary>
        /// Submits an unhandled exception event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="exception">The unhandled exception.</param>
        public static void SubmitUnhandledException(this ExceptionlessClient client, Exception exception) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var builder = exception.ToExceptionless(client: client);
            builder.PluginContextData.MarkAsUnhandledError();
            builder.Submit();
        }

        /// <summary>
        /// Submits an exception event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="exception">The exception.</param>
        public static void SubmitException(this ExceptionlessClient client, Exception exception) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.CreateException(exception).Submit();
        }

        /// <summary>
        /// Creates an exception event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="exception">The exception.</param>
        public static EventBuilder CreateException(this ExceptionlessClient client, Exception exception) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return exception.ToExceptionless(client: client);
        }

        /// <summary>
        /// Submits a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="message">The log message.</param>
        public static void SubmitLog(this ExceptionlessClient client, string message) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.CreateLog(message).Submit();
        }

        /// <summary>
        /// Submits a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="source">The log source.</param>
        /// <param name="message">The log message.</param>
        public static void SubmitLog(this ExceptionlessClient client, string source, string message) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.CreateLog(source, message).Submit();
        }

        /// <summary>
        /// Submits a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="source">The log source.</param>
        /// <param name="message">The log message.</param>
        /// <param name="level">The log level.</param>
        public static void SubmitLog(this ExceptionlessClient client, string source, string message, string level) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.CreateLog(source, message, level).Submit();
        }

        /// <summary>
        /// Submits a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="source">The log source.</param>
        /// <param name="message">The log message.</param>
        /// <param name="level">The log level.</param>
        public static void SubmitLog(this ExceptionlessClient client, string source, string message, LogLevel level) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.CreateLog(source, message, level.ToString()).Submit();
        }

        /// <summary>
        /// Submits a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="message">The log message.</param>
        /// <param name="level">The log level.</param>
        public static void SubmitLog(this ExceptionlessClient client, string message, LogLevel level) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.CreateLog(null, message, level.ToString()).Submit();
        }

        /// <summary>
        /// Creates a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="message">The log message.</param>
        public static EventBuilder CreateLog(this ExceptionlessClient client, string message) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.CreateEvent().SetType(Event.KnownTypes.Log).SetMessage(message);
        }

        /// <summary>
        /// Creates a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="source">The log source.</param>
        /// <param name="message">The log message.</param>
        public static EventBuilder CreateLog(this ExceptionlessClient client, string source, string message) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.CreateLog(message).SetSource(source);
        }

        /// <summary>
        /// Creates a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="source">The log source.</param>
        /// <param name="message">The log message.</param>
        /// <param name="level">The log level.</param>
        public static EventBuilder CreateLog(this ExceptionlessClient client, string source, string message, string level) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var builder = client.CreateLog(source, message);

            if (!String.IsNullOrWhiteSpace(level))
                builder.SetProperty(Event.KnownDataKeys.Level, level.Trim());

            return builder;
        }

        /// <summary>
        /// Creates a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="source">The log source.</param>
        /// <param name="message">The log message.</param>
        /// <param name="level">The log level.</param>
        public static EventBuilder CreateLog(this ExceptionlessClient client, string source, string message, LogLevel level) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return CreateLog(client, source, message, level.ToString());
        }

        /// <summary>
        /// Creates a log message event.
        /// </summary>
        /// <param name="client">The client instance.</param>S
        /// <param name="message">The log message.</param>
        /// <param name="level">The log level.</param>
        public static EventBuilder CreateLog(this ExceptionlessClient client, string message, LogLevel level) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return CreateLog(client, null, message, level.ToString());
        }

        /// <summary>
        /// Creates a feature usage event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="feature">The name of the feature that was used.</param>
        public static EventBuilder CreateFeatureUsage(this ExceptionlessClient client, string feature) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.CreateEvent().SetType(Event.KnownTypes.FeatureUsage).SetSource(feature);
        }

        /// <summary>
        /// Submits a feature usage event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="feature">The name of the feature that was used.</param>
        public static void SubmitFeatureUsage(this ExceptionlessClient client, string feature) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.CreateFeatureUsage(feature).Submit();
        }

        /// <summary>
        /// Creates a resource not found event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="resource">The name of the resource that was not found.</param>
        public static EventBuilder CreateNotFound(this ExceptionlessClient client, string resource) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.CreateEvent().SetType(Event.KnownTypes.NotFound).SetSource(resource);
        }

        /// <summary>
        /// Submits a resource not found event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="resource">The name of the resource that was not found.</param>
        public static void SubmitNotFound(this ExceptionlessClient client, string resource) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.CreateNotFound(resource).Submit();
        }

        /// <summary>
        /// Creates a session start event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        public static EventBuilder CreateSessionStart(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return client.CreateEvent().SetType(Event.KnownTypes.Session);
        }

        /// <summary>
        /// Submits a session start event.
        /// </summary>
        /// <param name="client">The client instance.</param>
        public static void SubmitSessionStart(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.CreateSessionStart().Submit();
        }

        /// <summary>
        /// Submits session end.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="sessionIdOrUserId">The session id or user id.</param>
        public static void SubmitSessionEnd(this ExceptionlessClient client, string sessionIdOrUserId = null) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            sessionIdOrUserId = sessionIdOrUserId ?? client.Configuration.CurrentSessionIdentifier;
            if (String.IsNullOrWhiteSpace(sessionIdOrUserId))
                return;

            var submissionClient = client.Configuration.Resolver.GetSubmissionClient();
            submissionClient.SendHeartbeat(sessionIdOrUserId, true, client.Configuration);
        }

        /// <summary>
        /// Submits session heartbeat.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <param name="sessionIdOrUserId">The session id or user id.</param>
        public static void SubmitSessionHeartbeat(this ExceptionlessClient client, string sessionIdOrUserId = null) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            sessionIdOrUserId = sessionIdOrUserId ?? client.Configuration.CurrentSessionIdentifier;
            if (String.IsNullOrWhiteSpace(sessionIdOrUserId))
                return;

            var submissionClient = client.Configuration.Resolver.GetSubmissionClient();
            submissionClient.SendHeartbeat(sessionIdOrUserId, false, client.Configuration);
        }

#endregion
    }
}

namespace Exceptionless.Extensions {
    public static class ExceptionlessClientExtensions {
        private static UnhandledExceptionEventHandler _onAppDomainUnhandledException;
        public static void RegisterAppDomainUnhandledExceptionHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onAppDomainUnhandledException == null) {
                _onAppDomainUnhandledException = (sender, args) => {
                    var exception = args.ExceptionObject as Exception;
                    if (exception == null)
                        return;

                    var contextData = new ContextData();
                    contextData.MarkAsUnhandledError();
                    contextData.SetSubmissionMethod("AppDomainUnhandledException");

                    exception.ToExceptionless(contextData, client).Submit();

                    // process queue immediately since the app is about to exit.
                    client.ProcessQueue();

                    if (client.Configuration.SessionsEnabled)
                        client.SubmitSessionEnd();
                };
            }

            try {
                AppDomain.CurrentDomain.UnhandledException -= _onAppDomainUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += _onAppDomainUnhandledException;
            } catch (Exception ex) {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessClientExtensions), ex, "An error occurred while wiring up to the unhandled exception event. This will happen when you are not running under full trust.");
            }
        }

        public static void UnregisterAppDomainUnhandledExceptionHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onAppDomainUnhandledException == null)
                return;

            AppDomain.CurrentDomain.UnhandledException -= _onAppDomainUnhandledException;
            _onAppDomainUnhandledException = null;
        }

        private static EventHandler _onProcessExit;
        public static void RegisterOnProcessExitHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onProcessExit == null) {
                _onProcessExit = (sender, args) => {
                    client.ProcessQueue();

                    if (client.Configuration.SessionsEnabled)
                        client.SubmitSessionEnd();
                };
            }

            try {
                AppDomain.CurrentDomain.ProcessExit -= _onProcessExit;
                AppDomain.CurrentDomain.ProcessExit += _onProcessExit;
            } catch (Exception ex) {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessClientExtensions), ex, "An error occurred while wiring up to the process exit event.");
            }
        }

        public static void UnregisterOnProcessExitHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onProcessExit == null)
                return;

            AppDomain.CurrentDomain.ProcessExit -= _onProcessExit;
            _onProcessExit = null;
        }

        private static EventHandler<UnobservedTaskExceptionEventArgs> _onTaskSchedulerOnUnobservedTaskException;
        public static void RegisterTaskSchedulerUnobservedTaskExceptionHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onTaskSchedulerOnUnobservedTaskException == null) {
                _onTaskSchedulerOnUnobservedTaskException = (sender, args) => {
                    var contextData = new ContextData();
                    contextData.MarkAsUnhandledError();
                    contextData.SetSubmissionMethod("UnobservedTaskException");

                    args.Exception.ToExceptionless(contextData, client).Submit();
                };
            }

            try {
                TaskScheduler.UnobservedTaskException -= _onTaskSchedulerOnUnobservedTaskException;
                TaskScheduler.UnobservedTaskException += _onTaskSchedulerOnUnobservedTaskException;
            } catch (Exception ex) {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessClientExtensions), ex, "An error occurred while wiring up to the unobserved task exception event.");
            }
        }

        public static void UnregisterTaskSchedulerUnobservedTaskExceptionHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onTaskSchedulerOnUnobservedTaskException == null)
                return;

            TaskScheduler.UnobservedTaskException -= _onTaskSchedulerOnUnobservedTaskException;
            _onTaskSchedulerOnUnobservedTaskException = null;
        }
    }
}