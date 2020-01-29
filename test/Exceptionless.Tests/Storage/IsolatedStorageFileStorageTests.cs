#if NET45
using System;
using Exceptionless.Dependency;
using Exceptionless.Serializer;
using Exceptionless.Storage;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Storage {
    public class IsolatedStorageFileStorageTests : FileStorageTestsBase {
        public IsolatedStorageFileStorageTests(ITestOutputHelper output) : base(output) { }

        protected override IObjectStorage GetStorage() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IJsonSerializer, DefaultJsonSerializer>();
            resolver.Register<IStorageSerializer, DefaultJsonSerializer>();
            return new IsolatedStorageObjectStorage(resolver);
        }
    }
}
#endif