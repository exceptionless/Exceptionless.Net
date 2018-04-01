using System;
using Exceptionless.Storage;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Storage {
    public class InMemoryFileStorageTests : FileStorageTestsBase {
        public InMemoryFileStorageTests(ITestOutputHelper output) : base(output) { }

        protected override IObjectStorage GetStorage() {
            return new InMemoryObjectStorage();
        }
    }
}
