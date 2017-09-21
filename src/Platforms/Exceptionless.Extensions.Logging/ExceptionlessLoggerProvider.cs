using Microsoft.Extensions.Logging;
using System;

namespace Exceptionless.Extensions.Logging
{
    public class ExceptionlessLoggerProvider : ILoggerProvider
    {
        ExceptionlessClient _Client;
        bool _ShouldDisposeClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessLoggerProvider"/> class.
        /// </summary>
        /// <param name="config">An <see cref="ExceptionlessConfiguration"/> which will be provided to created loggers.</param>
        public ExceptionlessLoggerProvider(ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            _Client = client;
            _ShouldDisposeClient = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessLoggerProvider"/> class.
        /// </summary>
        /// <param name="configure">An <see cref="Action{ExceptionlessConfiguration}"/> which will be used to configure created loggers.</param>
        public ExceptionlessLoggerProvider(Action<ExceptionlessConfiguration> configure) {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            _Client = new ExceptionlessClient(configure);
            _Client.Startup();
            _ShouldDisposeClient = true;
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
            _Client.ProcessQueue();
            if (_ShouldDisposeClient) 
            {
                _Client.Shutdown();
                ((IDisposable)_Client).Dispose();
            }
        }
    }
}
