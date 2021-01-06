using System;

namespace Exceptionless.Queue {
    internal class ProcessQueueScope : IDisposable {
        private readonly ExceptionlessClient _exceptionlessClient;

        public ProcessQueueScope(ExceptionlessClient exceptionlessClient) {
            _exceptionlessClient = exceptionlessClient;
        }

        public void Dispose() {
            _exceptionlessClient.ProcessQueue();
        }
    }
}
