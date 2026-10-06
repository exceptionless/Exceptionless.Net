using System;
using System.Collections.Generic;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Xunit;

namespace Exceptionless.Tests.Configuration {
    public class DataExclusionTests {
        [Fact]
        public void WillHandleNullKey() {
            var ev = new Event();
            Assert.Throws<ArgumentNullException>(() => ev.SetProperty(null, "test"));
        }
        
        [Fact]
        public void WillIgnoreNullValue() {
            var ev = new Event();
            ev.SetProperty("test", null);
            Assert.Empty(ev.Data);
        }
        
        [Fact]
        public void WillIgnoreExcludedKey() {
            var ev = new Event();
            ev.SetProperty("Credit Card Number", "test", excludedPropertyNames: new [] { "Credit Card Number" });
            Assert.Empty(ev.Data);
        }
        
        [Fact]
        public void CanHandleSortedList() {
            var values = new SortedList<string, string> {
                { "apple", "test" },
                { "Credit Card Number", "4444 4444 4444 4444" }
            };
            
            var ev = new Event();
            ev.SetProperty(nameof(values), values, excludedPropertyNames: new [] { "Credit Card Number" });
            Assert.Single(ev.Data);
            Assert.Contains(nameof(values), ev.Data.Keys);
            Assert.Equal("{\"apple\":\"test\"}", ev.Data.GetString(nameof(values)));
        }

        [Fact]
        public void CanHandleDictionary() {
            var values = new Dictionary<string, string> {
                { "apple", "test" },
                { "Credit Card Number", "4444 4444 4444 4444" }
            };
            
            var ev = new Event();
            ev.SetProperty(nameof(values), values, excludedPropertyNames: new [] { "Credit Card Number" });
            Assert.Single(ev.Data);
            Assert.Contains(nameof(values), ev.Data.Keys);
            Assert.Equal("{\"apple\":\"test\"}", ev.Data.GetString(nameof(values)));
        }
        
        [Fact]
        public void CanHandleObject() {
            var order = new Order {
                Id = "1234",
                CardLast4 = "4444",
                Data = new Dictionary<string, string> {
                    { nameof(Order.CardLast4), "5555" }
                }
            };
            
            var ev = new Event();
            ev.SetProperty(nameof(order), order, excludedPropertyNames: new [] { nameof(order.CardLast4) });
            Assert.Single(ev.Data);
            Assert.Equal("{\"id\":\"1234\",\"data\":{}}", ev.Data.GetString(nameof(order)));
        }
        
        [InlineData("Credit*", true)]
        [InlineData("*Number*", true)]
        [InlineData("Credit Card", false)]
        [InlineData("Credit Card Number", true)]
        [Theory]
        public void CanCheckWildCardMatches(string exclusion, bool isMatch) {
            const string key = "Credit Card Number";
            Assert.True(key.AnyWildcardMatches(new []{ exclusion }) == isMatch);
        }

        public class Order {
            public string Id { get; set; }
            public string CardLast4 { get; set; }
            public IDictionary<string, string> Data { get; set; }
        }
    }
}