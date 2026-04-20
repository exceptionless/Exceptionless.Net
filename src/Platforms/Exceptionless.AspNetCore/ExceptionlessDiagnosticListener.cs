using System;
using System.Collections.Generic;
using System.Reflection;
using Exceptionless.Plugins;
using Microsoft.AspNetCore.Http;

namespace Exceptionless.AspNetCore {
    public sealed class ExceptionlessDiagnosticListener : IObserver<KeyValuePair<string, object>> {
        private const string HandledExceptionEvent = "Microsoft.AspNetCore.Diagnostics.HandledException";
        private const string DiagnosticsUnhandledExceptionEvent = "Microsoft.AspNetCore.Diagnostics.UnhandledException";
        private const string HostingUnhandledExceptionEvent = "Microsoft.AspNetCore.Hosting.UnhandledException";
        private const string HostingDiagnosticsUnhandledExceptionEvent = "Microsoft.AspNetCore.Hosting.Diagnostics.UnhandledException";
        private const string MiddlewareExceptionEvent = "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException";
        private readonly ExceptionlessClient _client;

        public ExceptionlessDiagnosticListener(ExceptionlessClient client) {
            _client = client;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        internal static bool IsRelevantEvent(string eventName) {
            return String.Equals(eventName, HandledExceptionEvent, StringComparison.Ordinal) ||
                   String.Equals(eventName, DiagnosticsUnhandledExceptionEvent, StringComparison.Ordinal) ||
                   String.Equals(eventName, HostingUnhandledExceptionEvent, StringComparison.Ordinal) ||
                   String.Equals(eventName, HostingDiagnosticsUnhandledExceptionEvent, StringComparison.Ordinal) ||
                   String.Equals(eventName, MiddlewareExceptionEvent, StringComparison.Ordinal);
        }

        public void OnNext(KeyValuePair<string, object> diagnosticEvent) {
            switch (diagnosticEvent.Key) {
                case HandledExceptionEvent:
                    SubmitException(diagnosticEvent.Value, diagnosticEvent.Key, false);
                    break;
                case DiagnosticsUnhandledExceptionEvent:
                case HostingUnhandledExceptionEvent:
                case HostingDiagnosticsUnhandledExceptionEvent:
                    SubmitException(diagnosticEvent.Value, diagnosticEvent.Key, true);
                    break;
                case MiddlewareExceptionEvent:
                    if (diagnosticEvent.Value is null)
                        break;

                    string middlewareName = GetPropertyValue(diagnosticEvent.Value, "name") as string;
                    SubmitException(diagnosticEvent.Value, middlewareName ?? diagnosticEvent.Key, true);
                    break;
            }
        }

        private void SubmitException(object payload, string submissionMethod, bool isUnhandledError) {
            if (payload is null)
                return;

            var httpContext = GetPropertyValue(payload, "httpContext") as HttpContext;
            var exception = GetPropertyValue(payload, "exception") as Exception;
            if (httpContext == null || exception == null)
                return;

            // Skip if this exception was already submitted for this request by ExceptionlessExceptionHandler.
            if (httpContext.Items.TryGetValue(ExceptionlessExceptionHandler.HttpContextSubmittedKey, out var submitted)
                && ReferenceEquals(submitted, exception))
                return;

            var contextData = new ContextData();
            if (isUnhandledError)
                contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod(submissionMethod);

            exception.ToExceptionless(contextData, _client).SetHttpContext(httpContext).Submit();
        }

        private static object GetPropertyValue(object payload, string propertyName) {
            if (payload is null)
                return null;

            try {
                return payload.GetType()
                    .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase)?
                    .GetValue(payload);
            } catch (Exception ex) when (ex is TargetInvocationException or AmbiguousMatchException or TargetException or MethodAccessException or ArgumentException) {
                return null;
            }
        }
    }
}
