using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Exceptionless.Models.Data;
using Exceptionless.Extensions;
using Exceptionless.Dependency;
using System.Text;

namespace Exceptionless.AspNetCore {
    public static class RequestInfoCollector {
        private const int MAX_BODY_SIZE = 50 * 1024;
        private const int MAX_DATA_ITEM_LENGTH = 1000;

        public static RequestInfo Collect(HttpContext context, ExceptionlessConfiguration config) {
            if (context == null)
                return null;

            var info = new RequestInfo {
                HttpMethod = context.Request.Method,
                IsSecure = context.Request.IsHttps,
                Path = context.Request.Path.HasValue ? context.Request.Path.Value : "/"
            };

            if (config.IncludeIpAddress)
                info.ClientIpAddress = context.GetClientIpAddress();

            if (!String.IsNullOrEmpty(context.Request.Host.Host))
                info.Host = context.Request.Host.Host;

            info.Port = context.Request.Host.Port.GetValueOrDefault(info.IsSecure ? 443 : 80);

            if (context.Request.Headers.TryGetValue(HeaderNames.UserAgent, out var userAgentHeader))
                info.UserAgent = userAgentHeader.ToString();

            if (context.Request.Headers.TryGetValue(HeaderNames.Referer, out var refererHeader))
                info.Referrer = refererHeader.ToString();

            var exclusionList = config.DataExclusions as string[] ?? config.DataExclusions.ToArray();
            if (config.IncludeHeaders)
                info.Headers = context.Request.Headers.ToHeaderDictionary(exclusionList);

            if (config.IncludeCookies)
                info.Cookies = context.Request.Cookies.ToDictionary(exclusionList);

            if (config.IncludeQueryString)
                info.QueryString = context.Request.Query.ToDictionary(exclusionList);

            if (config.IncludePostData && !String.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                info.PostData = GetPostData(context, config, exclusionList);

            return info;
        }

        private static object GetPostData(HttpContext context, ExceptionlessConfiguration config, string[] exclusionList) {
            var log = config.Resolver.GetLog();

            if (context.Request.HasFormContentType && context.Request.Form.Count > 0) {
                log.Debug("Reading POST data from Request.Form");
                return context.Request.Form.ToDictionary(exclusionList);
            }

            var contentLength = context.Request.ContentLength.GetValueOrDefault();
            if(contentLength == 0) {
                string message = "Content-length was zero, empty post.";
                log.Debug(message);
                return message;
            }

            if (contentLength > MAX_BODY_SIZE) {
                string value = Math.Round(contentLength / 1024m, 0).ToString("N0");
                string message = $"Data is too large ({value}kb) to be included.";
                log.Debug(message);
                return message;
            }

            try {
                if (!context.Request.Body.CanSeek) {
                    string message = "Unable to get POST data: The stream could not be reset.";
                    log.Debug(message);
                    return message;
                }

                long originalPosition = context.Request.Body.Position;
                if (originalPosition > 0) {
                    context.Request.Body.Position = 0;
                }

                log.Debug($"Reading POST, original position: {originalPosition}");

                if (context.Request.Body.Position != 0) {
                    string message = "Unable to get POST data: The stream position was not at 0.";
                    log.Debug(message);
                    return message;
                }

                // pass default values, except for leaveOpen: true. This prevents us from disposing the underlying stream
                using (var inputStream = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true)) {
                    var sb = new StringBuilder();
                    int numRead;

                    int bufferSize = (int)Math.Min(1024, contentLength);

                    char[] buffer = new char[bufferSize];
                    while ((numRead = inputStream.ReadBlock(buffer, 0, bufferSize)) > 0 && (sb.Length + numRead) <= contentLength) {
                        sb.Append(buffer, 0, numRead);
                    }
                    string postData = sb.ToString();

                    context.Request.Body.Position = originalPosition;

                    log.Debug($"Reading POST, set back to position: {originalPosition}");
                    return postData;
                }
            }
            catch (Exception ex) {
                string message = $"Error retrieving POST data: {ex.Message}";
                log.Error(message);
                return message;
            }
        }

        private static readonly List<string> _ignoredHeaders = new List<string> {
            HeaderNames.Authorization,
            HeaderNames.Cookie,
            HeaderNames.Host,
            HeaderNames.Method,
            HeaderNames.Path,
            HeaderNames.ProxyAuthorization,
            HeaderNames.Referer,
            HeaderNames.UserAgent
        };

        private static readonly List<string> _ignoredCookies = new List<string> {
            ".ASPX*",
            "__*",
            "*SessionId*"
        };

        private static readonly List<string> _ignoredFormFields = new List<string> {
            "__*"
        };

        private static Dictionary<string, string[]> ToHeaderDictionary(this IEnumerable<KeyValuePair<string, StringValues>> headers, string[] exclusions) {
            var d = new Dictionary<string, string[]>();

            foreach (var header in headers) {
                if (String.IsNullOrEmpty(header.Key) || _ignoredHeaders.Contains(header.Key) || header.Key.AnyWildcardMatches(exclusions))
                    continue;

                string[] values = header.Value.Where(hv => hv != null && hv.Length < MAX_DATA_ITEM_LENGTH).ToArray();
                if (values.Length == 0)
                    continue;

                d[header.Key] = values;
            }

            return d;
        }

        private static Dictionary<string, string> ToDictionary(this IRequestCookieCollection cookies, string[] exclusions) {
            var d = new Dictionary<string, string>();

            foreach (var kvp in cookies) {
                if (String.IsNullOrEmpty(kvp.Key) || kvp.Key.AnyWildcardMatches(_ignoredCookies) || kvp.Key.AnyWildcardMatches(exclusions))
                    continue;

                if (kvp.Value == null || kvp.Value.Length >= MAX_DATA_ITEM_LENGTH)
                    continue;
                
                d[kvp.Key] = kvp.Value;
            }

            return d;
        }

        private static Dictionary<string, string> ToDictionary(this IEnumerable<KeyValuePair<string, StringValues>> values, string[] exclusions) {
            var d = new Dictionary<string, string>();

            foreach (var kvp in values) {
                if (String.IsNullOrEmpty(kvp.Key) || kvp.Key.AnyWildcardMatches(_ignoredFormFields) || kvp.Key.AnyWildcardMatches(exclusions))
                    continue;

                try {
                    string value = kvp.Value.ToString();
                    if (value.Length >= MAX_DATA_ITEM_LENGTH)
                        continue;
                    
                    d[kvp.Key] = value;
                } catch (Exception ex) {
                    d[kvp.Key] = $"EXCEPTION: {ex.Message}";
                }
            }

            return d;
        }

        private static string GetClientIpAddress(this HttpContext context) {
            if (context.Request.Headers.ContainsKey("X-Forwarded-For")) {
                string forwardedHeader = context.Request.Headers["X-Forwarded-For"].ToString();
                if (!String.IsNullOrEmpty(forwardedHeader)) {
                    string[] ips = forwardedHeader.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string ip in ips) {
                        string ipAddress = ip.Trim();
                        int location = ipAddress.IndexOf(':');
                        if (location > 0)
                            ipAddress = ipAddress.Substring(0, location - 1);

                        IPAddress temp;
                        if (IPAddress.TryParse(ipAddress, out temp))
                            return ipAddress;
                    }
                }
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
