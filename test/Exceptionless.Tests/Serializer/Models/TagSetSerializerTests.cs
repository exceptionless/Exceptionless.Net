using Exceptionless.Models;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class TagSetSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_EmptyTagSet_ProducesEmptyArray() {
            // Arrange
            var tags = new TagSet();
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(tags);

            // Assert
            Assert.Equal("[]", json);
        }

        [Fact]
        public void Serialize_SingleTag_ProducesArrayWithOneElement() {
            // Arrange
            var tags = new TagSet();
            tags.Add("Critical");
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(tags);

            // Assert
            Assert.Equal("[\"Critical\"]", json);
        }

        [Fact]
        public void Serialize_MultipleTags_ProducesArray() {
            // Arrange
            var tags = new TagSet();
            tags.Add("Critical");
            tags.Add("Production");
            tags.Add("API");
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(tags);

            // Assert
            Assert.Contains("\"Critical\"", json);
            Assert.Contains("\"Production\"", json);
            Assert.Contains("\"API\"", json);
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllTags() {
            // Arrange
            var serializer = GetSerializer();
            var original = new TagSet();
            original.Add("tag1");
            original.Add("tag2");
            original.Add("tag3");

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (TagSet)serializer.Deserialize(json, typeof(TagSet));

            // Assert
            Assert.Equal(3, deserialized.Count);
            Assert.Contains("tag1", deserialized);
            Assert.Contains("tag2", deserialized);
            Assert.Contains("tag3", deserialized);
        }

        [Fact]
        public void Deserialize_FromJsonArray_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            string json = "[\"Error\",\"Startup\",\"Critical\"]";

            // Act
            var deserialized = (TagSet)serializer.Deserialize(json, typeof(TagSet));

            // Assert
            Assert.Equal(3, deserialized.Count);
            Assert.Contains("Error", deserialized);
            Assert.Contains("Startup", deserialized);
            Assert.Contains("Critical", deserialized);
        }

        [Fact]
        public void Serialize_TagSetIsCaseInsensitive_NoDuplicates() {
            // Arrange
            var tags = new TagSet();
            tags.Add("Critical");
            tags.Add("critical");  // Should not add duplicate (case insensitive)
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(tags);

            // Assert - Only one tag due to case-insensitive HashSet
            Assert.Equal("[\"Critical\"]", json);
        }

        [Fact]
        public void Serialize_TagWithSpecialCharacters_EscapesCorrectly() {
            // Arrange
            var tags = new TagSet();
            tags.Add("tag-with-dashes");
            tags.Add("tag.with.dots");
            tags.Add("tag:with:colons");
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(tags);
            var deserialized = (TagSet)serializer.Deserialize(json, typeof(TagSet));

            // Assert
            Assert.Contains("tag-with-dashes", deserialized);
            Assert.Contains("tag.with.dots", deserialized);
            Assert.Contains("tag:with:colons", deserialized);
        }
    }
}
