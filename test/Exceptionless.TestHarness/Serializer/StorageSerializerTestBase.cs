using System;
using System.Collections.Generic;
using System.IO;
using Exceptionless.Dependency;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Exceptionless.Services;
using Exceptionless.Storage;
using Xunit;

namespace Exceptionless.Tests.Serializer {
    public abstract class StorageSerializerTestBase {
        private readonly IDependencyResolver _resolver;
        public StorageSerializerTestBase() {
            _resolver = new DefaultDependencyResolver();
            _resolver.Register<IObjectStorage, InMemoryObjectStorage>();
            _resolver.Register<IJsonSerializer, DefaultJsonSerializer>();
            _resolver.Register<IExceptionlessLog, InMemoryExceptionlessLog>();
            _resolver.Register<IEnvironmentInfoCollector, DefaultEnvironmentInfoCollector>();
            _resolver.Register<ExceptionlessConfiguration>(new ExceptionlessConfiguration(_resolver));
            Initialize(_resolver);
        }

        protected virtual void Initialize(IDependencyResolver resolver) { }

        protected abstract IStorageSerializer GetSerializer(IDependencyResolver resolver);

        private Event CreateSimpleEvent() {
            var ev= new Event {
                Date = DateTime.Now,
                Message = "Testing",
                Type = Event.KnownTypes.Log,
                Source = "StorageSerializer"
            };
            if (RandomData.GetBool(80))
                ev.Geo = RandomData.GetCoordinate();
            if (RandomData.GetBool(20))
                ev.Value = RandomData.GetDecimal();
            if (RandomData.GetBool(20))
                ev.Count = RandomData.GetInt();
            if (RandomData.GetBool(80))
                ev.ReferenceId = RandomData.GetAlphaNumericString(0,10);
            return ev;
        }

        private void AssertEventSerialize(Event evt) {
            var serializer = GetSerializer(_resolver);
            Event newEvent;
            using (var memory = new MemoryStream()) {
                serializer.Serialize(evt, memory);
                memory.Position = 0;
                newEvent = serializer.Deserialize<Event>(memory);
            }

            var jsonSerializer = _resolver.GetJsonSerializer();
            var expected = jsonSerializer.Serialize(evt);
            var actual = jsonSerializer.Serialize(newEvent);

            Assert.Equal(expected, actual);
        }

        public virtual void CanSerializeSimpleEvent() {
            AssertEventSerialize(CreateSimpleEvent());
        }

        public virtual void CanSerializeSimpleDataValues() {
            var evt = CreateSimpleEvent();
            evt.SetVersion("4.1.1972");
            evt.Data[Event.KnownDataKeys.Level] = "Warn";
            evt.Data[Event.KnownDataKeys.SubmissionMethod] = "test";
            evt.SetEventReference("test",Guid.NewGuid().ToString("N"));
            evt.Data["culture"] = "en-us";
            AssertEventSerialize(evt);
        }

        public virtual void CanSerializeTags() {
            var evt = CreateSimpleEvent();
            evt.AddTags("Critial", "Startup", "AspNetCore");
            AssertEventSerialize(evt);
        }

        public virtual void CanSerializeEnvironmentInfo() {
            var evt = CreateSimpleEvent();
            evt.Data[Event.KnownDataKeys.EnvironmentInfo] = _resolver.Resolve<IEnvironmentInfoCollector>().GetEnvironmentInfo();
            AssertEventSerialize(evt);
        }

        public virtual void CanSerializeRequestInfo() {
            var evt = CreateSimpleEvent();
            evt.AddRequestInfo(new RequestInfo {
                Host = "www.exceptionless.com",
                HttpMethod = "GET",
                QueryString = new Dictionary<string, string> {
                    { "n", "124" },
                    { "q", "advance" }
                },
                Path = "events/123456",
                ClientIpAddress = "10.245.126.2",
                Referrer = "www.google.com",
                IsSecure = true,
                Port = 80,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.75 Safari/537.36",
                Cookies = new Dictionary<string, string> {
                    { "n", "124" },
                    { "q", "advance" }
                }
            });
            AssertEventSerialize(evt);
        }

        public virtual void CanSerializeTraceLogEntries() {
            var evt = CreateSimpleEvent();
            evt.Data[Event.KnownDataKeys.TraceLog] = new List<string> {
                RandomData.GetString(),
                RandomData.GetString()
            };
            AssertEventSerialize(evt);
        }

        public virtual void CanSerializeUserInfo() {
            var evt = CreateSimpleEvent();
            evt.SetUserIdentity("Asp.Net Identity", "exceptionless");
            AssertEventSerialize(evt);
        }

        public virtual void CanSerializeUserDescription() {
            var evt = CreateSimpleEvent();
            evt.SetUserDescription("noreply@exceptionless.com","system account");
            AssertEventSerialize(evt);
        }

        public virtual void CanSerializeManualStackingInfo() {
            var evt = CreateSimpleEvent();
            evt.SetManualStackingInfo("test", new Dictionary<string, string> {
                { "n", "124" },
                { "q", "advance" }
            });
            AssertEventSerialize(evt);
        }

        public virtual void CanSerializeSimpleError() {
            var client = new ExceptionlessClient(new ExceptionlessConfiguration(_resolver));
            var exception = new ArgumentException("The argument cannot be null or empty", "value");

            var evt = CreateSimpleEvent();
            evt.Data[Event.KnownDataKeys.SimpleError] = exception.ToSimpleErrorModel(client);
            AssertEventSerialize(evt);
        }

        public virtual void CanSerializeError() {
            var client = new ExceptionlessClient(new ExceptionlessConfiguration(_resolver));
            var exception = new ArgumentException("The argument cannot be null or empty", "value");

            var evt = CreateSimpleEvent();
            evt.Data[Event.KnownDataKeys.Error] = exception.ToErrorModel(client);
            AssertEventSerialize(evt);
        }
    }
}
