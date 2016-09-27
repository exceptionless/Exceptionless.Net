using System;
using System.Threading;
using Exceptionless.Storage;
using Exceptionless.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Storage {
    public class LockFileTests {
        private readonly TestOutputWriter _writer;

        public LockFileTests(ITestOutputHelper output) {
            _writer = new TestOutputWriter(output);
        }

        [Fact]
        public void AcquireTimeOut() {
            _writer.WriteLine("Acquire Lock 1");
            var lock1 = LockFile.Acquire("test.lock");

            _writer.WriteLine("Acquire Lock 2");
            Assert.Throws<TimeoutException>(() => LockFile.Acquire("test.lock", TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public void Acquire() {
            var thread1 = new Thread(s => {
                _writer.WriteLine("[Thread: {0}] Lock 1 Entry", Thread.CurrentThread.ManagedThreadId);
                using (var lock1 = LockFile.Acquire("Acquire.lock")) {
                    _writer.WriteLine("[Thread: {0}] Lock 1", Thread.CurrentThread.ManagedThreadId);
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            });
            thread1.Start();

            var thread2 = new Thread(s => {
                _writer.WriteLine("[Thread: {0}] Lock 2 Entry", Thread.CurrentThread.ManagedThreadId);
                using (var lock2 = LockFile.Acquire("Acquire.lock")) {
                    _writer.WriteLine("[Thread: {0}] Lock 2", Thread.CurrentThread.ManagedThreadId);
                }
            });

            thread2.Start();
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}