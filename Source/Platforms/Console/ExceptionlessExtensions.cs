using System;
using Exceptionless.Dependency;
using Exceptionless.Logging;

namespace Exceptionless {
    public static class ExceptionlessExtensions {
        private static EventHandler _onProcessExit;

        /// <summary>
        /// Reads configuration settings, configures various plugins and wires up to platform specific exception handlers. 
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        public static void Register(this ExceptionlessClient client) {
            client.Startup();
            if (client.Configuration.SessionsEnabled)
                client.SubmitSessionStart();

            // make sure that queued events are sent when the app exits
            client.RegisterOnProcessExitHandler();
        }

        /// <summary>
        /// Unregisters platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        public static void Unregister(this ExceptionlessClient client) {
            client.Shutdown();
            client.UnregisterOnProcessExitHandler();
            if (client.Configuration.SessionsEnabled)
                client.SubmitSessionEnd();
            client.ProcessQueue();
        }

        private static void RegisterOnProcessExitHandler(this ExceptionlessClient client) {
            if (_onProcessExit == null) {
                _onProcessExit = (sender, args) => {
                    client.SubmitSessionEnd();
                    client.ProcessQueue();
                };
            }

            try {
                AppDomain.CurrentDomain.ProcessExit -= _onProcessExit;
                AppDomain.CurrentDomain.ProcessExit += _onProcessExit;
            } catch (Exception ex) {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessExtensions), ex, "An error occurred while wiring up to the process exit event.");
            }
        }

        private static void UnregisterOnProcessExitHandler(this ExceptionlessClient client) {
            if (_onProcessExit == null)
                return;

            AppDomain.CurrentDomain.ProcessExit -= _onProcessExit;
            _onProcessExit = null;
        }
    }
}