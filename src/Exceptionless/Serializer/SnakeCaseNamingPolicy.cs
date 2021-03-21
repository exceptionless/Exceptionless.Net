using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

using Exceptionless.Extensions;

namespace Exceptionless.Serializer {
    internal class SnakeCaseNamingPolicy : JsonNamingPolicy {
        [ThreadStatic] private static StringBuilder? s_builder;
        private readonly static ConcurrentDictionary<string, string> s_lookUp = new();

        private readonly string[]? _excludedNames;

        public SnakeCaseNamingPolicy(string[]? excludedNames = null) {
            _excludedNames = excludedNames;
        }

        public bool IsNameAllowed(string name) {
            if (_excludedNames is null) return true;
            if (name.AnyWildcardMatches(_excludedNames, true)) return false;

            return true;
        }

        public override string ConvertName(string name) {
            if (string.IsNullOrEmpty(name)) return name;

            var lookUp = s_lookUp;
            if (lookUp.TryGetValue(name, out var value)) return value;

            var span = name.AsSpan();
            int i;
            for (i = 0; i < span.Length; i++) {
                if (char.IsUpper(span[i])) break;
            }

            if (i == span.Length) return name;

            var sb = s_builder ??= new StringBuilder(name.Length + 5);
            try {
                if (i == 0) {
                    sb.Append(char.ToLower(span[0]));
                    i++;
                }

                for (; (uint)i < (uint)span.Length; i++) {
                    char ch = span[i];
                    if (char.IsUpper(ch)) {
                        sb.Append("_");
                        sb.Append(char.ToLower(ch));
                    }
                    else {
                        sb.Append(ch);
                    }
                }

                var newName = sb.ToString();
                lookUp[name] = newName;
                return newName;
            }
            finally {
                if (sb.Length > 100) s_builder = null;
                sb.Clear();
            }
        }
    }
}