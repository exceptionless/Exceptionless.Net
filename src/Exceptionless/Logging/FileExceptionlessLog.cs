using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security;
#if NET45
using System.Security.AccessControl;
using System.Security.Principal;
#endif
using System.Text;
using System.Threading;
using Exceptionless.Extensions;
using Exceptionless.Utility;

namespace Exceptionless.Logging {
    public class FileExceptionlessLog : IExceptionlessLog, IDisposable {
        private Mutex _flushMutex;
        private Timer _flushTimer;
        private readonly bool _append;
        private bool _firstWrite = true;
        private bool _isFlushing;
        private bool _isCheckingFileSize;

        public FileExceptionlessLog(string filePath, bool append = false) {
            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            FilePath = filePath;
            MinimumLogLevel = LogLevel.Trace;
            _append = append;

            Initialize();

            _flushMutex = CreateSystemFileMutex(FilePath);
            // flush the log every 3 seconds instead of on every write
            _flushTimer = new Timer(OnFlushTimer, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        }

        protected virtual void Initialize() {
            if (!Path.IsPathRooted(FilePath))
                FilePath = Path.GetFullPath(FilePath);

            string dir = Path.GetDirectoryName(FilePath);
            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        protected virtual WrappedDisposable<StreamWriter> GetWriter(bool append = false) {
#if NETSTANDARD
            return new WrappedDisposable<StreamWriter>(new StreamWriter(
                new FileStream(FilePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8)
            );
#else
            return new WrappedDisposable<StreamWriter>(new StreamWriter(FilePath, append, Encoding.UTF8));
#endif
        }

        protected virtual WrappedDisposable<Stream> GetReader() {
            return new WrappedDisposable<Stream>(new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        protected internal virtual string GetFileContents() {
            try {
                if (File.Exists(FilePath))
                    return File.ReadAllText(FilePath);
            } catch (IOException ex) {
                System.Diagnostics.Trace.WriteLine("Exceptionless: Error getting size of file: {0}", ex.ToString());
            }

            return String.Empty;
        }

        protected internal virtual long GetFileSize() {
            try {
                if (File.Exists(FilePath))
                    return new FileInfo(FilePath).Length;
            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine("Exceptionless: Error getting size of file: {0}", ex.ToString());
            }

            return -1;
        }

        public string FilePath { get; private set; }

        public LogLevel MinimumLogLevel { get; set; }

        public void Error(string message, string source = null, Exception exception = null) {
            if (source != null)
                WriteEntry(LogLevel.Error, String.Concat(source, ": ", message));
            else
                WriteEntry(LogLevel.Error, message);

            if (exception != null)
                WriteEntry(LogLevel.Error, exception.ToString());
        }

        public void Info(string message, string source = null) {
            if (source != null)
                WriteEntry(LogLevel.Info, String.Concat(source, ": ", message));
            else
                WriteEntry(LogLevel.Info, message);
        }

        public void Debug(string message, string source = null) {
            if (source != null)
                WriteEntry(LogLevel.Debug, String.Concat(source, ": ", message));
            else
                WriteEntry(LogLevel.Debug, message);
        }

        public void Warn(string message, string source = null) {
            if (source != null)
                WriteEntry(LogLevel.Warn, String.Concat(source, ": ", message));
            else
                WriteEntry(LogLevel.Warn, message);
        }

        public void Trace(string message, string source = null) {
            if (source != null)
                WriteEntry(LogLevel.Trace, String.Concat(source, ": ", message));
            else
                WriteEntry(LogLevel.Trace, message);
        }

        public void Flush() {
            if (_isFlushing || _buffer.IsEmpty)
                return;

            // Only check if appending file and size hasn't been checked in 2 minutes
            if ((_append || !_firstWrite) && DateTime.UtcNow.Subtract(_lastSizeCheck).TotalSeconds > 120)
                CheckFileSize();

            if (_isFlushing || _buffer.IsEmpty)
                return;

            bool hasFlushLock = false;
            try {
                _isFlushing = true;

                Run.WithRetries(() => {
                    if (!_flushMutex.WaitOne(TimeSpan.FromSeconds(5)))
                        return;

                    hasFlushLock = true;

                    bool append = _append || !_firstWrite;
                    _firstWrite = false;

                    try {
                        using (var writer = GetWriter(append)) {
                            while (_buffer.TryDequeue(out var entry)) {
                                if (entry != null && entry.LogLevel >= MinimumLogLevel)
                                    writer.Value.WriteLine($"{FormatLongDate(entry.Timestamp)} {entry.LogLevel.ToString().PadRight(5)} {entry.Message}");
                            }
                        }
                    } catch (Exception ex) {
                        System.Diagnostics.Trace.TraceError("Unable flush the logs. " + ex.Message);
                        while (_buffer.TryDequeue(out var entry)) {
                            System.Diagnostics.Trace.WriteLine(entry);
                        }
                    }
                });
            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine("Exceptionless: Error flushing log contents to disk: {0}", ex.ToString());
            } finally {
                if (hasFlushLock)
                    _flushMutex.ReleaseMutex();
                _isFlushing = false;
            }
        }

        private string FormatLongDate(DateTime timestamp) {
            var builder = new StringBuilder();
            Append4DigitsZeroPadded(timestamp.Year);
            builder.Append('-');
            Append2DigitsZeroPadded(timestamp.Month);
            builder.Append('-');
            Append2DigitsZeroPadded(timestamp.Day);
            builder.Append(' ');
            Append2DigitsZeroPadded(timestamp.Hour);
            builder.Append(':');
            Append2DigitsZeroPadded(timestamp.Minute);
            builder.Append(':');
            Append2DigitsZeroPadded(timestamp.Second);
            builder.Append('.');
            Append4DigitsZeroPadded((int)(timestamp.Ticks % 10000000) / 1000);
            return builder.ToString();

            void Append4DigitsZeroPadded(int number) {
                builder.Append((char)(number / 1000 % 10 + 0x30));
                builder.Append((char)(number / 100 % 10 + 0x30));
                builder.Append((char)(number / 10 % 10 + 0x30));
                builder.Append((char)(number / 1 % 10 + 0x30));
            }

            void Append2DigitsZeroPadded(int number) {
                builder.Append((char)(number / 10 + 0x30));
                builder.Append((char)(number % 10 + 0x30));
            }
        }

        private readonly ConcurrentQueue<LogEntry> _buffer = new ConcurrentQueue<LogEntry>();
        private void WriteEntry(LogLevel level, string entry) {
            _buffer.Enqueue(new LogEntry(level, entry));
        }

        private DateTime _lastSizeCheck = DateTime.UtcNow;
        protected const long FIVE_MB = 5 * 1024 * 1024;

        internal void CheckFileSize() {
            if (_isCheckingFileSize)
                return;

            _isCheckingFileSize = true;
            _lastSizeCheck = DateTime.UtcNow;

            if (GetFileSize() <= FIVE_MB) {
                _isCheckingFileSize = false;
                return;
            }

            // get the last X lines from the current file
            string lastLines = String.Empty;
            try {
                Run.WithRetries(() => {
                    if (!_flushMutex.WaitOne(TimeSpan.FromSeconds(5)))
                        return;

                    lastLines = GetLastLinesFromFile();
                    _flushMutex.ReleaseMutex();
                });
            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine("Exceptionless: Error getting last X lines from the log file: {0}", ex.ToString());
            }

            if (String.IsNullOrEmpty(lastLines)) {
                _isCheckingFileSize = false;
                return;
            }

            // overwrite the log file and initialize it with the last X lines it had
            try {
                Run.WithRetries(() => {
                    if (!_flushMutex.WaitOne(TimeSpan.FromSeconds(5)))
                        return;

                    using (var writer = GetWriter(true))
                        writer.Value.Write(lastLines);

                    _flushMutex.ReleaseMutex();
                });
            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine("Exceptionless: Error rewriting the log file after trimming it: {0}", ex.ToString());
            }

            _isCheckingFileSize = false;
        }

        private void OnFlushTimer(object state) {
            Flush();
        }

        public virtual void Dispose() {
            if (_flushTimer != null) {
                _flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _flushTimer.Dispose();
                _flushTimer = null;
            }

            Flush();

            if (_flushMutex != null) {
                _flushMutex.Close();
                _flushMutex = null;
            }
        }

        protected string GetLastLinesFromFile(int lines = 100) {
            byte[] buffer = Encoding.ASCII.GetBytes("\n");

            using (var fs = GetReader()) {
                long lineCount = 0;
                long endPosition = fs.Value.Length;

                for (long position = 1; position < endPosition; position++) {
                    fs.Value.Seek(-position, SeekOrigin.End);
                    fs.Value.Read(buffer, 0, 1);

                    if (buffer[0] != '\n')
                        continue;

                    lineCount++;
                    if (lineCount != lines)
                        continue;

                    var returnBuffer = new byte[fs.Value.Length - fs.Value.Position];
                    fs.Value.Read(returnBuffer, 0, returnBuffer.Length);

                    return Encoding.ASCII.GetString(returnBuffer);
                }

                // handle case where number of lines in file is less than desired line count
                fs.Value.Seek(0, SeekOrigin.Begin);
                buffer = new byte[fs.Value.Length];
                fs.Value.Read(buffer, 0, buffer.Length);

                return Encoding.ASCII.GetString(buffer);
            }
        }

        /// <summary>
        /// Allows file based locking across processes
        /// </summary>
        private Mutex CreateSystemFileMutex(string fileNameOrPath) {
#if NET45
            var security = new MutexSecurity();
            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            security.AddAccessRule(allowEveryoneRule);

            string name = GetFileBasedMutexName(fileNameOrPath);

            try {
                return new Mutex(false, name, out bool _, security);
            } catch (Exception ex) {
                if (ex is SecurityException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is NotImplementedException) {
                    System.Diagnostics.Trace.WriteLine("Exceptionless: Error creating global mutex falling back to previous implementation: {0}", ex.ToString());
                    return new Mutex(false, nameof(FileExceptionlessLog));
                }

                System.Diagnostics.Trace.WriteLine("Exceptionless: Error creating global mutex: {0}", ex.ToString());
                throw;
            }
#else
            System.Diagnostics.Trace.WriteLine("Exceptionless: This platform does not support taking out a global mutex");
            return new Mutex(false, nameof(FileExceptionlessLog));
#endif
        }

        private string GetFileBasedMutexName(string fileNameOrPath) {
            const string prefix = "Global\\Exceptionless-Log-";
            string name = fileNameOrPath.ToLowerInvariant().Replace('\\', '_').Replace('/', '_');

            const int maxLength = 260;
            if ((prefix.Length + name.Length) <= maxLength) {
                return $"{prefix}{name}";
            }
            
            // If name exceeds max length hash it and append part of the file name.
            string hash = name.ToSHA256();
            int startIndex = name.Length - (maxLength - prefix.Length - hash.Length);
            return $"{prefix}{hash}{name.Substring(startIndex)}";
        }

        private class LogEntry {
            public LogEntry(LogLevel level, string message) {
                LogLevel = level;
                Message = message;
                Timestamp = DateTime.Now;
            }

            public DateTime Timestamp { get; set; }
            public LogLevel LogLevel { get; set; }
            public string Message { get; set; }
        }
    }
}