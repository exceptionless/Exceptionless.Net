using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Exceptionless.Extensions.Hosting {
    public sealed class ExceptionlessLifetimeService : IHostedLifecycleService {
        private readonly ExceptionlessClient _exceptionlessClient;
        private int _started;

        public ExceptionlessLifetimeService(ExceptionlessClient client) {
            _exceptionlessClient = client;
        }

        public Task StartingAsync(CancellationToken cancellationToken) {
            if (Interlocked.Exchange(ref _started, 1) == 1)
                return Task.CompletedTask;

            _exceptionlessClient.RegisterAppDomainUnhandledExceptionHandler();
            _exceptionlessClient.RegisterTaskSchedulerUnobservedTaskExceptionHandler();
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        public Task StartedAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        public Task StoppingAsync(CancellationToken cancellationToken) {
            if (Volatile.Read(ref _started) == 0)
                return Task.CompletedTask;

            return _exceptionlessClient.ProcessQueueAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            if (Interlocked.Exchange(ref _started, 0) == 0)
                return Task.CompletedTask;

            return _exceptionlessClient.ShutdownAsync();
        }

        public Task StoppedAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
    }
}
