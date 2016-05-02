using System;
using System.Collections.Generic;
using System.Net;
using Exceptionless.Configuration;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Json.Linq;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Submission.Net;

namespace Exceptionless.Submission {
    public class DefaultSubmissionClient : ISubmissionClient {
        public SubmissionResponse PostEvents(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            var data = serializer.Serialize(events);

            HttpWebResponse response;
            try {
                var request = CreateHttpWebRequest(config, String.Format("{0}/events", config.GetServiceEndPoint()));
                response = request.PostJsonAsync(data).Result as HttpWebResponse;
            } catch (AggregateException aex) {
                var ex = aex.GetInnermostException() as WebException;
                if (ex != null)
                    response = (HttpWebResponse)ex.Response;
                else
                    return new SubmissionResponse(500, message: aex.GetMessage());
            } catch (Exception ex) {
                return new SubmissionResponse(500, message: ex.Message);
            }

            int settingsVersion;
            if (Int32.TryParse(response.Headers[ExceptionlessHeaders.ConfigurationVersion], out settingsVersion))
                SettingsManager.CheckVersion(settingsVersion, config);

            return new SubmissionResponse((int)response.StatusCode, GetResponseMessage(response));
        }

        public SubmissionResponse PostUserDescription(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            var data = serializer.Serialize(description);

            HttpWebResponse response;
            try {
                var request = CreateHttpWebRequest(config, String.Format("{0}/events/by-ref/{1}/user-description", config.GetServiceEndPoint(), referenceId));
                response = request.PostJsonAsync(data).Result as HttpWebResponse;
            } catch (AggregateException aex) {
                var ex = aex.GetInnermostException() as WebException;
                if (ex != null)
                    response = (HttpWebResponse)ex.Response;
                else
                    return new SubmissionResponse(500, message: aex.GetMessage());
            } catch (Exception ex) {
                return new SubmissionResponse(500, message: ex.Message);
            }

            int settingsVersion;
            if (Int32.TryParse(response.Headers[ExceptionlessHeaders.ConfigurationVersion], out settingsVersion))
                SettingsManager.CheckVersion(settingsVersion, config);

            return new SubmissionResponse((int)response.StatusCode, GetResponseMessage(response));
        }

        public SettingsResponse GetSettings(ExceptionlessConfiguration config, IJsonSerializer serializer) {
            HttpWebResponse response;
            try {
                var request = CreateHttpWebRequest(config, String.Format("{0}/projects/config", config.GetServiceEndPoint()));
                response = request.GetJsonAsync().Result as HttpWebResponse;
            } catch (Exception ex) {
                var message = String.Concat("Unable to retrieve configuration settings. Exception: ", ex.GetMessage());
                return new SettingsResponse(false, message: message);
            }

            if (response == null || response.StatusCode != HttpStatusCode.OK)
                return new SettingsResponse(false, message: String.Concat("Unable to retrieve configuration settings: ", GetResponseMessage(response)));

            var json = response.GetResponseText();
            if (String.IsNullOrWhiteSpace(json))
                return new SettingsResponse(false, message: "Invalid configuration settings.");

            var settings = serializer.Deserialize<ClientConfiguration>(json);
            return new SettingsResponse(true, settings.Settings, settings.Version);
        }

        public void SendHeartbeat(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config) {
            try {
                var request = CreateHttpWebRequest(config, String.Format("{0}/events/session/heartbeat?id={1}&close={2}", config.GetHeartbeatServiceEndPoint(), sessionIdOrUserId, closeSession));
                var response = request.GetResponseAsync().Result;
            } catch (Exception ex) {
                var log = config.Resolver.GetLog();
                log.Error(String.Concat("Error submitting heartbeat: ", ex.GetMessage()));
            }
        }

        private static string GetResponseMessage(HttpWebResponse response) {
            if (response.IsSuccessful())
                return null;

            int statusCode = (int)response.StatusCode;
            string responseText = response.GetResponseText();
            string message = statusCode == 404 ? "404 Page not found." : responseText.Length < 500 ? responseText : "";

            if (responseText.Trim().StartsWith("{")) {
                try {
                    var responseJson = JObject.Parse(responseText);
                    message = responseJson["message"].Value<string>();
                } catch { }
            }

            return message;
        }

        protected virtual HttpWebRequest CreateHttpWebRequest(ExceptionlessConfiguration config, string url) {
#if PORTABLE40
            var request = WebRequest.CreateHttp(url);
#else 
            var request = (HttpWebRequest)WebRequest.Create(url);            
#endif
            request.AddAuthorizationHeader(config);
            request.SetUserAgent(config);
            return request;
        }
    }
}
