using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using BenchmarkDotNet.Attributes;

namespace Exceptionless.Performance.Tests {
    [MemoryDiagnoser]
    public class SnakeCaseNaming {
        private const int Iterations = 5;

        [ParamsSource(nameof(Words))]
        public string Word { get; set; }

        public IEnumerable<string> Words => new[]
        {
            "z",
            "A",
            "AA",
            "zz",
            "AzA",
            "az_A",
            "azzAzzAzzAzz",
            "azzzAzzzAzzzAzzzAzzzAzzzAzzz"
        };

        [GlobalSetup]
        public void Verify() {
            var value0 = ToLowerUnderscoredWords(Word);
            var value1 = ToLowerUnderscoredWordsCreate(Word, new StringBuilder());
            var value2 = ToLowerUnderscoredWordsCached(Word);

            if (value0 != value1) throw new InvalidOperationException($"{value0} != {value1}");
            if (value1 != value2) throw new InvalidOperationException($"{value1} != {value2}");
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Iterations)]
        public string SnakeCase() {
            string value = "";
            for (int i = 0; i < Iterations; i++) {
                value = Operation(value);
            }

            return value;

            [MethodImpl(MethodImplOptions.NoInlining)]
            string Operation(string previous) {
                return ToLowerUnderscoredWords(Word);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public string StringBuilderCached() {
            string value = "";
            for (int i = 0; i < Iterations; i++) {
                value = Operation(value);
            }

            return value;

            [MethodImpl(MethodImplOptions.NoInlining)]
            string Operation(string previous) {
                var name = Word;
                ref var builder = ref s_builder;
                var sb = builder ?? new StringBuilder(name.Length + 5);
                builder = null;
                name = ToLowerUnderscoredWordsCreate(name, sb);

                if (value.Length <= 100) {
                    sb.Clear();
                    builder = sb;
                }

                return name;
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public string ConversionCached() {
            string value = "";
            for (int i = 0; i < Iterations; i++) {
                value = Operation(value);
            }

            return value;

            [MethodImpl(MethodImplOptions.NoInlining)]
            string Operation(string previous) {
                return ToLowerUnderscoredWordsCached(Word);
            }
        }

        public static string ToLowerUnderscoredWords(string value) {
            var builder = new StringBuilder(value.Length + 10);
            for (int index = 0; index < value.Length; index++) {
                char c = value[index];
                if (Char.IsUpper(c)) {
                    if (index > 0 && value[index - 1] != '_')
                        builder.Append('_');

                    builder.Append(Char.ToLower(c));
                }
                else {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static readonly ConcurrentDictionary<string, string> s_lookUp = new();
        [ThreadStatic] private static StringBuilder? s_builder;

        public static string ToLowerUnderscoredWordsCached(string name) {
            if (string.IsNullOrEmpty(name)) return name;

            var lookUp = s_lookUp;
            if (lookUp.TryGetValue(name, out string value)) return value;

            ref var builder = ref s_builder;
            var sb = builder ?? new StringBuilder(name.Length + 5);
            builder = null;

            value = ToLowerUnderscoredWordsCreate(name, sb);
            lookUp[name] = value;

            if (value.Length <= 100) {
                sb.Clear();
                builder = sb;
            }

            return value;
        }

        public static string ToLowerUnderscoredWordsCreate(string value, StringBuilder builder) {
            for (int index = 0; index < value.Length; index++) {
                char c = value[index];
                if (Char.IsUpper(c)) {
                    if (index > 0 && value[index - 1] != '_')
                        builder.Append('_');

                    builder.Append(Char.ToLower(c));
                }
                else {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
