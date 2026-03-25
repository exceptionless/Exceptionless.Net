using System.IO;
using System.Text;
using Exceptionless;
using Exceptionless.Dependency;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class AspNetCoreRequestInfoTests {
        [Fact]
        public void GetRequestInfo_DoesNotReadPostData_ForHandledErrors() {
            var context = CreateHttpContext("hello=world");
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());

            var requestInfo = context.GetRequestInfo(config);

            Assert.NotNull(requestInfo);
            Assert.Null(requestInfo.PostData);
            Assert.Equal(0L, context.Request.Body.Position);
        }

        [Fact]
        public void GetRequestInfo_ReadsAndRestoresPostData_ForUnhandledErrors() {
            const string body = "{\"hello\":\"world\"}";
            var context = CreateHttpContext(body);
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());

            context.Request.Body.Position = 5;

            var requestInfo = context.GetRequestInfo(config, isUnhandledError: true);

            Assert.NotNull(requestInfo);
            Assert.Equal(body, Assert.IsType<string>(requestInfo.PostData));
            Assert.Equal(5L, context.Request.Body.Position);
        }

        private static DefaultHttpContext CreateHttpContext(string body) {
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/json";
            context.Request.Body = new MemoryStream(bodyBytes);
            context.Request.ContentLength = bodyBytes.Length;
            return context;
        }
    }
}
