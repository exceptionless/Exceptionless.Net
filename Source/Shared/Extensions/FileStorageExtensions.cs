using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Models;
using Exceptionless.Storage;
using System.Text;
using System.Text.RegularExpressions;

namespace Exceptionless.Extensions
{
    public static class FileStorageExtensions
    {
        private static readonly object _lockObject = new object();

        private static readonly char WildcardFilterChar = '*';
        private static readonly char DirectorySeparatorChar = '\\';
        private static readonly string JsonExtension = "json";
        private static readonly string QueueDirectoryName = "q";
        private static readonly string LockExtension = "x";

        private static readonly TimeSpan DefaultQueueMaxAge = TimeSpan.FromDays(7);
        private static readonly int DefaultMaxUploadAttempts = 10;

        #region File storage helper classes
        /// <summary>
        /// File storage mechanism used to simplify attempt counting and renaming
        /// </summary>
        private class FileStorageName
        {
            public string Name { get; set; }
            public int Attempts { get; set; }
            public string Extension { get; set; }

            private static ArgumentException AttemptsArgumentException(string name)
            {
                return new ArgumentException(String.Format("Path \"{0}\" must contain the number of attempts.", name));
            }

            public FileStorageName(string name)
            {
                string[] parts = name.Split('.');

                try
                {
                    this.Name = parts[0];

                    this.Attempts = int.Parse(parts[1]);

                    this.Extension = parts[2];
                }
                catch (IndexOutOfRangeException)
                {
                    throw AttemptsArgumentException(name);
                }
                catch (FormatException)
                {
                    throw AttemptsArgumentException(name);
                }
            }

            public override string ToString()
            {
                return string.Join(".", Name, Attempts, Extension);
            }
        }

        /// <summary>
        /// File storage mechanism to simplify locking
        /// </summary>
        private class FileStorageLock
        {
            public string FullName { get; set; }
            public bool Locked { get; set; }

            public FileStorageLock(string name)
            {
                string lockSuffix = '.' + LockExtension;
                Locked = name.EndsWith(lockSuffix);
                FullName = Locked ? name.Remove(name.Length - lockSuffix.Length) : name;
            }

            public override string ToString()
            {
                return Locked ? FullName + '.' + LockExtension : FullName;
            }
        }
        #endregion File storage helper classes

        /// <summary>
        /// Gets the Queue Directory name of format: queueName\q
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        private static string GetQueueDirectory(string queueName)
        {
            return queueName + DirectorySeparatorChar + QueueDirectoryName;
        }

        /// <summary>
        /// Gets a new queued file name of format: 86998443a1294466bd12c1f9258cc394.json
        /// </summary>
        /// <returns></returns>
        private static string GetNewQueuedItemName()
        {
            return Guid.NewGuid().ToString("N") + '.' + JsonExtension;
        }

        /// <summary>
        /// Gets a filter for all items under the queue, regardless of extension: queueName\q\*
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        private static string GetAllQueueItemsFilter(string queueName)
        {
            return GetQueueDirectory(queueName) + DirectorySeparatorChar + WildcardFilterChar;
        }

        /// <summary>
        /// Gets a filter for all json (not including sub-extensions such as .json.x): queueName\q\*.json
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        private static string GetJsonQueueItemsFilter(string queueName)
        {
            return GetQueueDirectory(queueName) + DirectorySeparatorChar + WildcardFilterChar + '.' + JsonExtension;
        }

        private static string GetAllLockedQueueItemsFilter(string queueName)
        {
            return GetQueueDirectory(queueName) + DirectorySeparatorChar + WildcardFilterChar + '.' + LockExtension;
        }

        /// <summary>
        /// Saves an event as a json file in the designated storage queue
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="queueName"></param>
        /// <param name="ev"></param>
        public static void Enqueue(this IObjectStorage storage, string queueName, Event ev)
        {
            string queuedFilePath = GetQueueDirectory(queueName) + DirectorySeparatorChar + GetNewQueuedItemName();
            storage.SaveObject(queuedFilePath, ev);
        }

        /// <summary>
        /// Removes any files from the queue which are older than a maxAge or which have had an upload attempted more than maxAttempts times
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="queueName"></param>
        /// <param name="maxAge"></param>
        /// <param name="maxAttempts"></param>
        public static void CleanupQueueFiles(this IObjectStorage storage, string queueName, TimeSpan? maxAge = null, int? maxAttempts = null)
        {
            maxAge = maxAge ?? DefaultQueueMaxAge;
            maxAttempts = maxAttempts ?? DefaultMaxUploadAttempts;

            if (maxAttempts.Value <= 0)
                maxAttempts = DefaultMaxUploadAttempts;

            foreach (var file in storage.GetObjectList(GetAllQueueItemsFilter(queueName)))
            {
                if (DateTime.Now.Subtract(file.Created) > maxAge)
                    storage.DeleteObject(file.Path);
                else if (GetAttempts(file) >= maxAttempts)
                    storage.DeleteObject(file.Path);
            }
        }

        public static void CleanupAllQueueFiles(this IObjectStorage storage, string queueName)
        {
            storage.CleanupQueueFiles(queueName, TimeSpan.Zero, 0);
        }

        /// <summary>
        /// Gets a list of objects from the designated storage location, up to a specified limit and max age (maxCreatedDate)
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="queueName"></param>
        /// <param name="limit"></param>
        /// <param name="maxCreatedDate"></param>
        /// <returns></returns>
        public static ICollection<ObjectInfo> GetQueueFiles(this IObjectStorage storage, string queueName, int? limit = null, DateTime? maxCreatedDate = null)
        {
            string filter = GetJsonQueueItemsFilter(queueName);
            return storage.GetObjectList(filter, limit, maxCreatedDate).OrderByDescending(f => f.Created).ToList();
        }

        /// <summary>
        /// Edits the file extension of an item in the storage queue to indicate another attempt at upload
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool IncrementAttempts(this IObjectStorage storage, ObjectInfo info)
        {
            var fileInfo = new FileStorageName(info.Path);
            fileInfo.Attempts++;

            return storage.RenameObject(info.Path, fileInfo.ToString());
        }

        public static int GetAttempts(this ObjectInfo info)
        {
            try
            {
                var fileInfo = new FileStorageName(info.Path);
                return fileInfo.Attempts;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static bool LockFile(this IObjectStorage storage, ObjectInfo info)
        {
            // Get lock info
            var fileLockInfo = new FileStorageLock(info.Path);

            // Lock acquisition failure if already locked
            if (fileLockInfo.Locked)
                return false;

            // Try to lock the file by renaming
            fileLockInfo.Locked = true;
            string lockedPath = fileLockInfo.ToString();

            bool success = storage.RenameObject(info.Path, lockedPath);
            info.Path = success ? lockedPath : info.Path;

            // Successful if the lock name was acquired
            return success;
        }

        public static bool ReleaseFile(this IObjectStorage storage, ObjectInfo info)
        {
            // Get lock info
            var fileLockInfo = new FileStorageLock(info.Path);

            // Lock acquisition failure if already unlocked
            if (!fileLockInfo.Locked)
                return false;

            // Try to unlock the file by renaming
            fileLockInfo.Locked = false;
            string unlockedPath = fileLockInfo.ToString();

            bool success = storage.RenameObject(info.Path, unlockedPath);
            info.Path = success ? unlockedPath : info.Path;

            // Successful if the lock name was acquired
            return success;
        }

        public static void ReleaseStaleLocks(this IObjectStorage storage, string queueName, TimeSpan? maxLockAge = null)
        {
            if (!maxLockAge.HasValue)
                maxLockAge = TimeSpan.FromMinutes(60);

            foreach (var file in storage.GetObjectList(GetAllLockedQueueItemsFilter(queueName)).Where(f => f.Modified < DateTime.Now.Subtract(maxLockAge.Value)))
                storage.ReleaseFile(file);
        }

        public static IList<Tuple<ObjectInfo, Event>> GetEventBatch(this IObjectStorage storage, string queueName, IJsonSerializer serializer, int batchSize = 50, DateTime? maxCreatedDate = null)
        {
            var events = new List<Tuple<ObjectInfo, Event>>();

            lock (_lockObject)
            {
                foreach (var file in storage.GetQueueFiles(queueName, batchSize * 5, maxCreatedDate))
                {
                    if (!storage.LockFile(file))
                        continue;

                    try
                    {
                        storage.IncrementAttempts(file);
                    }
                    catch { }

                    try
                    {
                        var ev = storage.GetObject<Event>(file.Path);
                        events.Add(Tuple.Create(file, ev));
                        if (events.Count == batchSize)
                            break;

                    }
                    catch { }
                }

                return events;
            }
        }

        public static void DeleteFiles(this IObjectStorage storage, IEnumerable<ObjectInfo> files)
        {
            foreach (var file in files)
                storage.DeleteObject(file.Path);
        }

        public static void DeleteBatch(this IObjectStorage storage, IList<Tuple<ObjectInfo, Event>> batch)
        {
            foreach (var item in batch)
                storage.DeleteObject(item.Item1.Path);
        }

        public static void ReleaseBatch(this IObjectStorage storage, IList<Tuple<ObjectInfo, Event>> batch)
        {
            foreach (var item in batch)
                storage.ReleaseFile(item.Item1);
        }
    }
}
