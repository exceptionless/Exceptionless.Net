using System;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Serializer;
using Exceptionless.Storage;

namespace Exceptionless.Tests.Log {
    public class IsolatedStorageFileExceptionlessLogTests : FileExceptionlessLogTests {
        private readonly IsolatedStorageObjectStorage _storage;

        public IsolatedStorageFileExceptionlessLogTests() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IExceptionlessLog, NullExceptionlessLog>();
            resolver.Register<IJsonSerializer, DefaultJsonSerializer>();
            _storage = new IsolatedStorageObjectStorage(resolver);
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

        public override void Dispose() {
            base.Dispose();

            if (_storage != null)
                _storage.Dispose();
        }
    }
}