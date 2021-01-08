using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Exceptionless.SampleHosting {
    internal class SampleService : IHostedService {
        private readonly ExceptionlessClient _exceptionlessClient;
        private readonly ILogger _logger;

        public SampleService(ExceptionlessClient exceptionlessClient, ILogger<SampleService> logger) {
            _exceptionlessClient = exceptionlessClient;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Starting sample service.");

            // Submit a feature usage event directly using the client instance that is injected from the DI container.
            _exceptionlessClient.SubmitFeatureUsage("SampleService");

            // This log message will get sent to Exceptionless since Exceptionless has be added to the logging system in Program.cs.
            _logger.LogWarning("Test warning message");

            return Task.Run(() => {
                try {
                    throw new Exception($"Handled Exception: {Guid.NewGuid()}");
                }
                catch (Exception handledException) {
                    // Use the ToExceptionless extension method to submit this handled exception to Exceptionless using the client instance from DI.
                    handledException.ToExceptionless(_exceptionlessClient).Submit();
                }

                try {
                    throw new Exception($"Handled Exception (Default Client): {Guid.NewGuid()}");
                }
                catch (Exception handledException) {
                    // Use the ToExceptionless extension method to submit this handled exception to Exceptionless using the default client instance (ExceptionlessClient.Default).
                    // This works and is convenient, but its generally not recommended to use static singleton instances because it makes testing and
                    // other things harder.
                    handledException.ToExceptionless().Submit();
                }
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Stopping sample service.");
            return Task.CompletedTask;
        }
    }
}
