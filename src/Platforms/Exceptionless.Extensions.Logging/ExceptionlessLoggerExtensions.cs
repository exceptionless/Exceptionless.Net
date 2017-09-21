using System;
using Exceptionless.Extensions.Logging;
using ExceptionlessLogLevel = Exceptionless.Logging.LogLevel;
using Microsoft.Extensions.Logging;

namespace Exceptionless {
    public static class ExceptionlessLoggerExtensions {
        public static ExceptionlessLogLevel ToLogLevel(this LogLevel level) {
            if (level == LogLevel.Trace)
                return ExceptionlessLogLevel.Trace;
            if (level == LogLevel.Debug)
                return ExceptionlessLogLevel.Debug;
            if (level == LogLevel.Information)
                return ExceptionlessLogLevel.Info;
            if (level == LogLevel.Warning)
                return ExceptionlessLogLevel.Warn;
            if (level == LogLevel.Error)
                return ExceptionlessLogLevel.Error;
            if (level == LogLevel.Critical)
                return ExceptionlessLogLevel.Fatal;
            if (level == LogLevel.None)
                return ExceptionlessLogLevel.Off;

            return ExceptionlessLogLevel.Off;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline using the default client.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        public static ILoggerFactory AddExceptionless(this ILoggerFactory factory) {
            factory.AddProvider(new ExceptionlessLoggerProvider(ExceptionlessClient.Default));
            return factory;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline using a new client with the provided api key.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="apiKey">The project api key.</param>
        /// <param name="serverUrl">The Server Url</param>
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        public static ILoggerFactory AddExceptionless(this ILoggerFactory factory, string apiKey, string serverUrl = null) {
            if (String.IsNullOrEmpty(apiKey) && String.IsNullOrEmpty(serverUrl))
                return factory.AddExceptionless();

            factory.AddProvider(new ExceptionlessLoggerProvider(config => {
                if (!String.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE")
                    config.ApiKey = apiKey;
                if (!String.IsNullOrEmpty(serverUrl))
                    config.ServerUrl = serverUrl;

                config.UseInMemoryStorage();
            }));

            return factory;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline using a new client configured with the provided action.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="configure">An <see cref="Action{ExceptionlessConfiguration}"/> that applies additional settings and plugins. The project api key must be specified.</param>
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        public static ILoggerFactory AddExceptionless(this ILoggerFactory factory, Action<ExceptionlessConfiguration> configure) {
            factory.AddProvider(new ExceptionlessLoggerProvider(configure));
            return factory;
        }
    }
}