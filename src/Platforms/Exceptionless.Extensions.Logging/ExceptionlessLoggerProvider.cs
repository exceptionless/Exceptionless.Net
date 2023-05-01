using Microsoft.Extensions.Logging;
using System;

namespace Exceptionless.Extensions.Logging {
    public class ExceptionlessLoggerProvider : ILoggerProvider {
        private readonly ExceptionlessClient _client;
        private readonly bool _shouldDispose;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessLoggerProvider"/> class.
        /// </summary>
        public ExceptionlessLoggerProvider(ExceptionlessClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));

            // Rely on Logging Rules
            _client.Configuration.SetDefaultMinLogLevel(Exceptionless.Logging.LogLevel.Trace);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessLoggerProvider"/> class.
        /// </summary>
        /// <param name="configure">An <see cref="Action{ExceptionlessConfiguration}"/> which will be used to configure created loggers.</param>
        public ExceptionlessLoggerProvider(Action<ExceptionlessConfiguration> configure) {
            _client = ExceptionlessClient.Default;

            // Rely on Logging Rules
            _client.Configuration.SetDefaultMinLogLevel(Exceptionless.Logging.LogLevel.Trace);
            
            configure?.Invoke(_client.Configuration);
            _shouldDispose = true;
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>An <see cref="T:Microsoft.Extensions.Logging.ILogger" /></returns>
        public ILogger CreateLogger(string categoryName) {
            return new ExceptionlessLogger(_client, categoryName);
        }

        public void Dispose() {
            _client.ProcessQueueAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (_shouldDispose)
                ((IDisposable)_client).Dispose();
        }
    }
}