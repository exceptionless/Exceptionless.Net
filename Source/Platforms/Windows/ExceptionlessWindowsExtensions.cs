using System;
using System.Windows.Forms;
using Exceptionless.Dependency;
using Exceptionless.Dialogs;
using Exceptionless.Logging;
using Exceptionless.Windows.Extensions;

namespace Exceptionless {
    public static class ExceptionlessWindowsExtensions {
        private static EventHandler _onProcessExit;
        public static void Register(this ExceptionlessClient client, bool showDialog = true) {
            client.Startup();
            client.RegisterApplicationThreadExceptionHandler();

            // make sure that queued events are sent when the app exits
            client.RegisterOnProcessExitHandler();

            if (!showDialog)
                return;

            client.SubmittingEvent -= OnSubmittingEvent;
            client.SubmittingEvent += OnSubmittingEvent;
        }

        public static void Unregister(this ExceptionlessClient client) {
            client.Shutdown();
            client.UnregisterApplicationThreadExceptionHandler();
            client.UnregisterOnProcessExitHandler();
            
            client.SubmittingEvent -= OnSubmittingEvent;
        }

        private static void OnSubmittingEvent(object sender, EventSubmittingEventArgs e) {
            // ev.ExceptionlessClientInfo.Platform = ".NET Windows";

            if (!e.IsUnhandledError)
                return;

            var dialog = new CrashReportForm(e.Client, e.Event);
            e.Cancel = dialog.ShowDialog() != DialogResult.OK;
        }

        private static void RegisterOnProcessExitHandler(this ExceptionlessClient client) {
            if (_onProcessExit == null)
                _onProcessExit = (sender, args) => client.ProcessQueue();

            try {
                AppDomain.CurrentDomain.ProcessExit -= _onProcessExit;
                AppDomain.CurrentDomain.ProcessExit += _onProcessExit;
            } catch (Exception ex) {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessWindowsExtensions), ex, "An error occurred while wiring up to the process exit event.");
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