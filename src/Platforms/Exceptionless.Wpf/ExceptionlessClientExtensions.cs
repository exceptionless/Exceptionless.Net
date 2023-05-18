using System;
using System.Windows.Threading;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Logging;

namespace Exceptionless.Wpf.Extensions {
    public static class ExceptionlessClientExtensions {
        private static DispatcherUnhandledExceptionEventHandler _onApplicationDispatcherUnhandledException;

        public static void RegisterApplicationDispatcherUnhandledExceptionHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (System.Windows.Application.Current == null)
                return;

            if (_onApplicationDispatcherUnhandledException == null) {
                _onApplicationDispatcherUnhandledException = (sender, args) => {
                    var log = client.Configuration.Resolver.GetLog();

                    try {
                        log.Info(typeof(ExceptionlessClient), "Application.Current.DispatcherUnhandledException called");

                        var contextData = new ContextData();
                        contextData.MarkAsUnhandledError();
                        contextData.SetSubmissionMethod("DispatcherUnhandledException");

                        args.Exception.ToExceptionless(contextData, client).Submit();

                        // process queue immediately since the app is about to exit.
                        client.ProcessQueueAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                        log.Info(typeof(ExceptionlessClient), "Application.Current.DispatcherUnhandledException finished");
                    } catch (Exception ex) {
                        log.Error(typeof(ExceptionlessClientExtensions), ex, String.Concat("An error occurred while processing application dispatcher exception: ", ex.Message));
                    } finally {
                        log.Flush();
                    }
                };
            }

            try {
                System.Windows.Application.Current.DispatcherUnhandledException -= _onApplicationDispatcherUnhandledException;
                System.Windows.Application.Current.DispatcherUnhandledException += _onApplicationDispatcherUnhandledException;
            } catch (Exception ex) {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessClientExtensions), ex, "An error occurred while wiring up to the application dispatcher exception event.");
            }
        }

        public static void UnregisterApplicationDispatcherUnhandledExceptionHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onApplicationDispatcherUnhandledException == null)
                return;

            if (System.Windows.Application.Current != null)
                System.Windows.Application.Current.DispatcherUnhandledException -= _onApplicationDispatcherUnhandledException;

            _onApplicationDispatcherUnhandledException = null;
        }
    }
}