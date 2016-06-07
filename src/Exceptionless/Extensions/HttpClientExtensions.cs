using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Exceptionless.Submission.Net;

namespace Exceptionless.Extensions {
    internal static class HttpClientExtensions {
        public static void AddAuthorizationHeader(this HttpClient client, string apiKey) {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ExceptionlessHeaders.Bearer, apiKey);
        }
    }
}