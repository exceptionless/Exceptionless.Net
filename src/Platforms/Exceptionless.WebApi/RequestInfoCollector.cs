using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Web.Http.Controllers;
using Exceptionless.Extensions;
using Exceptionless.Models.Data;

namespace Exceptionless.ExtendedData {
    internal static class RequestInfoCollector {
        private const long MAX_CONTENT_LENGTH = 1024 * 50;

        private static readonly List<string> _ignoredFormFields = new List<string> {
            "__*"
        };

        private static readonly List<string> _ignoredCookies = new List<string> {
            ".ASPX*",
            "__*",
            "*SessionId*"
        };

        public static RequestInfo Collect(HttpActionContext context, ExceptionlessConfiguration config) {
            if (context == null)
                return null;

            var info = new RequestInfo {
                HttpMethod = context.Request.Method.Method
            };

            if (config.IncludePrivateInformation)
                info.ClientIpAddress = context.Request.GetClientIpAddress();

            if (context.Request.Headers.UserAgent != null)
                info.UserAgent = context.Request.Headers.UserAgent.ToString();

            if (context.Request.RequestUri != null) {
                info.Host = context.Request.RequestUri.Host;
                info.IsSecure = context.Request.RequestUri.Scheme == "https";
                info.Path = String.IsNullOrEmpty(context.Request.RequestUri.LocalPath) ? "/" : context.Request.RequestUri.LocalPath;
                info.Port = context.Request.RequestUri.Port;
            }

            if (context.Request.Headers.Referrer != null)
                info.Referrer = context.Request.Headers.Referrer.ToString();

            var exclusionList = config.DataExclusions as string[] ?? config.DataExclusions.ToArray();
            info.Cookies = context.Request.Headers.GetCookies().ToDictionary(exclusionList);
            info.QueryString = context.Request.RequestUri.ParseQueryString().ToDictionary(exclusionList);
            info.PostData = GetPostData(context.Request.Content, exclusionList);

            return info;
        }

        private static Dictionary<string, string> ToDictionary(this IEnumerable<CookieHeaderValue> cookies, IEnumerable<string> exclusions) {
            var d = new Dictionary<string, string>();

            foreach (CookieHeaderValue cookie in cookies) {
                foreach (CookieState innerCookie in cookie.Cookies.Where(k => k != null && !String.IsNullOrEmpty(k.Name) && !k.Name.AnyWildcardMatches(_ignoredCookies) && !k.Name.AnyWildcardMatches(exclusions))) {
                    if (!d.ContainsKey(innerCookie.Name))
                        d.Add(innerCookie.Name, innerCookie.Value);
                }
            }

            return d;
        }

        private static Dictionary<string, string> ToDictionary(this NameValueCollection values, IEnumerable<string> exclusions) {
            var d = new Dictionary<string, string>();

            var patternsToMatch = exclusions as string[] ?? exclusions.ToArray();
            foreach (string key in values.AllKeys) {
                if (String.IsNullOrEmpty(key) || key.AnyWildcardMatches(_ignoredFormFields) || key.AnyWildcardMatches(patternsToMatch))
                    continue;

                try {
                    string value = values.Get(key);
                    d.Add(key, value);
                }
                catch (Exception ex) {
                    if (!d.ContainsKey(key))
                        d.Add(key, ex.Message);
                }
            }

            return d;
        }

        private static string GetClientIpAddress(this HttpRequestMessage request) {
            try {
                if (request.Properties.ContainsKey("MS_HttpContext")) {
                    object context = request.Properties["MS_HttpContext"];
                    if (context != null) {
                        PropertyInfo webRequestProperty = context.GetType().GetProperty("Request");
                        if (webRequestProperty != null) {
                            object webRequest = webRequestProperty.GetValue(context, null);
                            PropertyInfo userHostAddressProperty = webRequestProperty.PropertyType.GetProperty("UserHostAddress");
                            if (userHostAddressProperty != null)
                                return userHostAddressProperty.GetValue(webRequest, null) as string;
                        }
                    }
                }

                if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
                    return ((RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name]).Address;
            }
            catch { }

            return String.Empty;
        }

        private static object GetPostData(HttpContent httpContent, string[] exclusionList) {
            if (httpContent.IsFormData()) {
                var content = httpContent.ReadAsFormDataAsync().GetAwaiter().GetResult();
                return content.ToDictionary(exclusionList);
            }

            if (!httpContent.Headers.ContentLength.HasValue)
                return null;

            if (httpContent.Headers.ContentLength.Value < MAX_CONTENT_LENGTH) {
                return GetContent(httpContent);
            }

            var contentSize = Math.Round(httpContent.Headers.ContentLength.Value / 1024m, 0).ToString("N0");
            return $"Data is too large ({contentSize}kb) to be included.";
        }

        private static object GetContent(HttpContent httpContent) {
            try {
                var contentStream = httpContent.ReadAsStreamAsync().GetAwaiter().GetResult();
                if (contentStream.CanSeek && contentStream.Position > 0)
                    contentStream.Position = 0;

                if (contentStream.Position == 0) {
                    using (var inputStream = new StreamReader(contentStream))
                        return inputStream.ReadToEnd();
                }

                return "Unable to get POST data: The stream could not be reset.";
            }
            catch (Exception ex) {
                return "Error retrieving POST data: " + ex.Message;
            }
        }
    }
}