using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models.Data;

namespace Exceptionless.ExtendedData {
    internal static class RequestInfoCollector {
        private const int MAX_DATA_ITEM_LENGTH = 1000;
        private const int MAX_BODY_SIZE = 50*1024;

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

            if (config.IncludePostData && !String.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                info.PostData = GetPostData(context, config, exclusionList);

            if (config.IncludeQueryString) {
                try {
                    info.QueryString = context.Request.QueryString.ToDictionary(exclusionList);
                } catch (Exception ex) {
                    config.Resolver.GetLog().Error(ex, "An error occurred while getting the query string");
                }
            }

            return info;
        }

        private static object GetPostData(HttpContextBase context, ExceptionlessConfiguration config, string[] exclusionList) {
            var log = config.Resolver.GetLog();

            if (context.Request.Form.Count > 0) {
                log.Debug("Reading POST data from Request.Form");

                return context.Request.Form.ToDictionary(exclusionList);
            }

            var contentLength = context.Request.ContentLength;
            if (contentLength == 0) {
                string message = "Content-length was zero, empty post.";
                log.Debug(message);
                return message;
            }

            if (contentLength > MAX_BODY_SIZE) {
                string value = Math.Round(contentLength / 1024m, 0).ToString("N0");
                string message = String.Format("Data is too large ({0}kb) to be included.", value);
                log.Debug(message);
                return message;
            }

            try {
                if (!context.Request.InputStream.CanSeek) {
                    string message = "Unable to get POST data: The stream could not be reset.";
                    log.Debug(message);
                    return message;
                }

                long originalPosition = context.Request.InputStream.Position;
                if (context.Request.InputStream.Position > 0) {
                    context.Request.InputStream.Position = 0;
                }

                log.FormattedDebug("Reading POST, original position: {0}", originalPosition);

                if (context.Request.InputStream.Position != 0) {
                    string message = "Unable to get POST data: The stream position was not at 0.";
                    log.Debug(message);
                    return message;
                }

                var maxDataToRead = contentLength == 0 ? MAX_BODY_SIZE : contentLength;

                // pass default values, except for leaveOpen: true. This prevents us from disposing the underlying stream
                using (var inputStream = new StreamReader(context.Request.InputStream, Encoding.UTF8, true, 1024, true)) {
                    var sb = new StringBuilder();
                    int numRead;

                    int bufferSize = Math.Min(1024, maxDataToRead);
                    
                    char[] buffer = new char[bufferSize];
                    while ((numRead = inputStream.ReadBlock(buffer, 0, bufferSize)) > 0 && (sb.Length + numRead) < maxDataToRead) {
                        sb.Append(buffer, 0, numRead);
                    }
                    string postData = sb.ToString();

                    context.Request.InputStream.Position = originalPosition;

                    log.FormattedDebug("Reading POST, set back to position: {0}", originalPosition);
                    return postData;
                }
            }
            catch (Exception ex) {
                string message = $"Error retrieving POST data: {ex.Message}";
                log.Error(message);
                return message;
            }
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