using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Exceptionless.Configuration;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Json.Linq;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Submission.Net;

namespace Exceptionless.Submission {
    public class DefaultSubmissionClient : ISubmissionClient, IDisposable {
        private readonly Lazy<HttpClient> _client;

        public DefaultSubmissionClient(ExceptionlessConfiguration config) {
            _client = new Lazy<HttpClient>(() => CreateHttpClient(config));
        }

        public SubmissionResponse PostEvents(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            if (!config.IsValid)
                return new SubmissionResponse(500, message: "Invalid client configuration settings");

            string data = serializer.Serialize(events);
            string url = String.Format("{0}/events", GetServiceEndPoint(config));

            HttpResponseMessage response;
            try {
                HttpContent content = new StringContent(data, Encoding.UTF8, "application/json");

                // don't compress data smaller than 4kb
                if (data.Length > 1024 * 4)
                    content = new GzipContent(content);

                _client.Value.AddAuthorizationHeader(config.ApiKey);
                response = _client.Value.PostAsync(url, content).ConfigureAwait(false).GetAwaiter().GetResult();
            } catch (Exception ex) {
                return new SubmissionResponse(500, message: ex.Message);
            }
            
            int settingsVersion;
            if (Int32.TryParse(GetSettingsVersionHeader(response.Headers), out settingsVersion))
                SettingsManager.CheckVersion(settingsVersion, config);

            return new SubmissionResponse((int)response.StatusCode, GetResponseMessage(response));
        }

        public SubmissionResponse PostUserDescription(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            if (!config.IsValid)
                return new SubmissionResponse(500, message: "Invalid client configuration settings.");

            string data = serializer.Serialize(description);
            string url = String.Format("{0}/events/by-ref/{1}/user-description", GetServiceEndPoint(config), referenceId);

            HttpResponseMessage response;
            try {
                HttpContent content = new StringContent(data, Encoding.UTF8, "application/json");

                // don't compress data smaller than 4kb
                if (data.Length > 1024 * 4)
                    content = new GzipContent(content);

                _client.Value.AddAuthorizationHeader(config.ApiKey);
                response = _client.Value.PostAsync(url, content).ConfigureAwait(false).GetAwaiter().GetResult();
            } catch (Exception ex) {
                return new SubmissionResponse(500, message: ex.Message);
            }

            int settingsVersion;
            if (Int32.TryParse(GetSettingsVersionHeader(response.Headers), out settingsVersion))
                SettingsManager.CheckVersion(settingsVersion, config);

            return new SubmissionResponse((int)response.StatusCode, GetResponseMessage(response));
        }

        public SettingsResponse GetSettings(ExceptionlessConfiguration config, int version, IJsonSerializer serializer) {
            if (!config.IsValid)
                return new SettingsResponse(false, message: "Invalid client configuration settings.");

            string url = String.Format("{0}/projects/config?v={1}", GetConfigServiceEndPoint(config), version);

            HttpResponseMessage response;
            try {
                _client.Value.AddAuthorizationHeader(config.ApiKey);
                response = _client.Value.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
            } catch (Exception ex) {
                var message = String.Concat("Unable to retrieve configuration settings. Exception: ", ex.GetMessage());
                return new SettingsResponse(false, message: message);
            }

            if (response != null && response.StatusCode == HttpStatusCode.NotModified)
                return new SettingsResponse(false, message: "Settings have not been modified.");

            if (response == null || response.StatusCode != HttpStatusCode.OK)
                return new SettingsResponse(false, message: String.Concat("Unable to retrieve configuration settings: ", GetResponseMessage(response)));

            var json = GetResponseText(response);
            if (String.IsNullOrWhiteSpace(json))
                return new SettingsResponse(false, message: "Invalid configuration settings.");

            var settings = serializer.Deserialize<ClientConfiguration>(json);
            return new SettingsResponse(true, settings.Settings, settings.Version);
        }

        public void SendHeartbeat(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config) {
            if (!config.IsValid)
                return;

            string url = String.Format("{0}/events/session/heartbeat?id={1}&close={2}", GetHeartbeatServiceEndPoint(config), sessionIdOrUserId, closeSession);
            try {
                _client.Value.AddAuthorizationHeader(config.ApiKey);
                _client.Value.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
            } catch (Exception ex) {
                var log = config.Resolver.GetLog();
                log.Error(String.Concat("Error submitting heartbeat: ", ex.GetMessage()));
            }
        }

        protected virtual HttpClient CreateHttpClient(ExceptionlessConfiguration config) {
#if NET45
            var handler = new WebRequestHandler { UseDefaultCredentials = true };
            handler.ServerCertificateValidationCallback = delegate { return true; };
#else
            var handler = new HttpClientHandler { UseDefaultCredentials = true };
#if !PORTABLE && !NETSTANDARD1_2
            //handler.ServerCertificateCustomValidationCallback = delegate { return true; };
#endif
#endif
            if (handler.SupportsAutomaticDecompression)
                handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;

            if (handler.SupportsRedirectConfiguration)
                handler.AllowAutoRedirect = true;

            if (handler.SupportsProxy && config.Proxy != null)
                handler.Proxy = config.Proxy;

            var client = new HttpClient(handler, true);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);

            return client;
        }

        private string GetResponseMessage(HttpResponseMessage response) {
            if (response.IsSuccessStatusCode)
                return null;

            int statusCode = (int)response.StatusCode;
            if (statusCode == 404)
                return "404 Page not found.";

            string responseText = GetResponseText(response);
            string message = responseText.Length < 500 ? responseText : "";

            if (responseText.Trim().StartsWith("{")) {
                try {
                    var responseJson = JObject.Parse(responseText);
                    message = responseJson["message"].Value<string>();
                } catch { }
            }

            return message;
        }

        private string GetResponseText(HttpResponseMessage response) {
            try {
                return response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            } catch {}

            return null;
        }
        
        private string GetSettingsVersionHeader(HttpResponseHeaders headers) {
            IEnumerable<string> values;
            if (headers != null && headers.TryGetValues(ExceptionlessHeaders.ConfigurationVersion, out values))
                return values.FirstOrDefault();

            return null;
        }
        
        private Uri GetServiceEndPoint(ExceptionlessConfiguration config) {
            var builder = new UriBuilder(config.ServerUrl);
            builder.Path += builder.Path.EndsWith("/") ? "api/v2" : "/api/v2";

            // EnableSSL
            if (builder.Scheme == "https" && builder.Port == 80 && !builder.Host.Contains("local"))
                builder.Port = 443;

            return builder.Uri;
        }

        private Uri GetConfigServiceEndPoint(ExceptionlessConfiguration config) {
            var builder = new UriBuilder(config.ConfigServerUrl);
            builder.Path += builder.Path.EndsWith("/") ? "api/v2" : "/api/v2";

            // EnableSSL
            if (builder.Scheme == "https" && builder.Port == 80 && !builder.Host.Contains("local"))
                builder.Port = 443;

            return builder.Uri;
        }

        private Uri GetHeartbeatServiceEndPoint(ExceptionlessConfiguration config) {
            var builder = new UriBuilder(config.HeartbeatServerUrl);
            builder.Path += builder.Path.EndsWith("/") ? "api/v2" : "/api/v2";

            // EnableSSL
            if (builder.Scheme == "https" && builder.Port == 80 && !builder.Host.Contains("local"))
                builder.Port = 443;

            return builder.Uri;
        }

        public void Dispose() {
            if (_client.IsValueCreated)
                _client.Value.Dispose();
        }
    }
}