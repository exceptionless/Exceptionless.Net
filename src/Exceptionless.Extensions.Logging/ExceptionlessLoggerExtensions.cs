using Exceptionless.Extensions.Logging;
using Microsoft.Extensions.Logging;
using System;

namespace Exceptionless
{
    public static class ExceptionlessLoggerExtensions
    {
        /// <summary>
        /// Adds Exceptionless to the logging pipeline.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="apiKey">The project api key.</param>
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        public static ILoggerFactory AddExceptionless(this ILoggerFactory factory, string apiKey)
        {
            ExceptionlessConfiguration config = new ExceptionlessConfiguration(ExceptionlessClient.Default.Configuration.Resolver);
            config.ApiKey = apiKey;

            factory.AddProvider(new ExceptionlessLoggerProvider(config));
            return factory;
        }

        /// <summary>
        /// Adds Exceptionless to the logging pipeline.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="config">An <see cref="ExceptionlessConfiguration"/> containing additional settings and plugins. The project api key must be specified.</param>
        /// <returns>The <see cref="ILoggerFactory"/>.</returns>
        public static ILoggerFactory AddExceptionless(this ILoggerFactory factory, ExceptionlessConfiguration config)
        {
            factory.AddProvider(new ExceptionlessLoggerProvider(config));
            return factory;
        }
    }
}
