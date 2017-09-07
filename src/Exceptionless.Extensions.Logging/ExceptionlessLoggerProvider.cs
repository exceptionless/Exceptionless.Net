using Microsoft.Extensions.Logging;
using System;

namespace Exceptionless.Extensions.Logging
{
    public class ExceptionlessLoggerProvider : ILoggerProvider
    {
        ExceptionlessClient _Client;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessLoggerProvider"/> class.
        /// </summary>
        /// <param name="config">An <see cref="ExceptionlessConfiguration"/> which will be provided to created loggers.</param>
        public ExceptionlessLoggerProvider(ExceptionlessConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _Client = new ExceptionlessClient(config);
            _Client.Startup();
        }

        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>An <see cref="ILogger"/></returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new ExceptionlessLogger(_Client, categoryName);
        }

        public void Dispose()
        {
            _Client.Shutdown();
        }
    }
}
