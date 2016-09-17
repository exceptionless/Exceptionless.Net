using System;

namespace Exceptionless.Logging {
    public static class ExceptionlessLogExtensions {
        public static void Error(this IExceptionlessLog log, Type source, string message) {
            log.Error(message, GetSourceName(source));
        }

        public static void Error(this IExceptionlessLog log, Type source, Exception exception, string message) {
            log.Error(message, GetSourceName(source), exception);
        }

        public static void FormattedError(this IExceptionlessLog log, Type source, Exception exception, string format, params object[] args) {
            log.Error(GetMessage(format, args), GetSourceName(source), exception);
        }

        public static void FormattedError(this IExceptionlessLog log, Type source, string format, params object[] args) {
            log.Error(GetMessage(format, args), GetSourceName(source));
        }

        public static void Info(this IExceptionlessLog log, Type source, string message) {
            log.Info(message, GetSourceName(source));
        }

        public static void FormattedInfo(this IExceptionlessLog log, Type source, string format, params object[] args) {
            log.Info(GetMessage(format, args), GetSourceName(source));
        }

        public static void Debug(this IExceptionlessLog log, Type source, string message) {
            log.Debug(message, GetSourceName(source));
        }

        public static void FormattedDebug(this IExceptionlessLog log, Type source, string format, params object[] args) {
            log.Debug(GetMessage(format, args), GetSourceName(source));
        }

        public static void Warn(this IExceptionlessLog log, Type source, string message) {
            log.Warn(message, GetSourceName(source));
        }

        public static void FormattedWarn(this IExceptionlessLog log, Type source, string format, params object[] args) {
            log.Warn(GetMessage(format, args), GetSourceName(source));
        }

        public static void Trace(this IExceptionlessLog log, Type source, string message) {
            log.Trace(message, GetSourceName(source));
        }

        public static void FormattedTrace(this IExceptionlessLog log, Type source, string format, params object[] args) {
            log.Trace(GetMessage(format, args), GetSourceName(source));
        }

        public static void Error(this IExceptionlessLog log, Exception exception, string message) {
            log.Error(message, exception: exception);
        }

        public static void FormattedError(this IExceptionlessLog log, Exception exception, string format, params object[] args) {
            log.Error(GetMessage(format, args), exception: exception);
        }

        public static void FormattedError(this IExceptionlessLog log, string format, params object[] args) {
            log.Error(GetMessage(format, args));
        }

        public static void FormattedInfo(this IExceptionlessLog log, string format, params object[] args) {
            log.Info(GetMessage(format, args));
        }

        public static void FormattedDebug(this IExceptionlessLog log, string format, params object[] args) {
            log.Debug(GetMessage(format, args));
        }

        public static void FormattedWarn(this IExceptionlessLog log, string format, params object[] args) {
            log.Warn(GetMessage(format, args));
        }

        private static string GetSourceName(Type type) {
            return type.Name;
        }

        private static string GetMessage(string format, params object[] args) {
            try {
                return String.Format(format, args);
            } catch (Exception) {
                return format;
            }
        }
    }
}