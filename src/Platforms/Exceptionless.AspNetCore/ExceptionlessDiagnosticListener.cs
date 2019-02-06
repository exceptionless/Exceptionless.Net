using System;
using Exceptionless.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Exceptionless.AspNetCore {
    public sealed class ExceptionlessDiagnosticListener {
        private readonly ExceptionlessClient _client;

        public ExceptionlessDiagnosticListener(ExceptionlessClient client) {
            _client = client;
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
        public void OnDiagnosticHandledException(HttpContext httpContext, Exception exception) {
            var contextData = new ContextData();
            contextData.SetSubmissionMethod("Microsoft.AspNetCore.Diagnostics.HandledException");

            exception.ToExceptionless(contextData, _client).SetHttpContext(httpContext).Submit();
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void OnDiagnosticUnhandledException(HttpContext httpContext, Exception exception) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("Microsoft.AspNetCore.Diagnostics.UnhandledException");

            exception.ToExceptionless(contextData, _client).SetHttpContext(httpContext).Submit();
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void OnHostingUnhandledException(HttpContext httpContext, Exception exception) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("Microsoft.AspNetCore.Hosting.UnhandledException");

            exception.ToExceptionless(contextData, _client).SetHttpContext(httpContext).Submit();
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException")]
        public void OnMiddlewareException(HttpContext httpContext, Exception exception, string name) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod(name ?? "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException");

            exception.ToExceptionless(contextData, _client).SetHttpContext(httpContext).Submit();
        }
    }
}