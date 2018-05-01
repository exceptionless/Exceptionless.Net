using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models.Data;

namespace Exceptionless.ExtendedData {
    internal static class RequestInfoCollector {
        private const int MAX_DATA_ITEM_LENGTH = 1000;

        public static RequestInfo Collect(HttpContextBase context, ExceptionlessConfiguration config) {
            if (context == null)
                return null;

            var info = new RequestInfo {
                HttpMethod = context.Request.HttpMethod,
                UserAgent = context.Request.UserAgent,
                Path = String.IsNullOrEmpty(context.Request.Path) ? "/" : context.Request.Path
            };

            if (config.IncludeIpAddress) {
                try {
                    info.ClientIpAddress = GetUserIpAddress(context);
                } catch (ArgumentException ex) {
                    config.Resolver.GetLog().Error(ex, "An error occurred while setting the Client Ip Address.");
                }
            }

            try {
                info.IsSecure = context.Request.IsSecureConnection;
            } catch (ArgumentException ex) {
                config.Resolver.GetLog().Error(ex, "An error occurred while setting Is Secure Connection.");
            }

            if (context.Request.Url != null)
                info.Host = context.Request.Url.Host;

            if (context.Request.UrlReferrer != null)
                info.Referrer = context.Request.UrlReferrer.ToString();

            if (context.Request.Url != null)
                info.Port = context.Request.Url.Port;

            var exclusionList = config.DataExclusions as string[] ?? config.DataExclusions.ToArray();

            if (config.IncludeCookies)
                info.Cookies = context.Request.Cookies.ToDictionary(exclusionList);

            if (config.IncludePostData) {
                if (context.Request.Form.Count > 0) {
                    info.PostData = context.Request.Form.ToDictionary(exclusionList);
                } else if (context.Request.ContentLength > 0) {
                    if (context.Request.ContentLength < 1024 * 50) {
                        try {
                            if (context.Request.InputStream.CanSeek && context.Request.InputStream.Position > 0)
                                context.Request.InputStream.Position = 0;

                            if (context.Request.InputStream.Position == 0) {
                                using (var inputStream = new StreamReader(context.Request.InputStream))
                                    info.PostData = inputStream.ReadToEnd();
                            } else {
                                info.PostData = "Unable to get POST data: The stream could not be reset.";
                            }
                        } catch (Exception ex) {
                            info.PostData = "Error retrieving POST data: " + ex.Message;
                        }
                    } else {
                        string value = Math.Round(context.Request.ContentLength / 1024m, 0).ToString("N0");
                        info.PostData = String.Format("Data is too large ({0}kb) to be included.", value);
                    }
                }
            }

            if (config.IncludeQueryString) {
                try {
                    info.QueryString = context.Request.QueryString.ToDictionary(exclusionList);
                } catch (Exception ex) {
                    config.Resolver.GetLog().Error(ex, "An error occurred while getting the query string");
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

        private static Dictionary<string, string> ToDictionary(this HttpCookieCollection cookies, IEnumerable<string> exclusions) {
            var d = new Dictionary<string, string>();

            foreach (string key in cookies.AllKeys.Distinct().Where(k => !String.IsNullOrEmpty(k) && !k.AnyWildcardMatches(_ignoredCookies) && !k.AnyWildcardMatches(exclusions))) {
                try {
                    HttpCookie cookie = cookies.Get(key);
                    if (cookie != null && cookie.Value != null && cookie.Value.Length < MAX_DATA_ITEM_LENGTH && !d.ContainsKey(key))
                        d.Add(key, cookie.Value);
                } catch (Exception ex) {
                    if (!d.ContainsKey(key))
                        d.Add(key, ex.Message);
                }
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
                    if (value != null && !d.ContainsKey(key) && value.Length < MAX_DATA_ITEM_LENGTH)
                        d.Add(key, value);
                } catch (Exception ex) {
                    if (!d.ContainsKey(key))
                        d.Add(key, "EXCEPTION: " + ex.Message);
                }
            }

            return d;
        }

        private static string GetUserIpAddress(HttpContextBase context) {
            string clientIp = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (String.IsNullOrEmpty(clientIp))
                clientIp = context.Request.UserHostAddress;

            return clientIp;
        }
    }
}