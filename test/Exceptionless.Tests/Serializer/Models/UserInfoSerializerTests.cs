using System;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class UserInfoSerializerTests : SerializerTestBase {
        private const string MinimalJson = /* lang=json */ """{"identity":null,"name":null,"data":{}}""";
        private const string CompleteJson = /* lang=json */ """{"identity":"123","name":"John Doe","data":{"Age":30,"City":"New York"}}""";

        [Fact]
        public void Serialize_MinimalUserInfo_ProducesCorrectJson() {
            // Arrange
            var user = new UserInfo();

            // Act
            string json = Serialize(user);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteUserInfo_ProducesCorrectJson() {
            // Arrange
            var user = new UserInfo {
                Identity = "123",
                Name = "John Doe",
                Data = {
                    ["Age"] = 30,
                    ["City"] = "New York"
                }
            };

            // Act
            string json = Serialize(user);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_UserInfo_RoundTrips() {
            // Arrange
            var user = new UserInfo {
                Identity = "123",
                Name = "John Doe",
                Data = {
                    ["Age"] = 30,
                    ["City"] = "New York"
                }
            };

            // Act
            UserInfo roundTripped = RoundTrip(user);

            // Assert
            Assert.Equal("123", roundTripped.Identity);
            Assert.Equal("John Doe", roundTripped.Name);
            Assert.Equal(30L, Convert.ToInt64(roundTripped.Data["Age"]));
            Assert.Equal("New York", roundTripped.Data["City"]);
        }

        [Fact]
        public void Deserialize_UserInfo_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            UserInfo user = Deserialize<UserInfo>(json);

            // Assert
            Assert.Equal("123", user.Identity);
            Assert.Equal("John Doe", user.Name);
            Assert.Equal(30L, Convert.ToInt64(user.Data["Age"]));
            Assert.Equal("New York", user.Data["City"]);
        }
    }
}
