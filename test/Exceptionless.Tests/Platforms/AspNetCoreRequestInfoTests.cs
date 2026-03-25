#if NET10_0_OR_GREATER
using System.Collections.Generic;
using System.IO;
using System.Text;
using Exceptionless;
using Exceptionless.Dependency;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class AspNetCoreRequestInfoTests {
        [Fact]
        public void GetRequestInfo_DoesNotReadPostData_ForHandledErrors() {
            // Arrange
            var context = CreateHttpContext("hello=world");
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());

            // Act
            var requestInfo = context.GetRequestInfo(config);

            // Assert
            Assert.NotNull(requestInfo);
            Assert.Null(requestInfo.PostData);
            Assert.Equal(0L, context.Request.Body.Position);
        }

        [Fact]
        public void GetRequestInfo_ReadsAndRestoresPostData_ForUnhandledErrors() {
            // Arrange
            const string body = "{\"hello\":\"world\"}";
            var context = CreateHttpContext(body);
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());

            context.Request.Body.Position = 5;

            // Act
            var requestInfo = context.GetRequestInfo(config, isUnhandledError: true);

            // Assert
            Assert.NotNull(requestInfo);
            Assert.Equal(body, Assert.IsType<string>(requestInfo.PostData));
            Assert.Equal(5L, context.Request.Body.Position);
        }

        [Fact]
        public void GetRequestInfo_ReadsFormData_ForUnhandledErrors() {
            // Arrange
            var context = CreateFormHttpContext();
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());

            // Act
            var requestInfo = context.GetRequestInfo(config, isUnhandledError: true);

            // Assert
            Assert.NotNull(requestInfo);
            var postData = Assert.IsType<Dictionary<string, string>>(requestInfo.PostData);
            Assert.Equal("world", postData["name"]);
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

        private static DefaultHttpContext CreateFormHttpContext() {
            const string formBody = "name=world";
            var bodyBytes = Encoding.UTF8.GetBytes(formBody);
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Request.ContentLength = bodyBytes.Length;
            context.Request.Body = new MemoryStream(bodyBytes);
            context.Request.Form = new FormCollection(new Dictionary<string, StringValues> {
                ["name"] = "world"
            });
            return context;
        }
    }
}
#endif
