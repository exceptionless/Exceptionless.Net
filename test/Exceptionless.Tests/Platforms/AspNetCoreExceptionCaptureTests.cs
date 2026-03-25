#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Exceptionless.AspNetCore;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class AspNetCoreExceptionCaptureTests {
        [Fact]
        public async Task Invoke_CapturesHandledExceptionsFromExceptionHandlerFeature() {
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("handled");
            var middleware = new ExceptionlessMiddleware(currentContext => {
                currentContext.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature {
                    Error = exception
                });

                return Task.CompletedTask;
            }, client);

            await middleware.Invoke(context);

            var submission = Assert.Single(submittingEvents);
            Assert.False(submission.IsUnhandledError);
            Assert.Equal(nameof(IExceptionHandlerFeature), submission.Event.Data[Event.KnownDataKeys.SubmissionMethod]);

            var requestInfo = Assert.IsType<RequestInfo>(submission.Event.Data[Event.KnownDataKeys.RequestInfo]);
            Assert.Null(requestInfo.PostData);
        }

        [Fact]
        public async Task Invoke_DoesNotDuplicateHandledExceptionsCapturedByDiagnostics() {
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("handled");
            var listener = new ExceptionlessDiagnosticListener(client);
            var middleware = new ExceptionlessMiddleware(currentContext => {
                currentContext.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature {
                    Error = exception
                });

                return Task.CompletedTask;
            }, client);

            listener.OnNext(new KeyValuePair<string, object>("Microsoft.AspNetCore.Diagnostics.HandledException", new {
                httpContext = context,
                exception
            }));

            await middleware.Invoke(context);

            Assert.Single(submittingEvents);
        }

        [Fact]
        public async Task Invoke_DoesNotDuplicateUnhandledExceptionsCapturedByMiddleware() {
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("unhandled");
            var listener = new ExceptionlessDiagnosticListener(client);
            var middleware = new ExceptionlessMiddleware(_ => throw exception, client);

            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context));

            listener.OnNext(new KeyValuePair<string, object>("Microsoft.AspNetCore.Hosting.UnhandledException", new {
                httpContext = context,
                exception
            }));

            var submission = Assert.Single(submittingEvents);
            Assert.True(submission.IsUnhandledError);
            Assert.Equal(nameof(ExceptionlessMiddleware), submission.Event.Data[Event.KnownDataKeys.SubmissionMethod]);
        }

        private static ExceptionlessClient CreateClient(ICollection<EventSubmittingEventArgs> submittingEvents) {
            var client = new ExceptionlessClient(configuration => {
                configuration.ApiKey = "test-api-key";
                configuration.ServerUrl = "http://localhost:5200";
                configuration.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
                configuration.UseInMemoryStorage();
            });

            client.Configuration.AddPlugin(new ExceptionlessAspNetCorePlugin(null));
            client.SubmittingEvent += (_, args) => submittingEvents.Add(args);

            return client;
        }

        private static DefaultHttpContext CreateHttpContext() {
            const string body = "{\"hello\":\"world\"}";
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = bodyBytes.Length;
            context.Request.Body = new MemoryStream(bodyBytes);
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}
#endif
