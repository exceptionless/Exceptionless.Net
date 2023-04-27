using Exceptionless.Storage;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Storage {
    public class InMemoryFileStorageTests : FileStorageTestsBase {
        public InMemoryFileStorageTests(ITestOutputHelper output) : base(output) { }

        protected override IObjectStorage GetStorage() {
            return new InMemoryObjectStorage();
        }

        [Fact]
        public void SaveObject_AddObject_WillRespectMaxItems() {
            // Assign
            var storage = new InMemoryObjectStorage(2);
            storage.SaveObject("1.json", "1");
            storage.SaveObject("2.json", "2");

            // Act
            storage.SaveObject("3.json", "3");

            // Assert
            Assert.Equal(storage.MaxObjects, storage.Count);
            Assert.False(storage.Exists("1.json"));
            Assert.True(storage.Exists("2.json"));
            Assert.True(storage.Exists("3.json"));
        }
    }
}
