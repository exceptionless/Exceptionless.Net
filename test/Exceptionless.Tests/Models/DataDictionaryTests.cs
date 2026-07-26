using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Models;
using Xunit;

namespace Exceptionless.Tests.Models {
    public class DataDictionaryTests {
        [Fact]
        public void ConstructorAndValueHelpersPreserveDictionarySemantics() {
            Assert.Throws<ArgumentNullException>(() =>
                new DataDictionary((IEnumerable<KeyValuePair<string, object>>)null));

            var data = new DataDictionary(new[] {
                new KeyValuePair<string, object>("Name", "value"),
                new KeyValuePair<string, object>("Count", 42)
            });
            int providerCalls = 0;

            Assert.Equal("value", data.GetValueOrDefault("name"));
            Assert.Null(data.GetValueOrDefault("missing"));
            Assert.Equal("fallback", data.GetValueOrDefault("missing", "fallback"));
            Assert.Equal("value", data.GetValueOrDefault("name", () => {
                providerCalls++;
                return "unused";
            }));
            Assert.Equal("generated", data.GetValueOrDefault("missing", () => {
                providerCalls++;
                return "generated";
            }));
            Assert.Equal(1, providerCalls);
            Assert.Equal("value", data.GetString("name"));
            Assert.Equal(String.Empty, data.GetString("count"));
            Assert.Equal("fallback", data.GetString("missing", "fallback"));
        }

        [Fact]
        public void GenericDictionaryInterfacesPreserveCollectionSemantics() {
            var data = new DataDictionary();
            IDictionary<string, object> dictionary = data;
            ICollection<KeyValuePair<string, object>> collection = data;

            dictionary.Add("one", 1);
            collection.Add(new KeyValuePair<string, object>("two", 2));
            dictionary["three"] = 3;

            Assert.Equal(3, collection.Count);
            Assert.False(collection.IsReadOnly);
            Assert.Contains("one", dictionary.Keys);
            Assert.Contains(2, dictionary.Values);
            Assert.True(dictionary.ContainsKey("ONE"));
            Assert.True(dictionary.TryGetValue("two", out object value));
            Assert.Equal(2, value);
            Assert.True(collection.Contains(new KeyValuePair<string, object>("three", 3)));
            Assert.False(collection.Contains(new KeyValuePair<string, object>("three", 4)));

            var copy = new KeyValuePair<string, object>[4];
            collection.CopyTo(copy, 1);
            Assert.Equal(3, copy.Skip(1).Count(entry => entry.Key != null));
            Assert.Equal(3, ((IEnumerable<KeyValuePair<string, object>>)data).Count());
            Assert.Equal(3, ((IEnumerable)data).Cast<object>().Count());

            Assert.False(collection.Remove(new KeyValuePair<string, object>("three", 4)));
            Assert.True(collection.Remove(new KeyValuePair<string, object>("three", 3)));
            Assert.True(dictionary.Remove("two"));
            collection.Clear();
            Assert.Empty(data);
        }

        [Fact]
        public void NonGenericDictionaryInterfacesPreserveCollectionSemantics() {
            var data = new DataDictionary();
            IDictionary dictionary = data;
            ICollection collection = data;

            Assert.False(dictionary.IsReadOnly);
            Assert.False(dictionary.IsFixedSize);
            Assert.False(collection.IsSynchronized);
            Assert.Same(data, collection.SyncRoot);
            Assert.Throws<ArgumentException>(() => dictionary.Add(42, "invalid"));
            Assert.Throws<ArgumentException>(() => dictionary[42] = "invalid");

            dictionary.Add("one", 1);
            dictionary["two"] = 2;
            Assert.Equal(2, collection.Count);
            Assert.Equal(1, dictionary["ONE"]);
            Assert.Null(dictionary["missing"]);
            Assert.Null(dictionary[42]);
            Assert.Contains("one", dictionary.Keys.Cast<string>(), StringComparer.OrdinalIgnoreCase);
            Assert.Contains(2, dictionary.Values.Cast<object>());
            Assert.True(dictionary.Contains("ONE"));
            Assert.False(dictionary.Contains(42));

            IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.IsType<string>(enumerator.Key);
            Assert.NotNull(enumerator.Value);
            Assert.Equal(enumerator.Entry, enumerator.Current);
            enumerator.Reset();

            var copy = new DictionaryEntry[2];
            collection.CopyTo(copy, 0);
            Assert.All(copy, entry => Assert.IsType<string>(entry.Key));

            dictionary.Remove(42);
            Assert.Equal(2, dictionary.Count);
            dictionary.Remove("one");
            Assert.Single(data);
            dictionary.Clear();
            Assert.Empty(data);
        }
    }
}
