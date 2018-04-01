using System;
using System.IO;
using Xunit;
using Exceptionless.Dependency;
using Exceptionless.Serializer;
using Exceptionless.Storage;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Storage {
    public class FolderFileStorageTests : FileStorageTestsBase {
        private const string DATA_DIRECTORY_QUEUE_FOLDER = @"|DataDirectory|\Queue";

        public FolderFileStorageTests(ITestOutputHelper output) : base(output) { }

        protected override IObjectStorage GetStorage() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IJsonSerializer, DefaultJsonSerializer>();
            resolver.Register<IStorageSerializer, DefaultJsonSerializer>();
            return new FolderObjectStorage(resolver, "temp");
        }

        [Fact]
        public void CanUseDataDirectory() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IJsonSerializer, DefaultJsonSerializer>();

            var storage = new FolderObjectStorage(resolver, DATA_DIRECTORY_QUEUE_FOLDER);
            Assert.NotNull(storage.Folder);
            Assert.NotEqual(DATA_DIRECTORY_QUEUE_FOLDER, storage.Folder);
            Assert.True(storage.Folder.EndsWith("Queue" + Path.DirectorySeparatorChar) || storage.Folder.EndsWith("Queue" + Path.AltDirectorySeparatorChar), storage.Folder);
        }
    }
}
