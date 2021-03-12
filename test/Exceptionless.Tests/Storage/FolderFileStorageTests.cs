using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Exceptionless.Dependency;
using Exceptionless.Logging;
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
            resolver.Register<IExceptionlessLog, NullExceptionlessLog>();

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


        [Fact]
        public void Get_Save_Object_Multiple_Test() {

            var storage = GetStorage();
            var list = new List<int>() { 1, 2, 3 };
            var path = "test.json";

            //
            storage.SaveObject(path, list);
            var list2 = storage.GetObject<List<int>>(path);
            Assert.Equal(list,list2);


            //Save after adding items to the list 
            list.Add(4);
            storage.SaveObject(path, list);
            list2 = storage.GetObject<List<int>>(path);
            Assert.Equal(list, list2);

            //Save the list after remove items 
            list.Remove(4);
            storage.SaveObject(path, list);
            list2 = storage.GetObject<List<int>>(path);
            Assert.Equal(list, list2);


        }
    }
}
