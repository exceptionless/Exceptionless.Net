﻿#if NET45
using System;
using System.IO;
using System.IO.IsolatedStorage;
using Exceptionless.Storage;
using Exceptionless.Utility;

namespace Exceptionless.Logging {
    public class IsolatedStorageFileExceptionlessLog : FileExceptionlessLog {
        public IsolatedStorageFileExceptionlessLog(string filePath, bool append = false) : base(filePath, append) {}

        protected override void Initialize() {}

        private IsolatedStorageFile GetStore() {
#if NETSTANDARD
            return Run.WithRetries(() => IsolatedStorageFile.GetUserStoreForApplication());
#else
            return Run.WithRetries(() => IsolatedStorageFile.GetStore(IsolatedStorageScope.Machine | IsolatedStorageScope.Assembly, typeof(IsolatedStorageObjectStorage), null));
#endif
        }

        protected override WrappedDisposable<StreamWriter> GetWriter(bool append = false) {
            var store = GetStore();
            return new WrappedDisposable<StreamWriter>(new StreamWriter(new IsolatedStorageFileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, store)), store.Dispose);
        }

        protected override WrappedDisposable<Stream> GetReader() {
            var store = GetStore();
            return new WrappedDisposable<Stream>(new IsolatedStorageFileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, store), store.Dispose);
        }

        protected internal override string GetFileContents() {
            return Run.WithRetries(() => {
                using (var store = GetStore()) {
                    if (!store.FileExists(FilePath))
                        return String.Empty;

                    using (var stream = new IsolatedStorageFileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, store)) {
                        using (var reader = new StreamReader(stream)) {
                            return reader.ReadToEnd();
                        }
                    }
                }
            });
        }
        
        protected internal override long GetFileSize() {
            using (var store = GetStore()) {
                string fullPath = store.GetFullPath(FilePath);
                try {
                    if (File.Exists(fullPath))
                        return new FileInfo(fullPath).Length;
                } catch (IOException ex) {
                    System.Diagnostics.Trace.WriteLine("Exceptionless: Error getting size of file: {0}", ex.ToString());
                }
            }

            return -1;
        }
    }
}
#endif