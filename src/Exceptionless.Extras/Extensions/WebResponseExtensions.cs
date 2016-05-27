using System;
using System.IO;
using System.Net;

namespace Exceptionless.Extras.Extensions {
    internal static class WebResponseExtensions {
        public static string GetResponseText(this WebResponse response) {
            try {
                using (response) {
                    using (var stream = response.GetResponseStream()) {
                        using (var reader = new StreamReader(stream)) {
                            return reader.ReadToEnd();
                        }
                    }
                }
            } catch (Exception) {
                return null;
            }
        }

        public static bool IsSuccessful(this HttpWebResponse response) {
            return response != null && (int)response.StatusCode >= 200 && (int)response.StatusCode <= 299;
        }
    }
}