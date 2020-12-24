using System;
using Exceptionless.Extensions.Logging;
using ExceptionlessLogLevel = Exceptionless.Logging.LogLevel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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
        /// Adds Exceptionless to the logging pipeline using the specified <see cref="ExceptionlessClient"/> instance.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/>.</param>
        /// <param name="client">The <see cref="ExceptionlessClient"/> instance that will be used.</param>
        /// <returns>The <see cref="ILoggingBuilder"/>.</returns>
        public static ILoggingBuilder AddExceptionless(this ILoggingBuilder builder, ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            builder.AddProvider(new ExceptionlessLoggerProvider(client));
            return builder;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline using the <see cref="ExceptionlessClient"/> instance retrieved from the <see cref="IServiceProvider"/> or the <see cref="ExceptionlessClient.Default"/> instance.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/>.</param>
        /// <returns>The <see cref="ILoggingBuilder"/>.</returns>
        public static ILoggingBuilder AddExceptionless(this ILoggingBuilder builder) {
            builder.Services.AddSingleton<ILoggerProvider>(sp => new ExceptionlessLoggerProvider(sp.GetService<ExceptionlessClient>() ?? ExceptionlessClient.Default));
            return builder;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline using a new client with the provided api key.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/>.</param>
        /// <param name="apiKey">The project api key.</param>
        /// <param name="serverUrl">The Server Url.</param>
        /// <returns>The <see cref="ILoggingBuilder"/>.</returns>
        public static ILoggingBuilder AddExceptionless(this ILoggingBuilder builder, string apiKey, string serverUrl = null) {
            if (String.IsNullOrEmpty(apiKey) && String.IsNullOrEmpty(serverUrl))
                return builder.AddExceptionless();

            builder.AddProvider(new ExceptionlessLoggerProvider(config => {
                if (!String.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE")
                    config.ApiKey = apiKey;
                if (!String.IsNullOrEmpty(serverUrl))
                    config.ServerUrl = serverUrl;

                config.UseInMemoryStorage();
            }));

            return builder;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline using a new client configured with the provided action.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/>.</param>
        /// <param name="configure">An <see cref="Action{ExceptionlessConfiguration}"/> that applies additional settings and plugins. The project api key must be specified.</param>
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        public static ILoggingBuilder AddExceptionless(this ILoggingBuilder builder, Action<ExceptionlessConfiguration> configure) {
            builder.AddProvider(new ExceptionlessLoggerProvider(configure));
            return builder;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline using the <see cref="ExceptionlessClient.Default"/>.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="client">If a client is not specified then the <see cref="ExceptionlessClient.Default"/> will be used.</param>
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        [Obsolete("Use ExceptionlessLoggerExtensions.AddExceptionless(ILoggingBuilder,ExceptionlessClient) instead.")]
        public static ILoggerFactory AddExceptionless(this ILoggerFactory factory, ExceptionlessClient client = null) {
            factory.AddProvider(new ExceptionlessLoggerProvider(client ?? ExceptionlessClient.Default));
            return factory;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline using a new client with the provided api key.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="apiKey">The project api key.</param>
        /// <param name="serverUrl">The Server Url</param>
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        [Obsolete("Use ExceptionlessLoggerExtensions.AddExceptionless(ILoggingBuilder,string,string) instead.")]
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
        [Obsolete("Use ExceptionlessLoggerExtensions.AddExceptionless(ILoggingBuilder,Action<ExceptionlessConfiguration>) instead.")]
        public static ILoggerFactory AddExceptionless(this ILoggerFactory factory, Action<ExceptionlessConfiguration> configure) {
            factory.AddProvider(new ExceptionlessLoggerProvider(configure));
            return factory;
        }
    }
}