using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Exceptionless.Dependency;
using Exceptionless.Dialogs;
using Exceptionless.Plugins.Default;
using Exceptionless.Services;
using Exceptionless.Wpf.Extensions;

namespace Exceptionless {
    public static class ExceptionlessWpfExtensions {
        /// <summary>
        /// Reads configuration settings, configures various plugins and wires up to platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        /// <param name="showDialog">Controls whether a dialog is shown when an unhandled exception occurs.</param>
        public static void Register(this ExceptionlessClient client, bool showDialog = true) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.Configuration.AddPlugin<SetEnvironmentUserPlugin>();
            client.Configuration.Resolver.Register<IEnvironmentInfoCollector, ExceptionlessWindowsEnvironmentInfoCollector>();
            client.Startup();

            client.RegisterApplicationDispatcherUnhandledExceptionHandler();

            if (!showDialog)
                return;

            client.SubmittingEvent -= OnSubmittingEvent;
            client.SubmittingEvent += OnSubmittingEvent;
        }

        /// <summary>
        /// Unregisters platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        public static async Task UnregisterAsync(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            await client.ShutdownAsync().ConfigureAwait(false);
            client.UnregisterApplicationDispatcherUnhandledExceptionHandler();
            client.SubmittingEvent -= OnSubmittingEvent;
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
    }
}