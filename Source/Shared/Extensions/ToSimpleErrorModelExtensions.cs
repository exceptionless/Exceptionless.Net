using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Logging;
using Exceptionless.Models.Data;

namespace Exceptionless.Extensions {
    internal static class ToSimpleErrorModelExtensions {
        private static readonly string[] _exceptionExclusions = {
            "HelpLink", "ExceptionContext", "InnerExceptions", "InnerException", "Errors", "Types",
            "Message", "Source", "StackTrace", "TargetSite", "HResult", 
            "Entries", "StateEntries",  "PersistedState", "Results"
        };

        /// <summary>
        /// Sets the properties from an exception.
        /// </summary>
        /// <param name="exception">The exception to populate properties from.</param>
        /// <param name="log">The log implementation used for diagnostic information.</param>
        /// <param name="dataExclusions">Data exclusions that get run on extra exception properties.</param>
        public static SimpleError ToSimpleErrorModel(this Exception exception, IExceptionlessLog log, IEnumerable<string> dataExclusions) {
            Type type = exception.GetType();

            var error = new SimpleError {
                Message = GetMessage(exception),
                Type = type.FullName,
                StackTrace = exception.StackTrace
            };

            try {
                var exclusions = _exceptionExclusions.Union(dataExclusions ?? new List<string>());
                var extraProperties = type.GetPublicProperties().Where(p => !exclusions.Contains(p.Name)).ToDictionary(p => p.Name, p => {
                    try {
                        return p.GetValue(exception, null);
                    } catch {}
                    return null;
                });

                extraProperties = extraProperties.Where(kvp => !ValueIsEmpty(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (extraProperties.Count > 0 && !error.Data.ContainsKey(SimpleError.KnownDataKeys.ExtraProperties)) {
                    error.AddObject(new ExtendedDataInfo {
                        Data = extraProperties,
                        Name = SimpleError.KnownDataKeys.ExtraProperties,
                        IgnoreSerializationErrors = true,
                        MaxDepthToSerialize = 5
                    });
                }
            } catch {}

            if (exception.InnerException != null)
                error.Inner = exception.InnerException.ToSimpleErrorModel(log, dataExclusions);

            return error;
        }

        private static bool ValueIsEmpty(object value) {
            if (value == null)
                return true;

            if (value is IEnumerable) {
                if (!(value as IEnumerable).Cast<Object>().Any())
                    return true;
            }

            return false;
        }

        private static string GetMessage(Exception exception) {
            string defaultMessage = String.Format("Exception of type '{0}' was thrown.", exception.GetType().FullName);
            string message = !String.IsNullOrEmpty(exception.Message) ? String.Join(" ", exception.Message.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Trim() : null;

            return !String.IsNullOrEmpty(message) ? message : defaultMessage;
        }
    }
}