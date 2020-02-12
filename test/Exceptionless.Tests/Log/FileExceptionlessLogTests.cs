using System;
using System.IO;
using System.Threading.Tasks;
using Exceptionless.Logging;
using Xunit;

namespace Exceptionless.Tests.Log {
    public class FileExceptionlessLogTests : FileExceptionlessLogTestBase {
        [Fact]
        public override void CanWriteToLogFile() {
            base.CanWriteToLogFile();
        }

        [Fact]
        public override void CheckSizeDoesNotFailIfLogIsMissing() {
            base.CheckSizeDoesNotFailIfLogIsMissing();
        }

        [Fact]
        public override Task LogFlushTimerWorks() {
            return base.LogFlushTimerWorks();
        }

        [Fact]
        public override void LogIsThreadSafe() {
            base.LogIsThreadSafe();
        }

        [Fact]
        public override void LogResetsAfter5mb() {
            base.LogResetsAfter5mb();
        }

        protected override FileExceptionlessLog GetLog(string filePath) {
            return new FileExceptionlessLog(filePath);
        }

        protected override bool LogExists(string path = LOG_FILE) {
            return File.Exists(Path.GetFullPath(path));
        }

        protected override void DeleteLog(string path = LOG_FILE) {
            if (LogExists(path))
                File.Delete(Path.GetFullPath(path));
        }
    }
}