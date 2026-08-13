using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Models;
using Xunit;

namespace Exceptionless.Tests.Models {
    public class DataDictionaryTests {
        [Fact]
        public void ConstructorAndValueHelpers_WithMixedValues_PreserveDictionarySemantics() {
            // Arrange
            IEnumerable<KeyValuePair<string, object>> nullValues = null;
            var values = new[] {
                new KeyValuePair<string, object>("Name", "value"),
                new KeyValuePair<string, object>("Count", 42)
            };
            int providerCalls = 0;

            // Act
            Exception exception = Record.Exception(() => new DataDictionary(nullValues));
            var data = new DataDictionary(values);
            object name = data.GetValueOrDefault("name");
            object missing = data.GetValueOrDefault("missing");
            object fallback = data.GetValueOrDefault("missing", "fallback");
            object existingProvider = data.GetValueOrDefault("name", () => {
                providerCalls++;
                return "unused";
            });
            object generatedProvider = data.GetValueOrDefault("missing", () => {
                providerCalls++;
                return "generated";
            });
            string nameString = data.GetString("name");
            string countString = data.GetString("count");
            string missingString = data.GetString("missing", "fallback");

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
            Assert.Equal("value", name);
            Assert.Null(missing);
            Assert.Equal("fallback", fallback);
            Assert.Equal("value", existingProvider);
            Assert.Equal("generated", generatedProvider);
            Assert.Equal(1, providerCalls);
            Assert.Equal("value", nameString);
            Assert.Equal(String.Empty, countString);
            Assert.Equal("fallback", missingString);
        }

        [Fact]
        public void GenericDictionaryInterfaces_WithMutations_PreserveCollectionSemantics() {
            // Arrange
            var data = new DataDictionary();
            IDictionary<string, object> dictionary = data;
            ICollection<KeyValuePair<string, object>> collection = data;
            var copy = new KeyValuePair<string, object>[4];

            // Act
            dictionary.Add("one", 1);
            collection.Add(new KeyValuePair<string, object>("two", 2));
            dictionary["three"] = 3;
            int count = collection.Count;
            bool isReadOnly = collection.IsReadOnly;
            string[] keys = dictionary.Keys.ToArray();
            object[] values = dictionary.Values.ToArray();
            bool containsOne = dictionary.ContainsKey("ONE");
            bool foundTwo = dictionary.TryGetValue("two", out object value);
            bool containsThree = collection.Contains(new KeyValuePair<string, object>("three", 3));
            bool containsWrongThree = collection.Contains(new KeyValuePair<string, object>("three", 4));
            collection.CopyTo(copy, 1);
            int genericEnumerationCount = ((IEnumerable<KeyValuePair<string, object>>)data).Count();
            int nonGenericEnumerationCount = ((IEnumerable)data).Cast<object>().Count();
            bool removedWrongThree = collection.Remove(new KeyValuePair<string, object>("three", 4));
            bool removedThree = collection.Remove(new KeyValuePair<string, object>("three", 3));
            bool removedTwo = dictionary.Remove("two");
            collection.Clear();

            // Assert
            Assert.Equal(3, count);
            Assert.False(isReadOnly);
            Assert.Contains("one", keys);
            Assert.Contains(2, values);
            Assert.True(containsOne);
            Assert.True(foundTwo);
            Assert.Equal(2, value);
            Assert.True(containsThree);
            Assert.False(containsWrongThree);
            Assert.Equal(3, copy.Skip(1).Count(entry => entry.Key != null));
            Assert.Equal(3, genericEnumerationCount);
            Assert.Equal(3, nonGenericEnumerationCount);
            Assert.False(removedWrongThree);
            Assert.True(removedThree);
            Assert.True(removedTwo);
            Assert.Empty(data);
        }

        [Fact]
        public void NonGenericDictionaryInterfaces_WithMutations_PreserveCollectionSemantics() {
            // Arrange
            var data = new DataDictionary();
            IDictionary dictionary = data;
            ICollection collection = data;
            var copy = new DictionaryEntry[2];

            // Act
            bool isReadOnly = dictionary.IsReadOnly;
            bool isFixedSize = dictionary.IsFixedSize;
            bool isSynchronized = collection.IsSynchronized;
            object syncRoot = collection.SyncRoot;
            Exception invalidAdd = Record.Exception(() => dictionary.Add(42, "invalid"));
            Exception invalidSet = Record.Exception(() => dictionary[42] = "invalid");
            dictionary.Add("one", 1);
            dictionary["two"] = 2;
            int populatedCount = collection.Count;
            object one = dictionary["ONE"];
            object missing = dictionary["missing"];
            object invalidKey = dictionary[42];
            string[] keys = dictionary.Keys.Cast<string>().ToArray();
            object[] values = dictionary.Values.Cast<object>().ToArray();
            bool containsOne = dictionary.Contains("ONE");
            bool containsInvalidKey = dictionary.Contains(42);
            IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
            bool moved = enumerator.MoveNext();
            object enumeratorKey = enumerator.Key;
            object enumeratorValue = enumerator.Value;
            DictionaryEntry entry = enumerator.Entry;
            object current = enumerator.Current;
            enumerator.Reset();
            collection.CopyTo(copy, 0);
            dictionary.Remove(42);
            int countAfterInvalidRemoval = dictionary.Count;
            dictionary.Remove("one");
            int countAfterRemoval = data.Count;
            dictionary.Clear();

            // Assert
            Assert.False(isReadOnly);
            Assert.False(isFixedSize);
            Assert.False(isSynchronized);
            Assert.Same(data, syncRoot);
            Assert.IsType<ArgumentException>(invalidAdd);
            Assert.IsType<ArgumentException>(invalidSet);
            Assert.Equal(2, populatedCount);
            Assert.Equal(1, one);
            Assert.Null(missing);
            Assert.Null(invalidKey);
            Assert.Contains("one", keys, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(2, values);
            Assert.True(containsOne);
            Assert.False(containsInvalidKey);
            Assert.True(moved);
            Assert.IsType<string>(enumeratorKey);
            Assert.NotNull(enumeratorValue);
            Assert.Equal(entry, current);
            Assert.All(copy, copiedEntry => Assert.IsType<string>(copiedEntry.Key));
            Assert.Equal(2, countAfterInvalidRemoval);
            Assert.Equal(1, countAfterRemoval);
            Assert.Empty(data);
        }
    }
}
