using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Models.Data;

namespace Exceptionless.Extensions {
    internal static class ToSimpleErrorModelExtensions {
        private static readonly string[] _exceptionExclusions = {
            "@exceptionless", "Data", "HelpLink", "ExceptionContext", "InnerExceptions", "InnerException", "Errors", "Types",
            "Message", "Source", "StackTrace", "TargetSite", "HResult", 
            "Entries", "StateEntries",  "PersistedState", "Results"
        };

        /// <summary>
        /// Sets the properties from an exception.
        /// </summary>
        /// <param name="exception">The exception to populate properties from.</param>
        /// <param name="client">
        /// The ExceptionlessClient instance used for configuration. If a client is not specified, it will use
        /// ExceptionlessClient.Default.
        /// </param>
        public static SimpleError ToSimpleErrorModel(this Exception exception, ExceptionlessClient client = null) {
            if (client == null)
                client = ExceptionlessClient.Default;

            return ToSimpleErrorModelInternal(exception, client);
        }

        private static SimpleError ToSimpleErrorModelInternal(this Exception exception, ExceptionlessClient client, bool isInner = false) {
            if (client == null)
                client = ExceptionlessClient.Default;

            var log = client.Configuration.Resolver.GetLog();
            Type type = exception.GetType();

            var error = new SimpleError {
                Message = GetMessage(exception),
                Type = type.FullName,
                StackTrace = exception.Demystify().StackTrace
            };

            if (!isInner)
                error.Modules = ToErrorModelExtensions.GetLoadedModules(log);

            var exclusions = _exceptionExclusions.Union(client.Configuration.DataExclusions).ToList();
            try {
                if (exception.Data != null) {
                    foreach (object k in exception.Data.Keys) {
                        string key = k != null ? k.ToString() : null;
                        if (String.IsNullOrEmpty(key) || key.AnyWildcardMatches(exclusions, true))
                            continue;

                        var item = exception.Data[k];
                        if (item == null)
                            continue;

                        error.Data[key] = item;
                    }
                }
            } catch (Exception ex) {
                log.Error(typeof(ExceptionlessClient), ex, "Error populating Data: " + ex.Message);
            }

            try {
                var extraProperties = type.GetPublicProperties().Where(p => !p.Name.AnyWildcardMatches(exclusions, true)).ToDictionary(p => p.Name, p => {
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
                    }, client);
                }
            } catch {}

            if (exception.InnerException != null)
                error.Inner = exception.InnerException.ToSimpleErrorModel(client);

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
            string message = !String.IsNullOrEmpty(exception.Message) ? exception.Message.Trim() : null;

            return !String.IsNullOrEmpty(message) ? message : defaultMessage;
        }
    }
}