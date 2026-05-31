using System;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class EventSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"type":"log","source":"app","date":"0001-01-01T00:00:00+00:00","tags":[],"message":null,"geo":null,"value":null,"count":null,"data":{},"reference_id":null}""";
        /* lang=json */
        private const string CompleteJson = """{"type":"log","source":"SampleApp","date":"2023-05-02T14:30:00+00:00","tags":["Critical","tag2"],"message":"An error occurred","geo":"40.7128,-74.0060","value":42.0,"count":2,"data":{"FirstName":"Blake","@level":"Warn","@trace":["log 1"],"@user_description":{"email_address":"test@example.com","description":"Test user description","data":{}}},"reference_id":"ref123"}""";
        /* lang=json */
        private const string ErrorTypeJson = """{"type":"error","source":"app","date":"0001-01-01T00:00:00+00:00","tags":[],"message":null,"geo":null,"value":null,"count":null,"data":{},"reference_id":null}""";
        /* lang=json */
        private const string UsageTypeJson = """{"type":"usage","source":"app","date":"0001-01-01T00:00:00+00:00","tags":[],"message":null,"geo":null,"value":null,"count":null,"data":{},"reference_id":null}""";
        /* lang=json */
        private const string LogTypeJson = """{"type":"log","source":"app","date":"0001-01-01T00:00:00+00:00","tags":[],"message":null,"geo":null,"value":null,"count":null,"data":{},"reference_id":null}""";
        /* lang=json */
        private const string NotFoundTypeJson = """{"type":"404","source":"app","date":"0001-01-01T00:00:00+00:00","tags":[],"message":null,"geo":null,"value":null,"count":null,"data":{},"reference_id":null}""";
        /* lang=json */
        private const string SessionTypeJson = """{"type":"session","source":"app","date":"0001-01-01T00:00:00+00:00","tags":[],"message":null,"geo":null,"value":null,"count":null,"data":{},"reference_id":null}""";
        /* lang=json */
        private const string TaggedJson = """{"type":"log","source":"app","date":"0001-01-01T00:00:00+00:00","tags":["Critical"],"message":null,"geo":null,"value":null,"count":null,"data":{},"reference_id":null}""";
        /* lang=json */
        private const string DecimalPrecisionJson = """{"type":"log","source":"app","date":"0001-01-01T00:00:00+00:00","tags":[],"message":null,"geo":null,"value":123.456789,"count":null,"data":{},"reference_id":null}""";
        /* lang=json */
        private const string TraceLogJson = """["log 1"]""";
        /* lang=json */
        private const string UserDescriptionJson = """{"email_address":"test@example.com","description":"Test user description","data":{}}""";

        [Fact]
        public void Serialize_MinimalEvent_ProducesCorrectJson() {
            // Arrange
            var model = CreateMinimalEvent();

            // Act
            string json = Serialize(model);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteEvent_ProducesCorrectJson() {
            // Arrange
            var model = CreateCompleteEvent();

            // Act
            string json = Serialize(model);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_Event_RoundTrips() {
            // Arrange
            var model = new Event {
                Type = Event.KnownTypes.Log,
                Source = "app",
                Tags = new TagSet { Event.KnownTags.Critical },
                Message = "Message",
                Geo = "40.7128,-74.0060",
                Value = 12.5m,
                Count = 2,
                ReferenceId = "ref-1",
                Data = {
                    [Event.KnownDataKeys.Level] = "Warn",
                    [Event.KnownDataKeys.Version] = "1.2.3",
                    ["FirstName"] = "Blake"
                }
            };

            // Act
            Event roundTripped = RoundTrip(model);

            // Assert
            Assert.Equal(Event.KnownTypes.Log, roundTripped.Type);
            Assert.Equal("app", roundTripped.Source);
            Assert.Contains(Event.KnownTags.Critical, roundTripped.Tags);
            Assert.Equal("Message", roundTripped.Message);
            Assert.Equal("40.7128,-74.0060", roundTripped.Geo);
            Assert.Equal(12.5m, roundTripped.Value);
            Assert.Equal(2, roundTripped.Count);
            Assert.Equal("ref-1", roundTripped.ReferenceId);
            Assert.Equal("Warn", roundTripped.Data[Event.KnownDataKeys.Level]);
            Assert.Equal("1.2.3", roundTripped.Data[Event.KnownDataKeys.Version]);
            Assert.Equal("Blake", roundTripped.Data["FirstName"]);
        }

        [Fact]
        public void Deserialize_Event_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            Event model = Deserialize<Event>(json);

            // Assert
            Assert.Equal(Event.KnownTypes.Log, model.Type);
            Assert.Equal("SampleApp", model.Source);
            Assert.Equal(new DateTimeOffset(2023, 5, 2, 14, 30, 0, TimeSpan.Zero), model.Date);
            Assert.Contains(Event.KnownTags.Critical, model.Tags);
            Assert.Contains("tag2", model.Tags);
            Assert.Equal("An error occurred", model.Message);
            Assert.Equal("40.7128,-74.0060", model.Geo);
            Assert.Equal(42.0m, model.Value);
            Assert.Equal(2, model.Count);
            Assert.Equal("Blake", model.Data["FirstName"]);
            Assert.Equal("Warn", model.Data[Event.KnownDataKeys.Level]);
            Assert.Equal(TraceLogJson, model.Data[Event.KnownDataKeys.TraceLog]);
            Assert.Equal(UserDescriptionJson, model.Data[Event.KnownDataKeys.UserDescription]);
            Assert.Equal("ref123", model.ReferenceId);
        }

        [Theory]
        [InlineData(Event.KnownTypes.Error)]
        [InlineData(Event.KnownTypes.FeatureUsage)]
        [InlineData(Event.KnownTypes.Log)]
        [InlineData(Event.KnownTypes.NotFound)]
        [InlineData(Event.KnownTypes.Session)]
        public void Serialize_EventKnownType_ProducesCorrectJson(string eventType) {
            // Arrange
            var model = new Event {
                Type = eventType,
                Source = "app"
            };

            // Act
            string json = Serialize(model);

            // Assert
            string expected = eventType switch {
                Event.KnownTypes.Error => ErrorTypeJson,
                Event.KnownTypes.FeatureUsage => UsageTypeJson,
                Event.KnownTypes.Log => LogTypeJson,
                Event.KnownTypes.NotFound => NotFoundTypeJson,
                Event.KnownTypes.Session => SessionTypeJson,
                _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
            };

            Assert.Equal(expected, json);
        }

        [Fact]
        public void Serialize_EventKnownDataKeys_MatchExpectedValues() {
            // Arrange
            var knownDataKeys = new[] {
                Event.KnownDataKeys.Error,
                Event.KnownDataKeys.UserInfo,
                Event.KnownDataKeys.RequestInfo,
                Event.KnownDataKeys.EnvironmentInfo,
                Event.KnownDataKeys.UserDescription,
                Event.KnownDataKeys.ManualStackingInfo,
                Event.KnownDataKeys.Version,
                Event.KnownDataKeys.Level,
                Event.KnownDataKeys.SubmissionMethod
            };

            // Act
            string actual = string.Join(",", knownDataKeys);

            // Assert
            Assert.Equal("@error,@user,@request,@environment,@user_description,@stack,@version,@level,@submission_method", actual);
        }

        [Fact]
        public void Serialize_EventWithTags_ProducesCorrectJson() {
            // Arrange
            var model = CreateMinimalEvent();
            model.Tags.Add(Event.KnownTags.Critical);

            // Act
            string json = Serialize(model);

            // Assert
            Assert.Equal(TaggedJson, json);
        }

        [Fact]
        public void Serialize_EventDecimalValue_ProducesCorrectJson() {
            // Arrange
            var model = CreateMinimalEvent();
            model.Value = 123.456789m;

            // Act
            string json = Serialize(model);

            // Assert
            Assert.Equal(DecimalPrecisionJson, json);
        }

        private static Event CreateMinimalEvent() {
            return new Event {
                Type = Event.KnownTypes.Log,
                Source = "app"
            };
        }

        private static Event CreateCompleteEvent() {
            return new Event {
                Type = Event.KnownTypes.Log,
                Source = "SampleApp",
                Date = new DateTimeOffset(2023, 5, 2, 14, 30, 0, TimeSpan.Zero),
                Tags = new TagSet { Event.KnownTags.Critical, "tag2" },
                Message = "An error occurred",
                Geo = "40.7128,-74.0060",
                Value = 42.0m,
                Count = 2,
                Data = {
                    ["FirstName"] = "Blake",
                    [Event.KnownDataKeys.Level] = "Warn",
                    [Event.KnownDataKeys.TraceLog] = new[] { "log 1" },
                    [Event.KnownDataKeys.UserDescription] = new UserDescription {
                        EmailAddress = "test@example.com",
                        Description = "Test user description"
                    }
                },
                ReferenceId = "ref123"
            };
        }
    }
}
