using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class UserInfoSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteUserInfo_ProducesSnakeCaseJson() {
            // Arrange
            var userInfo = new UserInfo("user@example.com", "Test User") {
                Data = {
                    { "Role", "Admin" },
                    { "Plan", "Enterprise" }
                }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(userInfo);

            // Assert
            SerializerContractAssertions.IncludesProperties(json, "identity", "name", "data");
            SerializerContractAssertions.ExcludesProperties(json, "Identity", "Name", "Data");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new UserInfo("user123", "John Doe") {
                Data = {
                    { "Age", 30 },
                    { "City", "New York" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (UserInfo)serializer.Deserialize(json, typeof(UserInfo));

            // Assert
            Assert.Equal("user123", deserialized.Identity);
            Assert.Equal("John Doe", deserialized.Name);
            Assert.NotNull(deserialized.Data);
            Assert.Equal(2, deserialized.Data.Count);
        }

        [Fact]
        public void Deserialize_WithIdentityOnly_PreservesIdentity() {
            // Arrange
            var serializer = GetSerializer();
            var original = new UserInfo("anonymous@test.com");

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (UserInfo)serializer.Deserialize(json, typeof(UserInfo));

            // Assert
            Assert.Equal("anonymous@test.com", deserialized.Identity);
            Assert.Null(deserialized.Name);
        }

        [Fact]
        public void Deserialize_WithEmptyData_PreservesEmptyDictionary() {
            // Arrange
            var serializer = GetSerializer();
            var original = new UserInfo("test@example.com", "Test");

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (UserInfo)serializer.Deserialize(json, typeof(UserInfo));

            // Assert
            Assert.NotNull(deserialized.Data);
            Assert.Empty(deserialized.Data);
        }

        [Fact]
        public void Deserialize_WithSpecialCharacters_PreservesCharacters() {
            // Arrange
            var serializer = GetSerializer();
            var original = new UserInfo("user+tag@example.com", "O'Brien, John Jr.");

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (UserInfo)serializer.Deserialize(json, typeof(UserInfo));

            // Assert
            Assert.Equal("user+tag@example.com", deserialized.Identity);
            Assert.Equal("O'Brien, John Jr.", deserialized.Name);
        }

        [Fact]
        public void Deserialize_WithUnicodeCharacters_PreservesUnicode() {
            // Arrange
            var serializer = GetSerializer();
            var original = new UserInfo("用户@example.com", "日本語ユーザー");

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (UserInfo)serializer.Deserialize(json, typeof(UserInfo));

            // Assert
            Assert.Equal("用户@example.com", deserialized.Identity);
            Assert.Equal("日本語ユーザー", deserialized.Name);
        }

        [Fact]
        public void Deserialize_FromSnakeCaseJson_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{\"identity\":\"parsed@example.com\",\"name\":\"Parsed User\",\"data\":{\"extra\":\"data\"}}";

            // Act
            var result = (UserInfo)serializer.Deserialize(json, typeof(UserInfo));

            // Assert
            Assert.Equal("parsed@example.com", result.Identity);
            Assert.Equal("Parsed User", result.Name);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public void Serialize_TrimsWhitespace_OnConstruction() {
            // Arrange
            var userInfo = new UserInfo("  user@test.com  ", "  John Doe  ");
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(userInfo);

            // Assert
            Assert.Contains("\"identity\":\"user@test.com\"", json);
            Assert.Contains("\"name\":\"John Doe\"", json);
        }
    }
}
