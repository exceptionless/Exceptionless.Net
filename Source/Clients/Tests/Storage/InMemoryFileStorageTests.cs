using System;
using Exceptionless.Storage;

namespace Exceptionless.Tests.Storage {
    public class InMemoryFileStorageTests : FileStorageTestsBase {
        protected override IObjectStorage GetStorage() {
            return new InMemoryObjectStorage();
        }
    }
}
