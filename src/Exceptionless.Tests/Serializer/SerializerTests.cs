using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Xunit;
using Exceptionless;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Serializer;

namespace Exceptionless.Tests.Serializer {
    public class SerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void CanSerialize() {
            var data = new SampleModel {
                Date = DateTime.Now,
                Message = "Testing"
            };
            IJsonSerializer serializer = GetSerializer();
            string json = serializer.Serialize(data, new[] { "Date" });
            Assert.Equal(@"{""message"":""Testing""}", json);
        }

        [Fact]
        public void CanSerializeEvent() {
            var ev = new Event {
                Date = DateTime.Now,
                Message = "Testing"
            };
            ev.Data["FirstName"] = "Blake";

            IJsonSerializer serializer = GetSerializer();
            string json = serializer.Serialize(ev, new[] { "Date" });
            Assert.Equal(@"{""message"":""Testing"",""data"":{""FirstName"":""Blake""}}", json);
        }


        [Fact]
        public void CanExcludeProperties() {
            var data = new SampleModel {
                Date = DateTime.Now,
                Message = "Testing"
            };
            IJsonSerializer serializer = GetSerializer();
            string json = serializer.Serialize(data, new[] { "Date" });
            Assert.Equal(@"{""message"":""Testing""}", json);
        }

        [Fact]
        public void CanExcludeNestedProperties() {
            var data = new SampleModel {
                Date = DateTime.Now,
                Message = "Testing",
                Nested = new SampleModel {
                    Date = DateTime.Now,
                    Message = "Nested"
                }
            };
            IJsonSerializer serializer = GetSerializer();
            string json = serializer.Serialize(data, new[] { "Date" });
            Assert.Equal(@"{""message"":""Testing"",""nested"":{""message"":""Nested""}}", json);
        }

        [Fact]
        public void WillIgnoreDefaultValues() {
            var data = new SampleModel {
                Number = 0,
                Bool = false
            };
            IJsonSerializer serializer = GetSerializer();
            string json = serializer.Serialize(data);
            Assert.Equal(@"{}", json);
            var model = serializer.Deserialize<SampleModel>(json);
            Assert.Equal(data.Number, model.Number);
            Assert.Equal(data.Bool, model.Bool);
        }

        [Fact]
        public void CanSetMaxDepth() {
            var data = new SampleModel {
                Message = "Level 1",
                Nested = new SampleModel {
                    Message = "Level 2",
                    Nested = new SampleModel {
                        Message = "Level 3"
                    }
                }
            };
            IJsonSerializer serializer = GetSerializer();
            string json = serializer.Serialize(data, maxDepth: 2);
            Assert.Equal(@"{""message"":""Level 1"",""nested"":{""message"":""Level 2""}}", json);
        }

        [Fact]
        public void WillIgnoreEmptyCollections() {
            var data = new SampleModel {
                Date = DateTime.Now,
                Message = "Testing",
                Collection = new Collection<string>()
            };
            IJsonSerializer serializer = GetSerializer();
            string json = serializer.Serialize(data, new[] { "Date" });
            Assert.Equal(@"{""message"":""Testing""}", json);
        }

        // TODO: Ability to deserialize objects without underscores
        //[Fact]
        public void CanDeserializeDataWithoutUnderscores() {
            const string json = @"{""BlahId"":""Hello""}";
            const string jsonWithUnderScore = @"{""blah_id"":""Hello""}";

            IJsonSerializer serializer = GetSerializer();
            var value = serializer.Deserialize<Blah>(json);
            Assert.Equal("Hello", value.BlahId);

            value = serializer.Deserialize<Blah>(jsonWithUnderScore);
            Assert.Equal("Hello", value.BlahId);

            string serialized = serializer.Serialize(value);
            Assert.Equal(jsonWithUnderScore, serialized);
        }

        [Fact]
        public void WillDeserializeReferenceIds() {
            IJsonSerializer serializer = GetSerializer();
            var ev = (Event)serializer.Deserialize(@"{""reference_id"": ""123"" }", typeof(Event));
            Assert.Equal("123", ev.ReferenceId);
        }

        [Fact]
        public void WillSerializeDeepExceptionWithStackInformation() {
            try {
                try {
                    try {
                        throw new ArgumentException("This is the innermost argument exception", "wrongArg");
                    } catch (Exception e1) {
                        throw new TargetInvocationException("Target invocation exception.", e1);
                    }
                } catch (Exception e2) {
                    throw new TargetInvocationException("Outer Exception. This is some text of the outer exception.", e2);
                }
            } catch (Exception ex) {
                var client = CreateClient();
                var error = ex.ToErrorModel(client);
                var ev = new Event();
                ev.Data[Event.KnownDataKeys.Error] = error;

                IJsonSerializer serializer = GetSerializer();
                string json = serializer.Serialize(ev);
                
                Assert.Contains(String.Format("\"line_number\":{0}", error.Inner.Inner.StackTrace.Single().LineNumber), json);
            }
        }

        private ExceptionlessClient CreateClient() {
            return new ExceptionlessClient(c => {
                c.UseTraceLogger();
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

    public class SampleModel {
        public int Number { get; set; }
        public bool Bool { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public DateTimeOffset DateOffset { get; set; }
        public IDictionary<string, string> Dictionary { get; set; }
        public ICollection<string> Collection { get; set; } 
        public SampleModel Nested { get; set; }
    }
}
