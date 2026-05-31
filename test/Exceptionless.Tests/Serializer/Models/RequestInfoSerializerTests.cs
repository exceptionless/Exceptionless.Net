using System.Collections.Generic;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class RequestInfoSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteRequestInfo_ProducesSnakeCaseJson() {
            // Arrange
            var request = new RequestInfo {
                UserAgent = "Mozilla/5.0",
                HttpMethod = "POST",
                IsSecure = true,
                Host = "api.example.com",
                Port = 443,
                Path = "/api/v1/events",
                Referrer = "https://www.google.com",
                ClientIpAddress = "192.168.1.100",
                Headers = new Dictionary<string, string[]> {
                    { "Content-Type", new[] { "application/json" } },
                    { "Authorization", new[] { "Bearer token123" } }
                },
                Cookies = new Dictionary<string, string> {
                    { "session_id", "abc123" }
                },
                QueryString = new Dictionary<string, string> {
                    { "page", "1" },
                    { "limit", "50" }
                },
                Data = {
                    [RequestInfo.KnownDataKeys.Browser] = "Chrome",
                    [RequestInfo.KnownDataKeys.BrowserVersion] = "120.0",
                    [RequestInfo.KnownDataKeys.OS] = "Windows"
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(request);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "user_agent", "http_method", "is_secure", "host", "port", "path",
                "referrer", "client_ip_address", "headers", "cookies", "post_data",
                "query_string", "data");
            SerializerContractAssertions.ExcludesProperties(json,
                "UserAgent", "HttpMethod", "IsSecure", "ClientIpAddress", "QueryString", "PostData");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new RequestInfo {
                UserAgent = "TestClient/2.0",
                HttpMethod = "PUT",
                IsSecure = false,
                Host = "localhost",
                Port = 5000,
                Path = "/api/users/123",
                Referrer = "http://localhost/dashboard",
                ClientIpAddress = "127.0.0.1",
                Headers = new Dictionary<string, string[]> {
                    { "Accept", new[] { "application/json", "text/plain" } }
                },
                Cookies = new Dictionary<string, string> {
                    { "theme", "dark" }
                },
                QueryString = new Dictionary<string, string> {
                    { "include", "profile" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (RequestInfo)serializer.Deserialize(json, typeof(RequestInfo));

            // Assert
            Assert.Equal(original.UserAgent, deserialized.UserAgent);
            Assert.Equal(original.HttpMethod, deserialized.HttpMethod);
            Assert.Equal(original.IsSecure, deserialized.IsSecure);
            Assert.Equal(original.Host, deserialized.Host);
            Assert.Equal(original.Port, deserialized.Port);
            Assert.Equal(original.Path, deserialized.Path);
            Assert.Equal(original.Referrer, deserialized.Referrer);
            Assert.Equal(original.ClientIpAddress, deserialized.ClientIpAddress);
        }

        [Fact]
        public void Deserialize_WithHeaders_PreservesMultiValueHeaders() {
            // Arrange
            var serializer = GetSerializer();
            var original = new RequestInfo {
                HttpMethod = "GET",
                Path = "/test",
                Headers = new Dictionary<string, string[]> {
                    { "Accept", new[] { "application/json", "text/html" } },
                    { "X-Custom", new[] { "value1" } }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (RequestInfo)serializer.Deserialize(json, typeof(RequestInfo));

            // Assert
            Assert.NotNull(deserialized.Headers);
            Assert.Equal(2, deserialized.Headers.Count);
            Assert.Equal(2, deserialized.Headers["Accept"].Length);
            Assert.Equal("application/json", deserialized.Headers["Accept"][0]);
            Assert.Equal("text/html", deserialized.Headers["Accept"][1]);
        }

        [Fact]
        public void Deserialize_WithQueryString_PreservesAllParameters() {
            // Arrange
            var serializer = GetSerializer();
            var original = new RequestInfo {
                HttpMethod = "GET",
                Path = "/search",
                QueryString = new Dictionary<string, string> {
                    { "q", "test query" },
                    { "page", "2" },
                    { "sort", "date" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (RequestInfo)serializer.Deserialize(json, typeof(RequestInfo));

            // Assert
            Assert.NotNull(deserialized.QueryString);
            Assert.Equal(3, deserialized.QueryString.Count);
            Assert.Equal("test query", deserialized.QueryString["q"]);
            Assert.Equal("2", deserialized.QueryString["page"]);
            Assert.Equal("date", deserialized.QueryString["sort"]);
        }

        [Fact]
        public void Deserialize_WithCookies_PreservesCookies() {
            // Arrange
            var serializer = GetSerializer();
            var original = new RequestInfo {
                HttpMethod = "GET",
                Path = "/dashboard",
                Cookies = new Dictionary<string, string> {
                    { "session", "xyz789" },
                    { "lang", "en-US" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (RequestInfo)serializer.Deserialize(json, typeof(RequestInfo));

            // Assert
            Assert.NotNull(deserialized.Cookies);
            Assert.Equal(2, deserialized.Cookies.Count);
            Assert.Equal("xyz789", deserialized.Cookies["session"]);
            Assert.Equal("en-US", deserialized.Cookies["lang"]);
        }

        [Fact]
        public void Deserialize_WithPostData_PreservesPostData() {
            // Arrange
            var serializer = GetSerializer();
            var request = new RequestInfo {
                HttpMethod = "POST",
                Path = "/api/data",
                PostData = new { Name = "Test", Value = 42 }
            };

            // Act
            string json = serializer.Serialize(request);

            // Assert
            Assert.Contains("\"post_data\"", json);
            Assert.Contains("\"Name\"", json);
        }

        [Fact]
        public void Serialize_KnownDataKeys_UsesCorrectConstants() {
            // Arrange
            var request = new RequestInfo {
                HttpMethod = "GET",
                Path = "/test",
                Data = {
                    [RequestInfo.KnownDataKeys.Browser] = "Firefox",
                    [RequestInfo.KnownDataKeys.BrowserVersion] = "110.0",
                    [RequestInfo.KnownDataKeys.BrowserMajorVersion] = "110",
                    [RequestInfo.KnownDataKeys.Device] = "Desktop",
                    [RequestInfo.KnownDataKeys.OS] = "macOS",
                    [RequestInfo.KnownDataKeys.OSVersion] = "13.2",
                    [RequestInfo.KnownDataKeys.OSMajorVersion] = "13",
                    [RequestInfo.KnownDataKeys.IsBot] = "False"
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(request);

            // Assert
            Assert.Contains("\"@browser\":\"Firefox\"", json);
            Assert.Contains("\"@browser_version\":\"110.0\"", json);
            Assert.Contains("\"@browser_major_version\":\"110\"", json);
            Assert.Contains("\"@device\":\"Desktop\"", json);
            Assert.Contains("\"@os\":\"macOS\"", json);
            Assert.Contains("\"@os_version\":\"13.2\"", json);
            Assert.Contains("\"@os_major_version\":\"13\"", json);
            Assert.Contains("\"@is_bot\":\"False\"", json);
        }

        [Fact]
        public void Deserialize_MinimalRequestInfo_PreservesProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new RequestInfo {
                HttpMethod = "DELETE",
                Path = "/api/items/456"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (RequestInfo)serializer.Deserialize(json, typeof(RequestInfo));

            // Assert
            Assert.Equal("DELETE", deserialized.HttpMethod);
            Assert.Equal("/api/items/456", deserialized.Path);
        }

        [Fact]
        public void Deserialize_WithEncodedPath_PreservesEncoding() {
            // Arrange
            var serializer = GetSerializer();
            var original = new RequestInfo {
                HttpMethod = "GET",
                Path = "/api/files/path%2Fto%2Ffile.txt"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (RequestInfo)serializer.Deserialize(json, typeof(RequestInfo));

            // Assert
            Assert.Equal("/api/files/path%2Fto%2Ffile.txt", deserialized.Path);
        }
    }
}
