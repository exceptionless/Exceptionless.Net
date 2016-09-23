#if !PORTABLE && !NETSTANDARD1_2
using System;
using System.IO;

namespace Exceptionless.Storage {
    public sealed class LockFile : LockBase<LockFile> {
        /// <summary>
        /// Initializes a new instance of the <see cref="LockFile"/> class.
        /// </summary>
        /// <param name="fileName">The file.</param>
        public LockFile(string fileName) : base(fileName) {
            FileName = fileName;
        }

        public override string GetLockFilePath() {
            return FileName;
        }

        /// <summary>
        /// The file that is being locked.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Checks to see if the specific path is locked.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns true if the lock has expired or the lock exists.</returns>
        public static bool IsLocked(string path) {
            if (String.IsNullOrEmpty(path))
                return false;

            return !IsLockExpired(path) && File.Exists(path);
        }
    }
}
#endif