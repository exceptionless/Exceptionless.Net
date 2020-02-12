using System;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Logging;
using Xunit;

namespace Exceptionless.Tests.Log {
    public abstract class FileExceptionlessLogTestBase {
        protected const string LOG_FILE = "test.log";

        public virtual void CanWriteToLogFile() {
            DeleteLog();

            using (var log = GetLog(LOG_FILE)) {
                log.Info("Test");
                log.Flush();

                Assert.True(LogExists(log.FilePath));
                string contents = log.GetFileContents();

                Assert.Contains(" Info  Test", contents);
            }
        }

        public virtual async Task LogFlushTimerWorks() {
            DeleteLog();

            using (var log = GetLog(LOG_FILE)) {
                log.Info("Test");

                string contents = log.GetFileContents();
                Assert.Equal("", contents);

                await Task.Delay(TimeSpan.FromMilliseconds(3100));

                Assert.True(LogExists(log.FilePath));
                contents = log.GetFileContents();

                Assert.Contains(" Info  Test", contents);
            }
        }

        public virtual void LogResetsAfter5mb() {
            DeleteLog();

            using (var log = GetLog(LOG_FILE)) {
                // write 3mb of content to the log
                for (int i = 0; i < 1024 * 3; i++)
                    log.Info(new string('0', 1024));

                log.Flush();
                Assert.True(log.GetFileSize() > 1024 * 1024 * 3);

                // force a check file size call
                log.CheckFileSize();

                // make sure it didn't clear the log
                Assert.True(log.GetFileSize() > 1024 * 1024 * 3);

                // write another 3mb of content to the log
                for (int i = 0; i < 1024 * 3; i++)
                    log.Info(new string('0', 1024));

                log.Flush();
                // force a check file size call
                log.CheckFileSize();

                // make sure it cleared the log
                long size = log.GetFileSize();

                // should be 99 lines of text in the file
                Assert.True(size > 1024 * 99);
            }
        }

        public virtual void CheckSizeDoesNotFailIfLogIsMissing() {
            using (var log = GetLog(LOG_FILE + ".doesnotexist")) {
                log.CheckFileSize();
            }
        }

        public virtual void LogIsThreadSafe() {
            DeleteLog();

            using (var log = GetLog(LOG_FILE)) {
                // write 3mb of content to the log in multiple threads
                Parallel.For(0, 1024 * 3, i => log.Info(new string('0', 1024)));

                log.Flush();
                Assert.True(log.GetFileSize() > 1024 * 1024 * 3);

                // force a check file size call
                log.CheckFileSize();

                // make sure it didn't clear the log
                Assert.True(log.GetFileSize() > 1024 * 1024 * 3);

                // write another 3mb of content to the log
                Parallel.For(0, 1024 * 3, i => log.Info(new string('0', 1024)));
                log.Flush();

                long size = log.GetFileSize();
                Console.WriteLine("File: " + size);

                // do the check size while writing to the log from multiple threads
                Parallel.Invoke(() => Parallel.For(0, 1024 * 3, i => log.Info(new string('0', 1024))), () => {
                                    Thread.Sleep(10);
                                    log.CheckFileSize();
                                });

                // should be more than 99 lines of text in the file
                size = log.GetFileSize();
                Console.WriteLine("File: " + size);
                Assert.True(size > 1024 * 99);
            }
        }

        protected abstract FileExceptionlessLog GetLog(string filePath);

        protected abstract bool LogExists(string path = LOG_FILE);

        protected abstract void DeleteLog(string path = LOG_FILE);
    }
}