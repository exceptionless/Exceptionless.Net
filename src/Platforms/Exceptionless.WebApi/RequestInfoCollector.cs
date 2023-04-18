using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        public static RequestInfo Collect(HttpActionContext context, ExceptionlessConfiguration config) {
            if (context == null)
                return null;

            var info = new RequestInfo {
                HttpMethod = context.Request.Method.Method
            };

            if (config.IncludeIpAddress)
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
            if (config.IncludeHeaders)
                info.Headers = context.Request.Headers.ToDictionary(exclusionList);

            if (config.IncludeCookies)
                info.Cookies = context.Request.Headers.GetCookies().ToDictionary(exclusionList);

            if (config.IncludeQueryString)
                info.QueryString = context.Request.RequestUri.ParseQueryString().ToDictionary(exclusionList);

            // TODO Collect form data.
            return info;
        }

        private static readonly List<string> _ignoredHeaders = new List<string> {
            "Authorization",
            "Cookie",
            "Host",
            "Method",
            "Path",
            "Proxy-Authorization",
            "Referer",
            "User-Agent"
        };

        private static readonly List<string> _ignoredCookies = new List<string> {
            ".ASPX*",
            "__*",
            "*SessionId*"
        };

        private static readonly List<string> _ignoredFormFields = new List<string> {
            "__*"
        };

        // TODO: MAX_DATA_ITEM_LENGTH
        private static Dictionary<string, string[]> ToDictionary(this HttpRequestHeaders headers, string[] exclusions) {
            var d = new Dictionary<string, string[]>();

            foreach (var header in headers) {
                if (!String.IsNullOrEmpty(header.Key) && !_ignoredHeaders.Contains(header.Key) && !header.Key.AnyWildcardMatches(exclusions))
                    d.Add(header.Key, header.Value.ToArray());
            }

            return d;
        }

        private static Dictionary<string, string> ToDictionary(this IEnumerable<CookieHeaderValue> cookies, string[] exclusions) {
            var d = new Dictionary<string, string>();

            foreach (var cookie in cookies) {
                foreach (var innerCookie in cookie.Cookies) {
                    if (innerCookie == null || String.IsNullOrEmpty(innerCookie.Name) || innerCookie.Name.AnyWildcardMatches(_ignoredCookies) || innerCookie.Name.AnyWildcardMatches(exclusions))
                        continue;

                    if (!d.ContainsKey(innerCookie.Name))
                        d.Add(innerCookie.Name, innerCookie.Value);
                }
            }

            return d;
        }

        private static Dictionary<string, string> ToDictionary(this NameValueCollection values, string[] exclusions) {
            var d = new Dictionary<string, string>();
            
            foreach (string key in values.AllKeys) {
                if (String.IsNullOrEmpty(key) || key.AnyWildcardMatches(_ignoredFormFields) || key.AnyWildcardMatches(exclusions))
                    continue;

                try {
                    string value = values.Get(key);
                    d.Add(key, value);
                } catch (Exception ex) {
                    if (!d.ContainsKey(key))
                        d.Add(key, ex.Message);
                }
            }

            return d;
        }

        public static string GetClientIpAddress(this HttpRequestMessage request) {
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
            } catch {}

            return String.Empty;
        }
    }
}