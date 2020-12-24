using System;
using System.Linq;
using System.Reflection;
using Exceptionless.Models;
using Exceptionless.Plugins;

namespace Exceptionless {
    public static class ExceptionExtensions {
        /// <summary>
        /// Creates a builder object for constructing error reports in a fluent api.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="pluginContextData">
        /// Any contextual data objects to be used by Exceptionless plugins to gather default
        /// information for inclusion in the report information.
        /// </param>
        /// <param name="client">
        /// The ExceptionlessClient instance used for configuration. If a client is not specified, it will use
        /// ExceptionlessClient.Default.
        /// </param>
        /// <returns></returns>
        public static EventBuilder ToExceptionless(this Exception exception, ContextData pluginContextData = null, ExceptionlessClient client = null) {
            if (client == null)
                client = ExceptionlessClient.Default;

            if (pluginContextData == null)
                pluginContextData = new ContextData();

            pluginContextData.SetException(exception);
            return client.CreateEvent(pluginContextData).SetType(Event.KnownTypes.Error);
        }

        /// <summary>
        /// Creates a builder object for constructing error reports in a fluent api.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="client">
        /// The ExceptionlessClient instance used for configuration.
        /// </param>
        /// <returns></returns>
        public static EventBuilder ToExceptionless(this Exception exception, ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var pluginContextData = new ContextData();
            pluginContextData.SetException(exception);
            
            return client.CreateEvent(pluginContextData).SetType(Event.KnownTypes.Error);
        }
    }
}

namespace Exceptionless.Extensions {
    public static class ExceptionExtensions {
        public static Exception GetInnermostException(this Exception exception) {
            if (exception == null)
                return null;

            Exception current = exception;
            while (current.InnerException != null)
                current = current.InnerException;

            return current;
        }

        public static string GetMessage(this Exception exception) {
            if (exception == null)
                return String.Empty;

            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
                return String.Join(Environment.NewLine, aggregateException.Flatten().InnerExceptions.Where(ex => !String.IsNullOrEmpty(ex.GetInnermostException().Message)).Select(ex => ex.GetInnermostException().Message));

            return exception.GetInnermostException().Message;
        }

        private static readonly string _marker = "@exceptionless";
        public static void MarkProcessed(this Exception exception) {
            if (exception == null)
                return;

            try {
                if (exception.Data != null) {
                    var genericTypes = exception.Data.GetType().GetGenericArguments();
                    if (genericTypes.Length == 0 || genericTypes[0].GetTypeInfo().IsAssignableFrom(typeof(string).GetTypeInfo()))
                        exception.Data[_marker] = genericTypes.Length > 0 ? genericTypes[1].GetDefaultValue() : null;
                }
            } catch (Exception) {}

            MarkProcessed(exception.InnerException);
        }

        public static bool IsProcessed(this Exception exception) {
            if (exception == null)
                return false;

            try {
                if (exception.Data != null && exception.Data.Contains(_marker))
                    return true;
            } catch (Exception) {}

            return IsProcessed(exception.InnerException);
        }
    }
}