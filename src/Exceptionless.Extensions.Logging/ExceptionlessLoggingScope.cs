using System;
using System.Threading;

namespace Exceptionless.Extensions.Logging
{
    internal class ExceptionlessLoggingScope : IDisposable
    {
        private static AsyncLocal<ExceptionlessLoggingScope> _Current = new AsyncLocal<ExceptionlessLoggingScope>();
        public static ExceptionlessLoggingScope Current
        {
            get
            {
                return _Current.Value;
            }

            set
            {
                _Current.Value = value;
            }

        }

        public static void Push(ExceptionlessLoggingScope scope)
        {
            var temp = Current;
            Current = scope;
            Current.Parent = temp;
        }

        public string Description { get; private set; }

        public string Id { get; private set; }

        public ExceptionlessLoggingScope Parent { get; private set; }

        public ExceptionlessLoggingScope(string description)
        {
            Description = description;
            Id = Guid.NewGuid().ToString();
        }

        public void Dispose()
        {
            Current = Current.Parent;
        }
    }
}
