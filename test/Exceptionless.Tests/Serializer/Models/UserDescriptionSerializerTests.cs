using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class UserDescriptionSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteUserDescription_ProducesSnakeCaseJson() {
            // Arrange
            var desc = new UserDescription {
                EmailAddress = "user@example.com",
                Description = "The app crashed when I clicked submit"
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(desc);

            // Assert
            SerializerContractAssertions.IncludesProperties(json, "email_address", "description", "data");
            SerializerContractAssertions.ExcludesProperties(json, "EmailAddress", "Description");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new UserDescription {
                EmailAddress = "test@example.com",
                Description = "Steps to reproduce: 1. Open page 2. Click button",
                Data = {
                    ["browser"] = "Chrome 120",
                    ["page_url"] = "https://app.example.com/checkout"
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (UserDescription)serializer.Deserialize(json, typeof(UserDescription));

            // Assert
            Assert.Equal("test@example.com", deserialized.EmailAddress);
            Assert.Equal("Steps to reproduce: 1. Open page 2. Click button", deserialized.Description);
            Assert.NotNull(deserialized.Data);
            Assert.Equal(2, deserialized.Data.Count);
        }

        [Fact]
        public void Deserialize_WithMinimalProperties_PreservesDescription() {
            // Arrange
            var serializer = GetSerializer();
            var original = new UserDescription {
                Description = "It broke"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (UserDescription)serializer.Deserialize(json, typeof(UserDescription));

            // Assert
            Assert.Equal("It broke", deserialized.Description);
            Assert.Null(deserialized.EmailAddress);
        }

        [Fact]
        public void Deserialize_FromSnakeCaseJson_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{\"email_address\":\"test@example.org\",\"description\":\"Bug report details\",\"data\":{}}";

            // Act
            var result = (UserDescription)serializer.Deserialize(json, typeof(UserDescription));

            // Assert
            Assert.Equal("test@example.org", result.EmailAddress);
            Assert.Equal("Bug report details", result.Description);
        }

        [Fact]
        public void Deserialize_WithSpecialCharacters_PreservesCharacters() {
            // Arrange
            var serializer = GetSerializer();
            var original = new UserDescription {
                EmailAddress = "user+support@example.com",
                Description = "Error at path \"C:\\Users\\test\" with <html> content"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (UserDescription)serializer.Deserialize(json, typeof(UserDescription));

            // Assert
            Assert.Equal("user+support@example.com", deserialized.EmailAddress);
            Assert.Contains("C:\\Users\\test", deserialized.Description);
        }

        [Fact]
        public void Serialize_TrimsWhitespace_OnConstruction() {
            // Arrange
            var desc = new UserDescription("  test@example.com  ", "  My description  ");
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(desc);

            // Assert
            Assert.Contains("\"email_address\":\"test@example.com\"", json);
            Assert.Contains("\"description\":\"My description\"", json);
        }

        [Fact]
        public void Deserialize_EmptyData_PreservesEmptyDictionary() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{\"email_address\":\"a@b.com\",\"description\":\"test\",\"data\":{}}";

            // Act
            var result = (UserDescription)serializer.Deserialize(json, typeof(UserDescription));

            // Assert
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }
    }
}
