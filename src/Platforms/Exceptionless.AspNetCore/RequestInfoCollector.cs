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

namespace Exceptionless.AspNetCore {
    internal static class RequestInfoCollector {
        public static RequestInfo Collect(HttpContext context, ExceptionlessConfiguration config) {
            if (context == null)
                return null;

            var info = new RequestInfo {
                HttpMethod = context.Request.Method,
                IsSecure = context.Request.IsHttps,
                Path = context.Request.Path.HasValue ? context.Request.Path.Value : "/",
            };

            if (config.IncludeIpAddress)
                info.ClientIpAddress = context.GetClientIpAddress();

            if (!String.IsNullOrEmpty(context.Request.Host.Host))
                info.Host = context.Request.Host.Host;

            if (context.Request.Host.Port.HasValue)
                info.Port = context.Request.Host.Port.Value;

            if (context.Request.Headers.ContainsKey(HeaderNames.UserAgent))
                info.UserAgent = context.Request.Headers[HeaderNames.UserAgent].ToString();

            if (context.Request.Headers.ContainsKey(HeaderNames.Referer))
                info.Referrer = context.Request.Headers[HeaderNames.Referer].ToString();

            var exclusionList = config.DataExclusions as string[] ?? config.DataExclusions.ToArray();
            if (config.IncludeCookies)
                info.Cookies = context.Request.Cookies.ToDictionary(exclusionList);

            if (config.IncludeQueryString)
                info.QueryString = context.Request.Query.ToDictionary(exclusionList);

            if (config.IncludePostData) {
                if (context.Request.HasFormContentType && context.Request.Form.Count > 0) {
                    info.PostData = context.Request.Form.ToDictionary(exclusionList);
                } else if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > 0) {
                    if (context.Request.ContentLength.Value < 1024 * 50) {
                        try {
                            if (context.Request.Body.CanSeek && context.Request.Body.Position > 0)
                                context.Request.Body.Position = 0;

                            if (context.Request.Body.Position == 0) {
                                using (var inputStream = new StreamReader(context.Request.Body))
                                    info.PostData = inputStream.ReadToEnd();
                            } else {
                                info.PostData = "Unable to get POST data: The stream could not be reset.";
                            }
                        } catch (Exception ex) {
                            info.PostData = "Error retrieving POST data: " + ex.Message;
                        }
                    } else {
                        string value = Math.Round(context.Request.ContentLength.Value / 1024m, 0).ToString("N0");
                        info.PostData = String.Format("Data is too large ({0}kb) to be included.", value);
                    }
                }
            }

            return info;
        }

        private static readonly List<string> _ignoredFormFields = new List<string> {
            "__*"
        };

        private static readonly List<string> _ignoredCookies = new List<string> {
            ".ASPX*",
            "__*",
            "*SessionId*"
        };

        private static Dictionary<string, string> ToDictionary(this IRequestCookieCollection cookies, IEnumerable<string> exclusions) {
            var d = new Dictionary<string, string>();

            foreach (var kvp in cookies) {
                if (String.IsNullOrEmpty(kvp.Key) || kvp.Key.AnyWildcardMatches(_ignoredCookies) || kvp.Key.AnyWildcardMatches(exclusions))
                    continue;

                d.Add(kvp.Key, kvp.Value);
            }

            return d;
        }

        private static Dictionary<string, string> ToDictionary(this IEnumerable<KeyValuePair<string, StringValues>> values, IEnumerable<string> exclusions) {
            var d = new Dictionary<string, string>();

            foreach (var kvp in values) {
                if (String.IsNullOrEmpty(kvp.Key) || kvp.Key.AnyWildcardMatches(_ignoredFormFields) || kvp.Key.AnyWildcardMatches(exclusions))
                    continue;

                try {
                    string value = kvp.Value.ToString();
                    d.Add(kvp.Key, value);
                } catch (Exception ex) {
                    if (!d.ContainsKey(kvp.Key))
                        d.Add(kvp.Key, ex.Message);
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