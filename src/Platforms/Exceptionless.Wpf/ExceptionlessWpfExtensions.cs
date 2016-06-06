using System;
using System.Windows;
using System.Windows.Threading;
using Exceptionless.Dependency;
using Exceptionless.Dialogs;
using Exceptionless.Logging;
using Exceptionless.Plugins.Default;
using Exceptionless.Wpf.Extensions;

namespace Exceptionless {
    public static class ExceptionlessWpfExtensions {
        private static EventHandler _onProcessExit;

        /// <summary>
        /// Reads configuration settings, configures various plugins and wires up to platform specific exception handlers. 
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        /// <param name="showDialog">Controls whether a dialog is shown when an unhandled exception occurs.</param>
        public static void Register(this ExceptionlessClient client, bool showDialog = true) {
            client.Configuration.AddPlugin<SetEnvironmentUserPlugin>();
            client.Startup();

            if (client.Configuration.SessionsEnabled)
                client.SubmitSessionStart();

            client.RegisterApplicationThreadExceptionHandler();
            client.RegisterApplicationDispatcherUnhandledExceptionHandler();

            // make sure that queued events are sent when the app exits
            client.RegisterOnProcessExitHandler();

            if (!showDialog)
                return;

            client.SubmittingEvent -= OnSubmittingEvent;
            client.SubmittingEvent += OnSubmittingEvent;
        }

        /// <summary>
        /// Unregisters platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        public static void Unregister(this ExceptionlessClient client) {
            client.Shutdown();
            client.UnregisterApplicationThreadExceptionHandler();
            client.UnregisterApplicationDispatcherUnhandledExceptionHandler();
            client.UnregisterOnProcessExitHandler();

            client.SubmittingEvent -= OnSubmittingEvent;

            client.ProcessQueue();
            client.SubmitSessionEnd();
        }

        private static void OnSubmittingEvent(object sender, EventSubmittingEventArgs e) {
            //error.ExceptionlessClientInfo.Platform = ".NET WPF";
            if (!e.IsUnhandledError)
                return;

            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
                e.Cancel = !(bool)Application.Current.Dispatcher.Invoke(new Func<EventSubmittingEventArgs, bool>(ShowDialog), DispatcherPriority.Send, e);
            else
                e.Cancel = !ShowDialog(e);
        }

        private static bool ShowDialog(EventSubmittingEventArgs e) {
            var dialog = new CrashReportDialog(e.Client, e.Event);
            bool? result = dialog.ShowDialog();
            return result.HasValue && result.Value;
        }

        private static void RegisterOnProcessExitHandler(this ExceptionlessClient client) {
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
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessWpfExtensions), ex, "An error occurred while wiring up to the process exit event.");
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