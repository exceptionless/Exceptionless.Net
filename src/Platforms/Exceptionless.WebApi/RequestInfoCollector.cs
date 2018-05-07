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
            if (config.IncludeCookies)
                info.Cookies = context.Request.Headers.GetCookies().ToDictionary(exclusionList);
            if (config.IncludeQueryString)
                info.QueryString = context.Request.RequestUri.ParseQueryString().ToDictionary(exclusionList);

            // TODO Collect form data.
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