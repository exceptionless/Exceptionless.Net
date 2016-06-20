using System;
using Exceptionless.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Exceptionless.AspNetCore {
    internal sealed class ExceptionlessDiagnosticListener {
        private readonly ExceptionlessClient _client;
        public ExceptionlessDiagnosticListener(ExceptionlessClient client) {
            _client = client;
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
        public void OnDiagnosticHandledException(HttpContext context, Exception ex) {
            var contextData = new ContextData();
            contextData.SetSubmissionMethod("Microsoft.AspNetCore.Diagnostics.UnhandledException");
            contextData.Add(nameof(HttpContext), context);

            ex.ToExceptionless(contextData, _client).Submit();
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void OnDiagnosticUnhandledException(HttpContext context, Exception ex) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("Microsoft.AspNetCore.Diagnostics.HandledException");
            contextData.Add(nameof(HttpContext), context);

            ex.ToExceptionless(contextData, _client).Submit();
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void OnHostingUnhandledException(HttpContext context, Exception ex) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("Microsoft.AspNetCore.Hosting.UnhandledException");
            contextData.Add(nameof(HttpContext), context);

            ex.ToExceptionless(contextData, _client).Submit();
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException")]
        public void OnMiddlewareException(Exception ex, string name) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod(name);

            ex.ToExceptionless(contextData, _client).Submit();
        }
    }
}