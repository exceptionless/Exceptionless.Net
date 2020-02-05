using System;
using System.Windows.Forms;
using Exceptionless.Dependency;
using Exceptionless.Dialogs;
using Exceptionless.Logging;
using Exceptionless.Plugins.Default;
using Exceptionless.Windows.Extensions;

namespace Exceptionless {
    public static class ExceptionlessWindowsExtensions {
        /// <summary>
        /// Reads configuration settings, configures various plugins and wires up to platform specific exception handlers. 
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        /// <param name="showDialog">Controls whether a dialog is shown when an unhandled exception occurs.</param>
        public static void Register(this ExceptionlessClient client, bool showDialog = true) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.Configuration.AddPlugin<SetEnvironmentUserPlugin>();
            client.Startup();

            client.RegisterApplicationThreadExceptionHandler();

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
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.Shutdown();
            client.UnregisterApplicationThreadExceptionHandler();
            
            client.SubmittingEvent -= OnSubmittingEvent;
        }

        private static void OnSubmittingEvent(object sender, EventSubmittingEventArgs e) {
            // ev.ExceptionlessClientInfo.Platform = ".NET Windows";

            if (!e.IsUnhandledError)
                return;

            var dialog = new CrashReportForm(e.Client, e.Event);
            e.Cancel = dialog.ShowDialog() != DialogResult.OK;
        }
    }
}