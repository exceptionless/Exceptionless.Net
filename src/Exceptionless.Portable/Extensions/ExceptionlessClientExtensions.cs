using System;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Plugins;
using Exceptionless.Logging;
using Exceptionless.Services;
using Exceptionless.Submission;

namespace Exceptionless {
    public static class ExceptionlessClientExtensions {
        /// <summary>
        /// Reads configuration settings, configures various plugins and wires up to platform specific exception handlers. 
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        public static void Startup(this ExceptionlessClient client) {
            if (client.Configuration.Resolver.HasDefaultRegistration<ISubmissionClient, DefaultSubmissionClient>())
                client.Configuration.Resolver.Register<ISubmissionClient, SubmissionClient>();

            if (client.Configuration.Resolver.HasDefaultRegistration<IEnvironmentInfoCollector, DefaultEnvironmentInfoCollector>())
                client.Configuration.Resolver.Register<IEnvironmentInfoCollector, EnvironmentInfoCollector>();

            client.Configuration.ReadAllConfig();
#if !NETPORTABLE && !NETSTANDARD1_2
            client.Configuration.UseErrorPlugin();
            client.Configuration.UseTraceLogEntriesPlugin();
            client.Configuration.AddPlugin<VersionPlugin>();
#endif

            if (client.Configuration.UpdateSettingsWhenIdleInterval == null)
                client.Configuration.UpdateSettingsWhenIdleInterval = TimeSpan.FromMinutes(2);

#if NETSTANDARD1_5 || NET45
            client.RegisterAppDomainUnhandledExceptionHandler();
#endif
            client.RegisterTaskSchedulerUnobservedTaskExceptionHandler();
        }

        /// <summary>
        /// Unregisters platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        public static void Shutdown(this ExceptionlessClient client) {
#if NETSTANDARD1_5 || NET45
            client.UnregisterAppDomainUnhandledExceptionHandler();
#endif
            client.UnregisterTaskSchedulerUnobservedTaskExceptionHandler();
        }
    }
}

namespace Exceptionless.Extensions {
    public static class ExceptionlessClientExtensions {
#if NETSTANDARD1_5 || NET45
        private static UnhandledExceptionEventHandler _onAppDomainUnhandledException;
        public static void RegisterAppDomainUnhandledExceptionHandler(this ExceptionlessClient client) {
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
            if (_onAppDomainUnhandledException == null)
                return;
            
            AppDomain.CurrentDomain.UnhandledException -= _onAppDomainUnhandledException;
            _onAppDomainUnhandledException = null;
        }
#endif

        private static EventHandler<UnobservedTaskExceptionEventArgs> _onTaskSchedulerOnUnobservedTaskException;
        public static void RegisterTaskSchedulerUnobservedTaskExceptionHandler(this ExceptionlessClient client) {
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
            if (_onTaskSchedulerOnUnobservedTaskException == null)
                return;

            TaskScheduler.UnobservedTaskException -= _onTaskSchedulerOnUnobservedTaskException;
            _onTaskSchedulerOnUnobservedTaskException = null;
        }
    }
}