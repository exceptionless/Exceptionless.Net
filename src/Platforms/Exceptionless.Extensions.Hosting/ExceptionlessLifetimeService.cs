using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Exceptionless.Extensions.Hosting {
    public class ExceptionlessLifetimeService : IHostedService {
        private readonly ExceptionlessClient _exceptionlessClient;

        public ExceptionlessLifetimeService(ExceptionlessClient client, IHostApplicationLifetime appLifetime) {
            _exceptionlessClient = client;
            
            _exceptionlessClient.RegisterAppDomainUnhandledExceptionHandler();
            _exceptionlessClient.RegisterTaskSchedulerUnobservedTaskExceptionHandler();

            appLifetime.ApplicationStopping.Register(() => _exceptionlessClient.ProcessQueue());
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
    }
}
