using System.Collections.Generic;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class RequestInfoSerializerTests : SerializerTestBase {
        private const string MinimalJson = /* lang=json */ """{"user_agent":null,"http_method":null,"is_secure":false,"host":null,"port":0,"path":null,"referrer":null,"client_ip_address":null,"headers":{},"cookies":{},"post_data":null,"query_string":{},"data":{}}""";
        private const string CompleteJson = /* lang=json */ """{"user_agent":"Mozilla/5.0","http_method":"GET","is_secure":true,"host":"www.example.com","port":443,"path":"/test","referrer":"https://www.google.com","client_ip_address":"192.168.1.1","headers":{"Content-Type":["application/json"]},"cookies":{"session":"abc123"},"post_data":null,"query_string":{"q":"test"},"data":{"@browser":"Mozilla Firefox","@browser_version":"97.0","@browser_major_version":"97","@device":"Desktop","@os":"Windows","@os_version":"10.0","@os_major_version":"10","@is_bot":"False"}}""";
        private const string KnownDataKeysJson = /* lang=json */ """{"user_agent":null,"http_method":null,"is_secure":false,"host":null,"port":0,"path":null,"referrer":null,"client_ip_address":null,"headers":{},"cookies":{},"post_data":null,"query_string":{},"data":{"@browser":"Firefox","@browser_version":"120.0","@browser_major_version":"120","@device":"Desktop","@os":"Windows","@os_version":"11.0","@os_major_version":"11","@is_bot":"False"}}""";
        private const string PostDataObjectJson = /* lang=json */ """{"user_agent":null,"http_method":null,"is_secure":false,"host":null,"port":0,"path":null,"referrer":null,"client_ip_address":null,"headers":{},"cookies":{},"post_data":{"Name":"Test","Value":42},"query_string":{},"data":{}}""";
        private const string ExpectedPostData = /* lang=json */ """
{
  "Name": "Test",
  "Value": 42
}
""";

        [Fact]
        public void Serialize_MinimalRequestInfo_ProducesCorrectJson() {
            // Arrange
            var requestInfo = new RequestInfo();

            // Act
            string json = Serialize(requestInfo);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteRequestInfo_ProducesCorrectJson() {
            // Arrange
            var requestInfo = CreateCompleteRequestInfo();

            // Act
            string json = Serialize(requestInfo);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_RequestInfo_RoundTrips() {
            // Arrange
            var requestInfo = CreateCompleteRequestInfo();

            // Act
            RequestInfo roundTripped = RoundTrip(requestInfo);

            // Assert
            Assert.Equal("Mozilla/5.0", roundTripped.UserAgent);
            Assert.Equal("GET", roundTripped.HttpMethod);
            Assert.True(roundTripped.IsSecure);
            Assert.Equal("www.example.com", roundTripped.Host);
            Assert.Equal(443, roundTripped.Port);
            Assert.Equal("/test", roundTripped.Path);
            Assert.Equal("https://www.google.com", roundTripped.Referrer);
            Assert.Equal("192.168.1.1", roundTripped.ClientIpAddress);
            Assert.Equal("application/json", roundTripped.Headers["Content-Type"][0]);
            Assert.Equal("abc123", roundTripped.Cookies["session"]);
            Assert.Null(roundTripped.PostData);
            Assert.Equal("test", roundTripped.QueryString["q"]);
            Assert.Equal("Mozilla Firefox", roundTripped.Data[RequestInfo.KnownDataKeys.Browser]);
            Assert.Equal("False", roundTripped.Data[RequestInfo.KnownDataKeys.IsBot]);
        }

        [Fact]
        public void Deserialize_RequestInfo_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            RequestInfo requestInfo = Deserialize<RequestInfo>(json);

            // Assert
            Assert.Equal("Mozilla/5.0", requestInfo.UserAgent);
            Assert.Equal("GET", requestInfo.HttpMethod);
            Assert.True(requestInfo.IsSecure);
            Assert.Equal("www.example.com", requestInfo.Host);
            Assert.Equal(443, requestInfo.Port);
            Assert.Equal("/test", requestInfo.Path);
            Assert.Equal("https://www.google.com", requestInfo.Referrer);
            Assert.Equal("192.168.1.1", requestInfo.ClientIpAddress);
            Assert.Equal("application/json", requestInfo.Headers["Content-Type"][0]);
            Assert.Equal("abc123", requestInfo.Cookies["session"]);
            Assert.Null(requestInfo.PostData);
            Assert.Equal("test", requestInfo.QueryString["q"]);
            Assert.Equal("Mozilla Firefox", requestInfo.Data[RequestInfo.KnownDataKeys.Browser]);
            Assert.Equal("97.0", requestInfo.Data[RequestInfo.KnownDataKeys.BrowserVersion]);
            Assert.Equal("False", requestInfo.Data[RequestInfo.KnownDataKeys.IsBot]);
        }

        [Fact]
        public void Deserialize_RequestInfoObjectPostData_ProducesIndentedString() {
            // Arrange
            const string json = PostDataObjectJson;

            // Act
            RequestInfo requestInfo = Deserialize<RequestInfo>(json);

            // Assert
            Assert.Equal(ExpectedPostData, requestInfo.PostData);
        }

        [Fact]
        public void Serialize_RequestInfoKnownDataKeys_ProducesCorrectJson() {
            // Arrange
            var requestInfo = new RequestInfo {
                Data = {
                    [RequestInfo.KnownDataKeys.Browser] = "Firefox",
                    [RequestInfo.KnownDataKeys.BrowserVersion] = "120.0",
                    [RequestInfo.KnownDataKeys.BrowserMajorVersion] = "120",
                    [RequestInfo.KnownDataKeys.Device] = "Desktop",
                    [RequestInfo.KnownDataKeys.OS] = "Windows",
                    [RequestInfo.KnownDataKeys.OSVersion] = "11.0",
                    [RequestInfo.KnownDataKeys.OSMajorVersion] = "11",
                    [RequestInfo.KnownDataKeys.IsBot] = "False"
                }
            };

            // Act
            string json = Serialize(requestInfo);

            // Assert
            Assert.Equal(KnownDataKeysJson, json);
        }

        private static RequestInfo CreateCompleteRequestInfo() {
            return new RequestInfo {
                UserAgent = "Mozilla/5.0",
                HttpMethod = "GET",
                IsSecure = true,
                Host = "www.example.com",
                Port = 443,
                Path = "/test",
                Referrer = "https://www.google.com",
                ClientIpAddress = "192.168.1.1",
                Headers = new Dictionary<string, string[]> {
                    ["Content-Type"] = new[] { "application/json" }
                },
                Cookies = new Dictionary<string, string> {
                    ["session"] = "abc123"
                },
                QueryString = new Dictionary<string, string> {
                    ["q"] = "test"
                },
                Data = {
                    [RequestInfo.KnownDataKeys.Browser] = "Mozilla Firefox",
                    [RequestInfo.KnownDataKeys.BrowserVersion] = "97.0",
                    [RequestInfo.KnownDataKeys.BrowserMajorVersion] = "97",
                    [RequestInfo.KnownDataKeys.Device] = "Desktop",
                    [RequestInfo.KnownDataKeys.OS] = "Windows",
                    [RequestInfo.KnownDataKeys.OSVersion] = "10.0",
                    [RequestInfo.KnownDataKeys.OSMajorVersion] = "10",
                    [RequestInfo.KnownDataKeys.IsBot] = "False"
                }
            };
        }
    }
}
