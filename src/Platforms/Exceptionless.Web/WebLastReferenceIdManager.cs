using System;
using System.Web;
using Exceptionless.Logging;

namespace Exceptionless {
    internal sealed class WebLastReferenceIdManager : ILastReferenceIdManager {
        private const string LAST_REFERENCE_ID_KEY = "__LastReferenceId";

        public WebLastReferenceIdManager(IExceptionlessLog log) {
            if (log == null)
                throw new ArgumentNullException("log");

            Log = log;
        }

        public IExceptionlessLog Log { get; set; }

        /// <summary>
        /// Gets the last reference id that was submitted to the server.
        /// </summary>
        /// <returns>The reference id</returns>
        public string GetLast() {
            try {
                HttpContext httpContext = HttpContext.Current;
                if (httpContext == null)
                    throw new InvalidOperationException("WebLastReferenceIdManager can only be used in web contexts.");

                if (httpContext.Session != null && httpContext.Session[LAST_REFERENCE_ID_KEY] != null)
                    return httpContext.Session[LAST_REFERENCE_ID_KEY].ToString();

                if (httpContext.Request.Cookies[LAST_REFERENCE_ID_KEY] != null)
                    return httpContext.Request.Cookies[LAST_REFERENCE_ID_KEY].Value;
            } catch (Exception e) {
                Log.Warn("Error getting last reference id: {0}", e.Message);
            }

            return null;
        }

        /// <summary>
        /// Clears the last reference id.
        /// </summary>
        public void ClearLast() {
            HttpContext httpContext = HttpContext.Current;
            if (httpContext == null)
                return;

            if (httpContext.Session != null)
                httpContext.Session.Remove(LAST_REFERENCE_ID_KEY);

            if (httpContext.Request.Cookies[LAST_REFERENCE_ID_KEY] == null)
                return;

            HttpCookie cookie = httpContext.Request.Cookies[LAST_REFERENCE_ID_KEY];
            if (cookie == null)
                return;

            cookie.Expires = DateTime.UtcNow.AddDays(-1);
            httpContext.Response.Cookies.Add(cookie);
        }

        public void SetLast(string eventId) {
            HttpContext httpContext = HttpContext.Current;
            if (httpContext == null)
                return;

            if (httpContext.Session != null)
                httpContext.Session[LAST_REFERENCE_ID_KEY] = eventId;

            // Session doesn't seem to be reliable so set it in a cookie as well.
            try {
                var cookie = new HttpCookie(LAST_REFERENCE_ID_KEY);
                cookie.HttpOnly = true;
                cookie.Value = eventId;
                httpContext.Response.Cookies.Add(cookie);
            } catch (Exception e) {
                Log.Warn("Error setting reference id cookie: {0}", e.Message);
            }
        }
    }
}