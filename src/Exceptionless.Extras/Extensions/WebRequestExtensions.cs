using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Exceptionless.Submission.Net;
using Exceptionless.Threading.Tasks;

namespace Exceptionless.Extras.Extensions {
    public static class WebRequestExtensions {
        public const string JSON_CONTENT_TYPE = "application/json";
        
        public static void AddAuthorizationHeader(this WebRequest request, ExceptionlessConfiguration configuration) {
            var authorizationHeader = new AuthorizationHeader {
                Scheme = ExceptionlessHeaders.Bearer,
                ParameterText = configuration.ApiKey
            };

            request.Headers[HttpRequestHeader.Authorization] = authorizationHeader.ToString();
        }

        private static readonly Lazy<PropertyInfo> _userAgentProperty = new Lazy<PropertyInfo>(() => typeof(HttpWebRequest).GetProperty("UserAgent"));

        public static void SetUserAgent(this HttpWebRequest request, string userAgent) {
            if (_userAgentProperty.Value != null)
                _userAgentProperty.Value.SetValue(request, userAgent, null);
            else
                request.Headers[ExceptionlessHeaders.Client] = userAgent;
        }

        public static async Task<WebResponse> PostJsonAsyncWithCompression(this HttpWebRequest request, string data) {
            // don't compress data smaller than 4kb
            bool shouldCompress = data.Length > 1024 * 4;
            request.Accept = request.ContentType = JSON_CONTENT_TYPE;
            request.Method = "POST";
            if (shouldCompress)
                request.Headers["Content-Encoding"] = "gzip";

            byte[] buffer = Encoding.UTF8.GetBytes(data);
            using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false)) {
                if (shouldCompress) {
                    using (var zipStream = new GZipStream(requestStream, CompressionMode.Compress)) {
                        await zipStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    }
                } else {
                    using (var stream = new BinaryWriter(requestStream)) {
                        stream.Write(buffer, 0, buffer.Length);
                    }
                }

                return await request.GetResponseAsync();
            }
        }

        public static Task<WebResponse> GetJsonAsync(this HttpWebRequest request) {
            request.Accept = JSON_CONTENT_TYPE;
            request.Method = "GET";

            return request.GetResponseAsync();
        }
    }
}