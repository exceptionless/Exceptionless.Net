using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Exceptionless.Utility;

namespace Exceptionless.Storage {
    internal sealed class LockFile : IDisposable {
        private static readonly ConcurrentDictionary<string, bool> _lockStatus = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="LockFile"/> class.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="defaultTimeOutInSeconds"></param>
        public LockFile(string path, int defaultTimeOutInSeconds = 30) {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            FileName = path;
            DefaultTimeOutInSeconds = defaultTimeOutInSeconds;
        }

        /// <summary>
        /// Acquires a lock while waiting with the default timeout value.
        /// </summary>
        public void AcquireLock() {
            AcquireLock(TimeSpan.FromSeconds(DefaultTimeOutInSeconds));
        }

        /// <summary>
        /// Acquires a lock in a specific amount of time.
        /// </summary>
        /// <param name="timeout">The time to wait for when trying to acquire a lock.</param>
        public void AcquireLock(TimeSpan timeout) {
            CreateLock(FileName, timeout);
        }

        /// <summary>
        /// Acquires a lock in a specific amount of time.
        /// </summary>
        /// <param name="path">The path to acquire a lock on.</param>
        /// <param name="timeout">The time to wait for when trying to acquire a lock.</param>
        /// <returns>A lock instance.</returns>
        public static LockFile Acquire(string path, TimeSpan? timeout = null) {
            var lockInstance = new LockFile(path);
            lockInstance.AcquireLock(timeout ?? TimeSpan.FromSeconds(lockInstance.DefaultTimeOutInSeconds));
            return lockInstance;
        }

        /// <summary>
        /// Creates a lock file.
        /// </summary>
        /// <param name="path">The place to create the lock file.</param>
        /// <param name="timeout">The amount of time to wait before a TimeoutException is thrown.</param>
        private void CreateLock(string path, TimeSpan timeout) {
            DateTime expire = DateTime.UtcNow.Add(timeout);

            Retry:
            while (File.Exists(path)) {
                if (expire < DateTime.UtcNow)
                    throw new TimeoutException($"The lock '{path}' timed out.");

                if (IsLockExpired(path)) {
                    Debug.WriteLine($"The lock '{path}' has expired on '{GetCreationTimeUtc(path)}' and wasn't cleaned up properly.");
                    ReleaseLock();
                } else {
                    Debug.WriteLine($"Waiting for lock: {path}");
                    Thread.Sleep(500);
                }
            }

            // create file
            try {
                using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    fs.Dispose();

                _lockStatus[path] = true;
            } catch (IOException) {
                Debug.WriteLine($"Error creating lock: {path}");
                goto Retry;
            }

            Debug.WriteLine(String.Format($"Created lock: {path}"));
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        public void ReleaseLock() {
            RemoveLock(FileName);
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        /// <param name="path">The path to the lock file.</param>
        private void RemoveLock(string path) {
            Run.WithRetries(() => {
                try {
                    DeleteFile(path);
                    _lockStatus[path] = false;
                    Debug.WriteLine($"Deleted lock: {path}");
                } catch (Exception) {
                    Debug.WriteLine($"Error deleting lock: {path}");
                    throw;
                }
            }, 5);
        }

        private void DeleteFile(string path) {
            Run.WithRetries(() => {
                try {
                    File.Delete(path);
#if !NETSTANDARD
                } catch (DriveNotFoundException) {
#endif
                } catch (DirectoryNotFoundException) {
                } catch (FileNotFoundException) { }
            });
        }

        private DateTime GetCreationTimeUtc(string path) {
            return Run.WithRetries(() => {
                try {
                    return File.GetCreationTimeUtc(path);
#if !NETSTANDARD
                } catch (DriveNotFoundException) {
                    return DateTime.MinValue;
#endif
                } catch (DirectoryNotFoundException) {
                    return DateTime.MinValue;
                } catch (FileNotFoundException) {
                    return DateTime.MinValue;
                }
            });
        }

        private bool IsLockExpired(string path) {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            return (!_lockStatus.ContainsKey(path) || !_lockStatus[path])
                && GetCreationTimeUtc(path) < DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(DefaultTimeOutInSeconds * 10));
        }

        /// <summary>
        /// The file that is being locked.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The default time to wait when trying to acquire a lock.
        /// </summary>
        public double DefaultTimeOutInSeconds { get; }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        public void Dispose() {
            ReleaseLock();
        }
    }
}