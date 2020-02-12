#if NET45
using System;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Serializer;
using Exceptionless.Storage;
using Xunit;

namespace Exceptionless.Tests.Log {
    public class IsolatedStorageFileExceptionlessLogTests : FileExceptionlessLogTestBase, IDisposable {
        private readonly IsolatedStorageObjectStorage _storage;

        public IsolatedStorageFileExceptionlessLogTests() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IExceptionlessLog, NullExceptionlessLog>();
            resolver.Register<IJsonSerializer, DefaultJsonSerializer>();
            _storage = new IsolatedStorageObjectStorage(resolver);
        }

        [Fact]
        public override void CanWriteToLogFile() {
            base.CanWriteToLogFile();
        }

        [Fact]
        public override void CheckSizeDoesNotFailIfLogIsMissing() {
            base.CheckSizeDoesNotFailIfLogIsMissing();
        }

        [Fact]
        public override Task LogFlushTimerWorks() {
            return base.LogFlushTimerWorks();
        }

        [Fact]
        public override void LogIsThreadSafe() {
            base.LogIsThreadSafe();
        }

        [Fact]
        public override void LogResetsAfter5mb() {
            base.LogResetsAfter5mb();
        }
        
        protected override FileExceptionlessLog GetLog(string filePath) {
            return new IsolatedStorageFileExceptionlessLog(filePath);
        }

        protected override bool LogExists(string path = LOG_FILE) {
            return _storage.Exists(path);
        }

        protected override void DeleteLog(string path = LOG_FILE) {
            if (LogExists(path))
                _storage.DeleteObject(path);
        }

        public void Dispose() {
            _storage?.Dispose();
        }
    }
}

#endif