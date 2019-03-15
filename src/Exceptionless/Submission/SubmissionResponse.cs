using System;
using System.Net;

namespace Exceptionless.Submission {
    public class SubmissionResponse {
        public SubmissionResponse(int statusCode, string message = null, Exception exception = null) {
            StatusCode = statusCode;
            Message = message;

            Success = statusCode >= 200 && statusCode <= 299;
            BadRequest = (HttpStatusCode)statusCode == HttpStatusCode.BadRequest;
            ServiceUnavailable = (HttpStatusCode)statusCode == HttpStatusCode.ServiceUnavailable;
            PaymentRequired = (HttpStatusCode)statusCode == HttpStatusCode.PaymentRequired;
            UnableToAuthenticate = (HttpStatusCode)statusCode == HttpStatusCode.Unauthorized || (HttpStatusCode)statusCode == HttpStatusCode.Forbidden;
            NotFound = (HttpStatusCode)statusCode == HttpStatusCode.NotFound;
            RequestEntityTooLarge = (HttpStatusCode)statusCode == HttpStatusCode.RequestEntityTooLarge;
            Exception = exception;
        }

        public bool Success { get; private set; }
        public bool BadRequest { get; private set; }
        public bool ServiceUnavailable { get; private set; }
        public bool PaymentRequired { get; private set; }
        public bool UnableToAuthenticate { get; private set; }
        public bool NotFound { get; private set; }
        public bool RequestEntityTooLarge { get; private set; }

        public int StatusCode { get; private set; }
        public string Message { get; private set; }

        public Exception Exception { get; private set; }
    }
}