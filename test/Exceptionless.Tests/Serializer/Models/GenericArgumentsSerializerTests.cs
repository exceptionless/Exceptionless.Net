using Exceptionless.Models;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class GenericArgumentsSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """[]""";
        /* lang=json */
        private const string CompleteJson = """["T","U"]""";

        [Fact]
        public void Serialize_MinimalGenericArguments_ProducesCorrectJson() {
            // Arrange
            var arguments = new GenericArguments();

            // Act
            string json = Serialize(arguments);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteGenericArguments_ProducesCorrectJson() {
            // Arrange
            var arguments = new GenericArguments { "T", "U" };

            // Act
            string json = Serialize(arguments);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_GenericArguments_RoundTrips() {
            // Arrange
            var arguments = new GenericArguments { "T", "U" };

            // Act
            GenericArguments roundTripped = RoundTrip(arguments);

            // Assert
            Assert.Equal(2, roundTripped.Count);
            Assert.Equal("T", roundTripped[0]);
            Assert.Equal("U", roundTripped[1]);
        }

        [Fact]
        public void Deserialize_GenericArguments_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            GenericArguments arguments = Deserialize<GenericArguments>(json);

            // Assert
            Assert.Equal(2, arguments.Count);
            Assert.Equal("T", arguments[0]);
            Assert.Equal("U", arguments[1]);
        }
    }
}
