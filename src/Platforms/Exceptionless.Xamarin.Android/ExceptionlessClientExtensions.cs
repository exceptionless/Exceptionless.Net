using System;
using Android.Runtime;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Plugins;

namespace Exceptionless.Xamarin.Android
{
    public static class ExceptionlessClientExtensions {
        private static UnhandledExceptionEventHandler _onAppDomainUnhandledException;
        private static EventHandler<RaiseThrowableEventArgs> _onAndroidEnvironmentUnhandledException;
        public static void RegisterAppDomainUnhandledExceptionHandler(this ExceptionlessClient client)
        {
            if (_onAppDomainUnhandledException == null)
            {
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
                };
            }

            try
            {
                AppDomain.CurrentDomain.UnhandledException -= _onAppDomainUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += _onAppDomainUnhandledException;
            }
            catch (Exception ex)
            {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessClientExtensions), ex, "An error occurred while wiring up to the unhandled exception event.");
            }
        }

        public static void UnregisterAppDomainUnhandledExceptionHandler(this ExceptionlessClient client)
        {
            if (_onAppDomainUnhandledException == null)
                return;

            AppDomain.CurrentDomain.UnhandledException -= _onAppDomainUnhandledException;
            _onAppDomainUnhandledException = null;
        }

        public static void RegisterAndroidEnvironmentUnhandledExceptionRaiser(this ExceptionlessClient client)
        {
            if (_onAndroidEnvironmentUnhandledException == null)
            {
                _onAndroidEnvironmentUnhandledException = (sender, args) => {
                    var exception = args.Exception;
                    if (exception == null)
                        return;

                    var contextData = new ContextData();
                    contextData.MarkAsUnhandledError();
                    contextData.SetSubmissionMethod("AndroidEnvironmentUnhandledException");

                    exception.ToExceptionless(contextData, client).Submit();

                    // process queue immediately since the app is about to exit.
                    client.ProcessQueue();
                };
            }

            try
            {
                AndroidEnvironment.UnhandledExceptionRaiser -= _onAndroidEnvironmentUnhandledException;
                AndroidEnvironment.UnhandledExceptionRaiser += _onAndroidEnvironmentUnhandledException;
            }
            catch (Exception ex)
            {
                client.Configuration.Resolver.GetLog().Error(typeof(ExceptionlessClientExtensions), ex, "An error occurred while wiring up to the unhandled exception event.");
            }
        }

        public static void UnregisterAndroidEnvironmentUnhandledExceptionRaiser(this ExceptionlessClient client)
        {
            if (_onAndroidEnvironmentUnhandledException == null)
                return;

            AndroidEnvironment.UnhandledExceptionRaiser -= _onAndroidEnvironmentUnhandledException;
            _onAndroidEnvironmentUnhandledException = null;
        }
    }
}