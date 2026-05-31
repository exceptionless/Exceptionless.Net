using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class UserDescriptionSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"email_address":null,"description":null,"data":{}}""";
        /* lang=json */
        private const string CompleteJson = """{"email_address":"test@example.com","description":"Test user description","data":{}}""";

        [Fact]
        public void Serialize_MinimalUserDescription_ProducesCorrectJson() {
            // Arrange
            var description = new UserDescription();

            // Act
            string json = Serialize(description);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteUserDescription_ProducesCorrectJson() {
            // Arrange
            var description = new UserDescription {
                EmailAddress = "test@example.com",
                Description = "Test user description"
            };

            // Act
            string json = Serialize(description);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_UserDescription_RoundTrips() {
            // Arrange
            var description = new UserDescription {
                EmailAddress = "test@example.com",
                Description = "Test user description"
            };

            // Act
            UserDescription roundTripped = RoundTrip(description);

            // Assert
            Assert.Equal("test@example.com", roundTripped.EmailAddress);
            Assert.Equal("Test user description", roundTripped.Description);
            Assert.Empty(roundTripped.Data);
        }

        [Fact]
        public void Deserialize_UserDescription_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            UserDescription description = Deserialize<UserDescription>(json);

            // Assert
            Assert.Equal("test@example.com", description.EmailAddress);
            Assert.Equal("Test user description", description.Description);
            Assert.Empty(description.Data);
        }
    }
}
