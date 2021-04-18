using System;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Exceptionless.Performance.Tests
{
    class Program
    {
        static void Main(string[] args) {
            var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(args);
        }
    }
}
