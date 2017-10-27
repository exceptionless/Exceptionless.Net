﻿using System;
using Exceptionless.Dependency;
using Exceptionless.Serializer;
using Exceptionless.Storage;

namespace Exceptionless.Tests.Storage {
    public class IsolatedStorageFileStorageTests : FileStorageTestsBase {
        protected override IObjectStorage GetStorage() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IJsonSerializer, DefaultJsonSerializer>();
            resolver.Register<IStorageSerializer, DefaultJsonSerializer>();
            return new IsolatedStorageObjectStorage(resolver);
        }
    }
}
