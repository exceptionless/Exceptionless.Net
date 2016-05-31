﻿using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Submission.Net;

namespace Exceptionless.Extensions {
    internal static class WebRequestExtensions {
        public const string JSON_CONTENT_TYPE = "application/json";
        
        public static void AddAuthorizationHeader(this WebRequest request, ExceptionlessConfiguration configuration) {
            var authorizationHeader = new AuthorizationHeader {
                Scheme = ExceptionlessHeaders.Bearer,
                ParameterText = configuration.ApiKey
            };

            request.Headers[HttpRequestHeader.Authorization] = authorizationHeader.ToString();
        }

        private static readonly Lazy<PropertyInfo> _userAgentProperty = new Lazy<PropertyInfo>(() => typeof(HttpWebRequest).GetProperty("UserAgent"));

        public static void SetUserAgent(this HttpWebRequest request, ExceptionlessConfiguration configuration) {
            if (_userAgentProperty.Value != null) {
                try {
                    _userAgentProperty.Value.SetValue(request, configuration.UserAgent, null);
                    return;
                } catch (Exception ex) {
                    configuration.Resolver.GetLog().Error(ex, "Error occurred setting the user agent.");
                }
            }

            request.Headers[ExceptionlessHeaders.Client] = configuration.UserAgent;
        }

        public static async Task<WebResponse> PostJsonAsync(this HttpWebRequest request, string data) {
            request.Accept = request.ContentType = JSON_CONTENT_TYPE;
            request.Method = "POST";

            byte[] buffer = Encoding.UTF8.GetBytes(data);
            using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false)) {
                await requestStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                return await request.GetResponseAsync().ConfigureAwait(false);
            }
        }

        public static Task<WebResponse> GetJsonAsync(this HttpWebRequest request) {
            request.Accept = JSON_CONTENT_TYPE;
            request.Method = "GET";

            return request.GetResponseAsync();
        }
    }
}