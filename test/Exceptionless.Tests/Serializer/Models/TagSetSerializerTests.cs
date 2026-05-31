using Exceptionless.Models;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class TagSetSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """[]""";
        /* lang=json */
        private const string CompleteJson = """["Critical"]""";
        /* lang=json */
        private const string KnownJson = """["alpha","beta"]""";

        [Fact]
        public void Serialize_MinimalTagSet_ProducesCorrectJson() {
            // Arrange
            var tags = new TagSet();

            // Act
            string json = Serialize(tags);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteTagSet_ProducesCorrectJson() {
            // Arrange
            var tags = new TagSet { "Critical" };

            // Act
            string json = Serialize(tags);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_TagSet_RoundTrips() {
            // Arrange
            var tags = new TagSet { "Critical" };

            // Act
            TagSet roundTripped = RoundTrip(tags);

            // Assert
            Assert.Single(roundTripped);
            Assert.Contains("Critical", roundTripped);
        }

        [Fact]
        public void Deserialize_TagSet_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = KnownJson;

            // Act
            TagSet tags = Deserialize<TagSet>(json);

            // Assert
            Assert.Equal(2, tags.Count);
            Assert.Contains("alpha", tags);
            Assert.Contains("beta", tags);
        }
    }
}
