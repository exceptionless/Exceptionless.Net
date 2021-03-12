using System;
using System.Collections.Generic;
using System.IO;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Utility;

namespace Exceptionless.Storage {
    public class FolderObjectStorage : IObjectStorage {
        private readonly object _lockObject = new object();
        private readonly IDependencyResolver _resolver;

        public FolderObjectStorage(IDependencyResolver resolver, string folder) {
            _resolver = resolver;

            folder = PathHelper.ExpandPath(folder);

            if (!Path.IsPathRooted(folder))
                folder = Path.GetFullPath(folder);
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()) && !folder.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                folder += Path.DirectorySeparatorChar;

            Folder = folder;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        public string Folder { get; set; }

        public T GetObject<T>(string path) where T : class {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            try {
                using (var reader = File.OpenRead(Path.Combine(Folder, path))) {
                    return _resolver.GetStorageSerializer().Deserialize<T>(reader);
                }
            } catch (Exception ex) {
                _resolver.GetLog().Error(ex.Message, exception: ex);
                return null;
            }
        }

        public ObjectInfo GetObjectInfo(string path) {
            var info = new System.IO.FileInfo(path);
            if (!info.Exists)
                return null;

            return new ObjectInfo {
                Path = path.Replace(Folder, String.Empty),
                Created = info.CreationTime,
                Modified = info.LastWriteTime
            };
        }

        public bool Exists(string path) {
            return File.Exists(Path.Combine(Folder, path));
        }

        public bool SaveObject<T>(string path, T value) where T : class {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            try {
                string directory = Path.GetDirectoryName(Path.Combine(Folder, path));
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                using (var writer = File.Open(Path.Combine(Folder, path), FileMode.Create)) {
                    _resolver.GetStorageSerializer().Serialize(value, writer);
                }
            } catch (Exception ex) {
                _resolver.GetLog().Error(ex.Message, exception: ex);
                return false;
            }

            return true;
        }

        public bool RenameObject(string oldpath, string newpath) {
            if (String.IsNullOrEmpty(oldpath))
                throw new ArgumentNullException("oldpath");
            if (String.IsNullOrEmpty(newpath))
                throw new ArgumentNullException("newpath");

            try {
                lock (_lockObject) {
                    File.Move(Path.Combine(Folder, oldpath), Path.Combine(Folder, newpath));
                }
            } catch (Exception ex) {
                _resolver.GetLog().Error(ex.Message, exception: ex);
                return false;
            }

            return true;
        }

        public bool DeleteObject(string path) {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            try {
                File.Delete(Path.Combine(Folder, path));
            } catch (Exception ex) {
                _resolver.GetLog().Error(ex.Message, exception: ex);
                return false;
            }

            return true;
        }

        public IEnumerable<ObjectInfo> GetObjectList(string searchPattern = null, int? limit = null, DateTime? maxCreatedDate = null) {
            if (String.IsNullOrEmpty(searchPattern))
                searchPattern = "*";

            if (!maxCreatedDate.HasValue)
                maxCreatedDate = DateTime.MaxValue;

            var list = new List<ObjectInfo>();

            try {
                foreach (var path in Directory.EnumerateFiles(Folder, searchPattern, SearchOption.AllDirectories)) {
                    var info = new System.IO.FileInfo(path);
                    if (!info.Exists || info.CreationTime > maxCreatedDate)
                        continue;

                    list.Add(new ObjectInfo {
                        Path = path.Replace(Folder, String.Empty),
                        Created = info.CreationTime,
                        Modified = info.LastWriteTime
                    });

                    if (list.Count == limit)
                        break;
                }
            } catch (DirectoryNotFoundException) {
            } catch (Exception ex) {
                _resolver.GetLog().Error(ex.Message, exception: ex);
            }

            return list;
        }

        public void Dispose() {}
    }
}