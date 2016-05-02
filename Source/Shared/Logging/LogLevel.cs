using System;

namespace Exceptionless.Logging {
    public sealed class LogLevel : IComparable, IEquatable<LogLevel> {
        public static readonly LogLevel Trace = new LogLevel("Trace", 0);
        public static readonly LogLevel Debug = new LogLevel("Debug", 1);
        public static readonly LogLevel Info = new LogLevel("Info", 2);
        public static readonly LogLevel Warn = new LogLevel("Warn", 3);
        public static readonly LogLevel Error = new LogLevel("Error", 4);
        public static readonly LogLevel Fatal = new LogLevel("Fatal", 5);
        public static readonly LogLevel Off = new LogLevel("Off", 6);

        private readonly int _ordinal;
        private readonly string _name;

        private LogLevel(string name, int ordinal) {
            _name = name;
            _ordinal = ordinal;
        }

        public string Name {
            get { return this._name; }
        }

        internal static LogLevel MaxLevel {
            get { return LogLevel.Fatal; }
        }

        internal static LogLevel MinLevel {
            get { return LogLevel.Trace; }
        }

        public int Ordinal {
            get { return _ordinal; }
        }

        public static bool operator ==(LogLevel level1, LogLevel level2) {
            if (Object.ReferenceEquals(level1, null))
                return Object.ReferenceEquals(level2, null);
            if (Object.ReferenceEquals(level2, null))
                return false;

            return level1.Ordinal == level2.Ordinal;
        }

        public static bool operator !=(LogLevel level1, LogLevel level2) {
            if (Object.ReferenceEquals(level1, null))
                return !Object.ReferenceEquals(level2, null);
            if (Object.ReferenceEquals(level2, null))
                return true;

            return level1.Ordinal != level2.Ordinal;
        }

        public static bool operator >(LogLevel level1, LogLevel level2) {
            if (level1 == null)
                throw new ArgumentNullException("level1");
            if (level2 == null)
                throw new ArgumentNullException("level2");

            return level1.Ordinal > level2.Ordinal;
        }

        public static bool operator >=(LogLevel level1, LogLevel level2) {
            if (level1 == null)
                throw new ArgumentNullException("level1");
            if (level2 == null)
                throw new ArgumentNullException("level2");

            return level1.Ordinal >= level2.Ordinal;
        }

        public static bool operator <(LogLevel level1, LogLevel level2) {
            if (level1 == null)
                throw new ArgumentNullException("level1");
            if (level2 == null)
                throw new ArgumentNullException("level2");

            return level1.Ordinal < level2.Ordinal;
        }

        public static bool operator <=(LogLevel level1, LogLevel level2) {
            if (level1 == null)
                throw new ArgumentNullException("level1");
            if (level2 == null)
                throw new ArgumentNullException("level2");

            return level1.Ordinal <= level2.Ordinal;
        }

        public static LogLevel FromOrdinal(int ordinal) {
            switch (ordinal) {
                case 0:
                    return Trace;
                case 1:
                    return Debug;
                case 2:
                    return Info;
                case 3:
                    return Warn;
                case 4:
                    return Error;
                case 5:
                    return Fatal;
                case 6:
                    return Off;
                default:
                    throw new ArgumentException("Invalid ordinal.");
            }
        }

        public static LogLevel FromString(string levelName) {
            if (levelName == null)
                throw new ArgumentNullException("levelName");
            if (levelName.Equals("Trace", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Trace;
            if (levelName.Equals("Debug", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Debug;
            if (levelName.Equals("Info", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Info;
            if (levelName.Equals("Warn", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Warn;
            if (levelName.Equals("Error", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Error;
            if (levelName.Equals("Fatal", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Fatal;
            if (levelName.Equals("Off", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Off;

            throw new ArgumentException("Unknown log level: " + levelName);
        }

        public override string ToString() {
            return Name;
        }

        public override int GetHashCode() {
            return Ordinal;
        }

        public override bool Equals(object obj) {
            LogLevel logLevel = obj as LogLevel;
            if (logLevel == null)
                return false;

            return Ordinal == logLevel.Ordinal;
        }

        public bool Equals(LogLevel other) {
            if (!(other == (LogLevel)null))
                return Ordinal == other.Ordinal;

            return false;
        }

        public int CompareTo(object obj) {
            if (obj == null)
                throw new ArgumentNullException("obj");

            return Ordinal - ((LogLevel)obj).Ordinal;
        }
    }
}
