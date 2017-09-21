using Exceptionless.Extensions.Logging;
using Microsoft.Extensions.Logging;
using System;

namespace Exceptionless
{
    public static class ExceptionlessLoggerExtensions
    {
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
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        public static ILoggerFactory AddExceptionless(this ILoggerFactory factory, string apiKey)
        {
            factory.AddProvider(new ExceptionlessLoggerProvider((config) => config.ApiKey = apiKey));
            return factory;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline using a new client configured with the provided action.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="configure">An <see cref="Action{ExceptionlessConfiguration}"/> that applies additional settings and plugins. The project api key must be specified.</param>
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        public static ILoggerFactory AddExceptionless(this ILoggerFactory factory, Action<ExceptionlessConfiguration> configure)
        {
            factory.AddProvider(new ExceptionlessLoggerProvider(configure));
            return factory;
        }
    }
}
