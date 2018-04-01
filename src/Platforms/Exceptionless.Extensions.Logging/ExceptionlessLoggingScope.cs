using System;
using System.Threading;

namespace Exceptionless.Extensions.Logging {
    internal class ExceptionlessLoggingScope : IDisposable {
        private static readonly AsyncLocal<ExceptionlessLoggingScope> _current = new AsyncLocal<ExceptionlessLoggingScope>();

        public static ExceptionlessLoggingScope Current {
            get => _current.Value;
            private set => _current.Value = value;
        }

        public static void Push(ExceptionlessLoggingScope scope) {
            var temp = Current;
            Current = scope;
            Current.Parent = temp;
        }

        public string Description { get; }

        public string Id { get; }

        public ExceptionlessLoggingScope Parent { get; private set; }

        public ExceptionlessLoggingScope(string description) {
            Description = description;
            Id = Guid.NewGuid().ToString();
        }

        public void Dispose() {
            Current = Current.Parent;
        }
    }
}