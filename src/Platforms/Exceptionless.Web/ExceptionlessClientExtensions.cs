using System;
using System.Web;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Logging;

namespace Exceptionless.Web.Extensions {
    public static class ExceptionlessClientExtensions {
        private static EventHandler _onHttpApplicationError;

        public static void RegisterHttpApplicationErrorHandler(this ExceptionlessClient client, HttpApplication app) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onHttpApplicationError == null)
                _onHttpApplicationError = (sender, args) => {
                    if (HttpContext.Current == null)
                        return;

                    var exception = HttpContext.Current.Server.GetLastError();
                    if (exception == null)
                        return;

                    var log = client.Configuration.Resolver.GetLog();
                    try {
                        log.Info(typeof(ExceptionlessClient), "HttpApplication.Error called");

                        var contextData = new ContextData();
                        contextData.MarkAsUnhandledError();
                        contextData.SetSubmissionMethod("HttpApplicationError");
                        contextData.Add("HttpContext", HttpContext.Current.ToWrapped());

                        exception.ToExceptionless(contextData, client).Submit();

                        // process queue immediately since the app is about to exit.
                        client.ProcessQueueAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                        log.Info(typeof(ExceptionlessClient), "HttpApplication.Error finished");
                    } catch (Exception ex) {
                        log.Error(typeof(ExceptionlessClientExtensions), ex, String.Concat("An error occurred while processing HttpApplication unhandled exception: ", ex.Message));
                    } finally {
                        log.Flush();
                    }
                };

            try {
                app.Error -= _onHttpApplicationError;
                app.Error += _onHttpApplicationError;
            } catch (Exception ex) {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessClientExtensions), ex, "An error occurred while wiring up to the unobserved task exception event.");
            }
        }

        public static void UnregisterHttpApplicationErrorHandler(this ExceptionlessClient client, HttpApplication app) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (_onHttpApplicationError == null)
                return;

            app.Error -= _onHttpApplicationError;
            _onHttpApplicationError = null;
        }
    }
}