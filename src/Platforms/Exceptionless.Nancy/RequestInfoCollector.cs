using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Exceptionless.Extensions;
using Exceptionless.Models.Data;
using Nancy;
using Nancy.Helpers;

namespace Exceptionless.ExtendedData {
    internal static class RequestInfoCollector {
        public static RequestInfo Collect(NancyContext context, ExceptionlessConfiguration config) {
            if (context == null)
                return null;

            var info = new RequestInfo {
                HttpMethod = context.Request.Method
            };

            if (config.IncludeIpAddress)
                info.ClientIpAddress = context.Request.UserHostAddress;

            if (!String.IsNullOrWhiteSpace(context.Request.Headers.UserAgent))
                info.UserAgent = context.Request.Headers.UserAgent;

            if (context.Request.Url != null) {
                info.Host = context.Request.Url.HostName;
                info.IsSecure = context.Request.Url.IsSecure;
                info.Path = context.Request.Url.BasePath + context.Request.Url.Path;
                info.Port = context.Request.Url.Port ?? 80;
            }

            if (!String.IsNullOrWhiteSpace(context.Request.Headers.Referrer))
                info.Referrer = context.Request.Headers.Referrer;

            var exclusionsArray = config.DataExclusions as string[] ?? config.DataExclusions.ToArray();
            if (config.IncludeCookies)
                info.Cookies = context.Request.Cookies.ToDictionary(exclusionsArray);

            if (config.IncludeQueryString && context.Request.Url != null && !String.IsNullOrWhiteSpace(context.Request.Url.Query))
                info.QueryString = HttpUtility.ParseQueryString(context.Request.Url.Query).ToDictionary(exclusionsArray);

            return info;
        }

        private static readonly List<string> _ignoredFormFields = new List<string> {
            "__*"
        };

        private static readonly List<string> _ignoredCookies = new List<string> {
            ".ASPX*",
            "__*",
            "*SessionId*",
            "_ncfa"
        };

        private static Dictionary<string, string> ToDictionary(this IEnumerable<KeyValuePair<string, string>> cookies, IEnumerable<string> exclusions) {
            var d = new Dictionary<string, string>();

            foreach (var kv in cookies.Where(pair => !String.IsNullOrEmpty(pair.Key) && !pair.Key.AnyWildcardMatches(_ignoredCookies) && !pair.Key.AnyWildcardMatches(exclusions))) {
                if (!d.ContainsKey(kv.Key))
                    d.Add(kv.Key, kv.Value);
            }

            return d;
        }

        private static Dictionary<string, string> ToDictionary(this NameValueCollection values, IEnumerable<string> exclusions) {
            var d = new Dictionary<string, string>();

            var exclusionsArray = exclusions as string[] ?? exclusions.ToArray();
            foreach (string key in values.AllKeys) {
                if (String.IsNullOrEmpty(key) || key.AnyWildcardMatches(_ignoredFormFields) || key.AnyWildcardMatches(exclusionsArray))
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
    }
}