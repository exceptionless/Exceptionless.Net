using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Models;
using Exceptionless.Serializer;
using Exceptionless.Submission;
using Xunit;

namespace Exceptionless.Tests.Submission {
    public class DefaultSubmissionClientTests {
        [Fact]
        public async Task PostEvents_ReadsStructuredErrorMessageWithSystemTextJson() {
            using var resolver = DependencyResolver.CreateDefault();
            var configuration = new ExceptionlessConfiguration(resolver) {
                ApiKey = "00000000000000000000000000000000",
                ServerUrl = "https://collector.example.test"
            };
            using var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) {
                Content = new StringContent("""{"message":"Invalid \u263A payload"}""")
            };
            using var client = new StubSubmissionClient(configuration, httpResponse);

            SubmissionResponse response = await client.PostEventsAsync(
                new List<Event> { new Event { Type = Event.KnownTypes.Log } },
                configuration,
                new DefaultJsonSerializer());

            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Invalid ☺ payload", response.Message);
        }

        private sealed class StubSubmissionClient : DefaultSubmissionClient {
            private readonly HttpResponseMessage _response;

            public StubSubmissionClient(ExceptionlessConfiguration configuration, HttpResponseMessage response)
                : base(configuration) {
                _response = response;
            }

            protected override HttpClient CreateHttpClient(ExceptionlessConfiguration config) {
                return new HttpClient(new StubMessageHandler(_response), disposeHandler: true);
            }
        }

        private sealed class StubMessageHandler : HttpMessageHandler {
            private readonly HttpResponseMessage _response;

            public StubMessageHandler(HttpResponseMessage response) {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
                return Task.FromResult(_response);
            }
        }
    }
}
