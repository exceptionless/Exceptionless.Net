#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.AspNetCore;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class AspNetCoreExceptionCaptureTests {
        [Fact]
        public async Task TryHandleAsync_WhenRequestIsActive_ReturnsFalseAndCapturesUnhandledException() {
            // Arrange
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("unhandled");
            var handler = new ExceptionlessExceptionHandler(client);

            // Act
            var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            Assert.False(result);
            var submission = Assert.Single(submittingEvents);
            Assert.True(submission.IsUnhandledError);
            Assert.Equal(nameof(ExceptionlessExceptionHandler), submission.Event.Data[Event.KnownDataKeys.SubmissionMethod]);
        }

        [Fact]
        public async Task TryHandleAsync_WhenCancellationIsRequested_ReturnsFalseWithoutCapturingException() {
            // Arrange
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var cts = new CancellationTokenSource();
            var context = CreateHttpContext();
            cts.Cancel();
            var handler = new ExceptionlessExceptionHandler(client);

            // Act
            var result = await handler.TryHandleAsync(context, new InvalidOperationException(), cts.Token);

            // Assert
            Assert.False(result);
            Assert.Empty(submittingEvents);
        }

        [Fact]
        public void OnNext_WhenUnhandledExceptionEventIsPublished_CapturesUnhandledException() {
            // Arrange
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("unhandled");
            var listener = new ExceptionlessDiagnosticListener(client);

            // Act
            listener.OnNext(new KeyValuePair<string, object>("Microsoft.AspNetCore.Hosting.UnhandledException", new {
                httpContext = context,
                exception
            }));

            // Assert
            var submission = Assert.Single(submittingEvents);
            Assert.True(submission.IsUnhandledError);
        }

        [Fact]
        public void OnNext_WhenHostingDiagnosticsUnhandledExceptionEventIsPublished_CapturesUnhandledException() {
            // Arrange
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("unhandled");
            var listener = new ExceptionlessDiagnosticListener(client);

            // Act
            listener.OnNext(new KeyValuePair<string, object>("Microsoft.AspNetCore.Hosting.Diagnostics.UnhandledException", new {
                httpContext = context,
                exception
            }));

            // Assert
            var submission = Assert.Single(submittingEvents);
            Assert.True(submission.IsUnhandledError);
        }

        [Fact]
        public void OnNext_WhenMiddlewareExceptionPayloadIsNull_DoesNotThrowOrCaptureException() {
            // Arrange
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var listener = new ExceptionlessDiagnosticListener(client);

            // Act
            listener.OnNext(new KeyValuePair<string, object>("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException", null));

            // Assert
            Assert.Empty(submittingEvents);
        }

        [Fact]
        public async Task Invoke_WhenResponseStatusIsNotFound_SubmitsNotFoundEvent() {
            // Arrange
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var middleware = new ExceptionlessMiddleware(currentContext => {
                currentContext.Response.StatusCode = 404;
                return Task.CompletedTask;
            }, client);

            // Act
            await middleware.Invoke(context);

            // Assert
            var submission = Assert.Single(submittingEvents);
            Assert.Equal(Event.KnownTypes.NotFound, submission.Event.Type);
        }

        [Fact]
        public async Task Invoke_WhenNextDelegateThrows_RethrowsExceptionWithoutSubmittingEvent() {
            // Arrange
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var middleware = new ExceptionlessMiddleware(_ => throw new InvalidOperationException("boom"), client);

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context));

            // Assert
            Assert.Empty(submittingEvents);
        }

        [Fact]
        public async Task DiagnosticListener_WhenExceptionAlreadySubmittedByHandler_SkipsDuplicateSubmission() {
            // Arrange
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("unhandled");
            var handler = new ExceptionlessExceptionHandler(client);
            var listener = new ExceptionlessDiagnosticListener(client);

            // Act — handler submits first, then diagnostic listener sees the same exception
            await handler.TryHandleAsync(context, exception, CancellationToken.None);
            listener.OnNext(new KeyValuePair<string, object>("Microsoft.AspNetCore.Hosting.UnhandledException", new {
                httpContext = context,
                exception
            }));

            // Assert — only one submission
            Assert.Single(submittingEvents);
        }

        [Fact]
        public void DiagnosticListener_WhenExceptionDiffersFromSubmitted_SubmitsNewEvent() {
            // Arrange
            var submittingEvents = new List<EventSubmittingEventArgs>();
            var client = CreateClient(submittingEvents);
            var context = CreateHttpContext();
            var listener = new ExceptionlessDiagnosticListener(client);

            // Mark a different exception as already submitted
            context.Items[ExceptionlessExceptionHandler.HttpContextSubmittedKey] = new InvalidOperationException("other");

            // Act — diagnostic listener sees a different exception instance
            var newException = new InvalidOperationException("different");
            listener.OnNext(new KeyValuePair<string, object>("Microsoft.AspNetCore.Hosting.UnhandledException", new {
                httpContext = context,
                exception = newException
            }));

            // Assert — still submits because it's a different exception
            Assert.Single(submittingEvents);
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
