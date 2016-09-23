#if !PORTABLE && !NETSTANDARD1_2
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Exceptionless.Utility;

namespace Exceptionless.Storage {
    public abstract class LockBase<T> : IDisposable where T : LockBase<T> {
        private static readonly ConcurrentDictionary<string, bool> _lockStatus = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Ensures that the derived classes always have a string parameter to pass in a path.
        /// </summary>
        /// <param name="path"></param>
        protected LockBase(string path) {}

        /// <summary>
        /// Acquires a lock while waiting with the default timeout value.
        /// </summary>
        public virtual void AcquireLock() {
            AcquireLock(TimeSpan.FromSeconds(DefaultTimeOutInSeconds));
        }

        /// <summary>
        /// Acquires a lock in a specific amount of time.
        /// </summary>
        /// <param name="timeout">The time to wait for when trying to acquire a lock.</param>
        public void AcquireLock(TimeSpan timeout) {
            CreateLock(GetLockFilePath(), timeout);
        }

        /// <summary>
        /// Acquires a lock while waiting with the default timeout value.
        /// </summary>
        /// <param name="path">The path to acquire a lock on.</param>
        /// <returns>A lock instance.</returns>
        public static T Acquire(string path) {
            return Acquire(path, TimeSpan.FromSeconds(DefaultTimeOutInSeconds));
        }

        /// <summary>
        /// Acquires a lock in a specific amount of time.
        /// </summary>
        /// <param name="path">The path to acquire a lock on.</param>
        /// <param name="timeout">The time to wait for when trying to acquire a lock.</param>
        /// <returns>A lock instance.</returns>
        public static T Acquire(string path, TimeSpan timeout) {
            var lockInstance = Activator.CreateInstance(typeof(T), path) as T;
            if (lockInstance == null)
                throw new Exception("Unable to create locking instance.");

            lockInstance.AcquireLock(timeout);
            return lockInstance;
        }

        /// <summary>
        /// Creates a lock file.
        /// </summary>
        /// <param name="path">The place to create the lock file.</param>
        /// <param name="timeout">The amount of time to wait before a TimeoutException is thrown.</param>
        protected virtual void CreateLock(string path, TimeSpan timeout) {
            DateTime expire = DateTime.UtcNow.Add(timeout);

            Retry:
            while (File.Exists(path)) {
                if (expire < DateTime.UtcNow)
                    throw new TimeoutException($"The lock '{path}' timed out.");

                if (IsLockExpired(path)) {
                    Debug.WriteLine($"The lock '{path}' has expired on '{File.GetCreationTimeUtc(path)}' and wasn't cleaned up properly.");
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
        public virtual void ReleaseLock() {
            RemoveLock(GetLockFilePath());
        }

        public abstract string GetLockFilePath();

        /// <summary>
        /// Releases the lock.
        /// </summary>
        /// <param name="path">The path to the lock file.</param>
        protected virtual void RemoveLock(string path) {
            Run.WithRetries(() => {
                try {
                    if (!File.Exists(path))
                        return;

                    File.Delete(path);
                    _lockStatus[path] = false;

                    Debug.WriteLine($"Deleted lock: {path}");
                } catch (IOException) {
                    Debug.WriteLine($"Error creating lock: {path}");
                    throw;
                }
            }, 5);
        }

        /// <summary>
        /// The default time to wait when trying to acquire a lock.
        /// </summary>
        protected static double DefaultTimeOutInSeconds { get; set; } = 30;

        protected static bool IsLockExpired(string path) {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            return (!_lockStatus.ContainsKey(path) || !_lockStatus[path]) 
                && File.GetCreationTimeUtc(path) < DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(DefaultTimeOutInSeconds * 10));
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        public void Dispose() {
            ReleaseLock();
        }
    }
}
#endif