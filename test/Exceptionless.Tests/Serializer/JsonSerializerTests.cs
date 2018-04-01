using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Xunit;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Serializer;
using Exceptionless.Tests.Log;
using Exceptionless.Tests.Utility;
using Xunit.Abstractions;
using LogLevel = Exceptionless.Logging.LogLevel;

namespace Exceptionless.Tests.Serializer {
    public class JsonSerializerTests {
        private readonly TestOutputWriter _writer;
        public JsonSerializerTests(ITestOutputHelper output) {
            _writer = new TestOutputWriter(output);
        }

        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void CanSerializeEvent() {
            var ev = new Event {
                Date = DateTime.Now,
                Message = "Testing"
            };
            ev.Data["FirstName"] = "Blake";

            var exclusions = new[] { nameof(Event.Type), nameof(Event.Source), "Date", nameof(Event.Geo), nameof(Event.Count), nameof(Event.ReferenceId), nameof(Event.Tags), nameof(Event.Value) };
            var serializer = GetSerializer();
            string json = serializer.Serialize(ev, exclusions);
            Assert.Equal(@"{""message"":""Testing"",""data"":{""FirstName"":""Blake""}}", json);
        }

        [Fact]
        public void CanExcludeProperties() {
            var data = new SampleModel {
                Date = DateTime.Now,
                Message = "Testing"
            };
            var serializer = GetSerializer();
            string json = serializer.Serialize(data, new[] { nameof(SampleModel.Date), nameof(SampleModel.Number), nameof(SampleModel.Bool), nameof(SampleModel.DateOffset), nameof(SampleModel.Collection), nameof(SampleModel.Dictionary), nameof(SampleModel.Nested) });
            Assert.Equal(@"{""message"":""Testing""}", json);
        }

        [Fact]
        public void CanExcludeNestedProperties() {
            var data = new NestedModel {
                Number = 1,
                Message = "Testing",
                Nested = new NestedModel {
                    Message = "Nested",
                    Number = 2
                }
            };

            var serializer = GetSerializer();
            string json = serializer.Serialize(data, new[] { nameof(NestedModel.Number) });
            Assert.Equal(@"{""message"":""Testing"",""nested"":{""message"":""Nested"",""nested"":null}}", json);
        }

        [Fact]
        public void ShouldIncludeNullObjects() {
            var data = new DefaultsModel();
            var serializer = GetSerializer();
            string json = serializer.Serialize(data);
            Assert.Equal(@"{""number"":0,""bool"":false,""message"":null,""collection"":null,""dictionary"":null}", json);
        }

        [Fact]
        public void CanExcludeMultiwordProperties() {
            var user = new User {
                FirstName = "John",
                LastName = "Doe",
                PasswordHash = "1234567890",
                Billing = new BillingInfo {
                    ExpirationMonth = 10,
                    ExpirationYear = 2020,
                    CardNumberRedacted = "1xxxxxxxx89",
                    EncryptedCardNumber = "9876543210"
                }
            };

            var exclusions = new[] { nameof(user.PasswordHash), nameof(user.Billing.CardNumberRedacted), nameof(user.Billing.EncryptedCardNumber) };
            var serializer = GetSerializer();
            string json = serializer.Serialize(user, exclusions, maxDepth: 2);
            Assert.Equal(@"{""first_name"":""John"",""last_name"":""Doe"",""billing"":{""expiration_month"":10,""expiration_year"":2020}}", json);
        }

        [Fact]
        public void ShouldIncludeDefaultValues() {
            var data = new SampleModel();
            var serializer = GetSerializer();
            string json = serializer.Serialize(data, new []{ nameof(SampleModel.Date), nameof(SampleModel.DateOffset) });
            Assert.Equal(@"{""number"":0,""bool"":false,""message"":null,""dictionary"":null,""collection"":null,""nested"":null}", json);
            var model = serializer.Deserialize<SampleModel>(json);
            Assert.Equal(data.Number, model.Number);
            Assert.Equal(data.Bool, model.Bool);
            Assert.Equal(data.Message, model.Message);
            Assert.Equal(data.Collection, model.Collection);
            Assert.Equal(data.Dictionary, model.Dictionary);
            Assert.Equal(data.Nested, model.Nested);
        }

        [Fact]
        public void ShouldSerializeValues() {
            var data = new SampleModel {
                Number = 1,
                Bool = true,
                Message = "test",
                Collection = new List<string> { "one" },
                Dictionary = new Dictionary<string, string> { { "key", "value" } },
                Date = DateTime.MaxValue,
                DateOffset = DateTimeOffset.MaxValue
            };

            var serializer = GetSerializer();
            string json = serializer.Serialize(data);
            Assert.Equal(@"{""number"":1,""bool"":true,""date"":""9999-12-31T23:59:59.9999999"",""message"":""test"",""date_offset"":""9999-12-31T23:59:59.9999999+00:00"",""dictionary"":{""key"":""value""},""collection"":[""one""],""nested"":null}", json);
            var model = serializer.Deserialize<SampleModel>(json);
            Assert.Equal(data.Number, model.Number);
            Assert.Equal(data.Bool, model.Bool);
            Assert.Equal(data.Message, model.Message);
            Assert.Equal(data.Collection, model.Collection);
            Assert.Equal(data.Dictionary, model.Dictionary);
            Assert.Equal(data.Nested, model.Nested);
        }

        [Fact]
        public void CanSetMaxDepth() {
            var data = new NestedModel {
                Message = "Level 1",
                Nested = new NestedModel {
                    Message = "Level 2",
                    Nested = new NestedModel {
                        Message = "Level 3"
                    }
                }
            };
            var serializer = GetSerializer();
            string json = serializer.Serialize(data, new[] { nameof(NestedModel.Number) }, maxDepth: 2);
            Assert.Equal(@"{""message"":""Level 1"",""nested"":{""message"":""Level 2""}}", json);
        }

        [Fact]
        public void WillIgnoreEmptyCollections() {
            var data = new DefaultsModel {
                Message = "Testing",
                Collection = new Collection<string>(),
                Dictionary = new Dictionary<string, string>()
            };
            var serializer = GetSerializer();
            string json = serializer.Serialize(data, new[] { nameof(DefaultsModel.Bool), nameof(DefaultsModel.Number) });
            Assert.Equal(@"{""message"":""Testing""}", json);
        }

        // TODO: Ability to deserialize objects without underscores
        //[Fact]
        private void CanDeserializeDataWithoutUnderscores() {
            const string json = @"{""BlahId"":""Hello""}";
            const string jsonWithUnderScore = @"{""blah_id"":""Hello""}";

            var serializer = GetSerializer();
            var value = serializer.Deserialize<Blah>(json);
            Assert.Equal("Hello", value.BlahId);

            value = serializer.Deserialize<Blah>(jsonWithUnderScore);
            Assert.Equal("Hello", value.BlahId);

            string serialized = serializer.Serialize(value);
            Assert.Equal(jsonWithUnderScore, serialized);
        }

        [Fact]
        public void WillDeserializeReferenceIds() {
            var serializer = GetSerializer();
            var ev = (Event)serializer.Deserialize(@"{""reference_id"": ""123"" }", typeof(Event));
            Assert.Equal("123", ev.ReferenceId);
        }

        [Fact]
        public void WillSerializeDeepExceptionWithStackInformation() {
            try {
                try {
                    try {
                        throw new ArgumentException("This is the innermost argument exception", "wrongArg");
                    }
                    catch (Exception e1) {
                        throw new TargetInvocationException("Target invocation exception.", e1);
                    }
                }
                catch (Exception e2) {
                    throw new TargetInvocationException("Outer Exception. This is some text of the outer exception.", e2);
                }
            }
            catch (Exception ex) {
                var client = CreateClient();
                var error = ex.ToErrorModel(client);
                var ev = new Event();
                ev.Data[Event.KnownDataKeys.Error] = error;

                var serializer = GetSerializer();
                string json = serializer.Serialize(ev);

                Assert.Contains(String.Format("\"line_number\":{0}", error.Inner.Inner.StackTrace.Single().LineNumber), json);
            }
        }

        private ExceptionlessClient CreateClient() {
            return new ExceptionlessClient(c => {
                c.UseLogger(new XunitExceptionlessLog(_writer) { MinimumLogLevel = LogLevel.Trace });
                c.ReadFromAttributes();
                c.UserAgent = "testclient/1.0.0.0";

                // Disable updating settings.
                c.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
            });
        }
    }

    public class Blah {
        public string BlahId { get; set; }
    }

    public class NestedModel {
        public int Number { get; set; }
        public string Message { get; set; }
        public NestedModel Nested { get; set; }
    }

    public class SampleModel {
        public int Number { get; set; }
        public bool Bool { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public DateTimeOffset DateOffset { get; set; }
        public IDictionary<string, string> Dictionary { get; set; }
        public ICollection<string> Collection { get; set; }
        public SampleModel Nested { get; set; }
    }

    public class DefaultsModel {
        public int Number { get; set; }
        public bool Bool { get; set; }
        public string Message { get; set; }
        public ICollection<string> Collection { get; set; }
        public IDictionary<string, string> Dictionary { get; set; }
    }

    public class User {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordHash { get; set; }
        public BillingInfo Billing { get; set; }
    }

    public class BillingInfo {
        public string CardNumberRedacted { get; set; }
        public string EncryptedCardNumber { get; set; }
        public int ExpirationMonth { get; set; }
        public int ExpirationYear { get; set; }
    }
}
