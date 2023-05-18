using System;
using System.Threading;
using System.Windows.Forms;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Logging;

namespace Exceptionless.Windows.Extensions {
    public static class ExceptionlessClientExtensions {
        private static ThreadExceptionEventHandler _onApplicationThreadException;

        public static void RegisterApplicationThreadExceptionHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onApplicationThreadException == null) {
                _onApplicationThreadException = (sender, args) => {
                    var log = client.Configuration.Resolver.GetLog();
                    try {
                        log.Info(typeof(ExceptionlessClient), "Application.ThreadException called");
                        var contextData = new ContextData();
                        contextData.MarkAsUnhandledError();
                        contextData.SetSubmissionMethod("ApplicationThreadException");

                        args.Exception.ToExceptionless(contextData, client).Submit();

                        // process queue immediately since the app is about to exit.
                        client.ProcessQueueAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                        log.Info(typeof(ExceptionlessClient), "Application.ThreadException finished");
                    } catch (Exception ex) {
                        log.Error(typeof(ExceptionlessClientExtensions), ex, String.Concat("An error occurred while processing Application Thread Exception: ", ex.Message));
                    } finally {
                        log.Flush();
                    }
                };
            }

            try {
                Application.ThreadException -= _onApplicationThreadException;
                Application.ThreadException += _onApplicationThreadException;
            } catch (Exception ex) {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessClientExtensions), ex, "An error occurred while wiring up to the unobserved task exception event.");
            }
        }

        public static void UnregisterApplicationThreadExceptionHandler(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onApplicationThreadException == null)
                return;

            Application.ThreadException -= _onApplicationThreadException;
            _onApplicationThreadException = null;
        }
    }
}