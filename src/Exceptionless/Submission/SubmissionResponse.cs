using System;
using System.Net;

namespace Exceptionless.Submission {
    public class SubmissionResponse {
        internal static SubmissionResponse Ok200 { get; } = new(200, "OK");
        internal static SubmissionResponse InvalidClientConfig500 { get; } = new(500, "Invalid client configuration settings");

        public SubmissionResponse(int statusCode, string message = null, Exception exception = null) {
            StatusCode = statusCode;
            Message = message;

            Exception = exception;
        }

        public bool Success => StatusCode >= 200 && StatusCode <= 299;
        public bool BadRequest => (HttpStatusCode)StatusCode == HttpStatusCode.BadRequest;
        public bool ServiceUnavailable => (HttpStatusCode)StatusCode == HttpStatusCode.ServiceUnavailable;
        public bool PaymentRequired => (HttpStatusCode)StatusCode == HttpStatusCode.PaymentRequired;
        public bool UnableToAuthenticate => (HttpStatusCode)StatusCode == HttpStatusCode.Unauthorized || (HttpStatusCode)StatusCode == HttpStatusCode.Forbidden;
        public bool NotFound => (HttpStatusCode)StatusCode == HttpStatusCode.NotFound;
        public bool RequestEntityTooLarge => (HttpStatusCode)StatusCode == HttpStatusCode.RequestEntityTooLarge;

        public int StatusCode { get; }
        public string Message { get; }

        public Exception Exception { get; }
    }
}