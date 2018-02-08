using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Exceptionless.Extensions.Logging {
    public class ExceptionlessLogger : ILogger {
        private readonly ExceptionlessClient _client;
        private readonly string _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessLogger"/> class.
        /// </summary>
        /// <param name="client">The <see cref="ExceptionlessClient"/> to be used by the logger.</param>
        /// <param name="source">The source to tag events with, typically the category.</param>
        public ExceptionlessLogger(ExceptionlessClient client, string source) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _source = source;
        }

        /// <inheritdoc />
        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>The identifier for the scope.
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="state"></param>
        /// <returns>An <see cref="T:System.IDisposable" /> that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state) {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            // Typically a string will be used as the state for a scope, but the BeginScope extension provides
            // a FormattedLogValues and ASP.NET provides multiple scope objects, so just use ToString()
            string description = state.ToString();

            var scope = new ExceptionlessLoggingScope(description);

            // Add to stack to support nesting within execution context
            ExceptionlessLoggingScope.Push(scope);
            return scope;
        }

        /// <inheritdoc />
        /// <summary>
        /// Checks if the given <see cref="T:Microsoft.Extensions.Logging.LogLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">The level to be checked.</param>
        /// <returns>Returns true if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel) {
            if (logLevel == LogLevel.None || !_client.Configuration.IsValid)
                return false;

            var minLogLevel = _client.Configuration.Settings.GetMinLogLevel(_source);
            return logLevel.ToLogLevel() >= minLogLevel;
        }

        /// <inheritdoc />
        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <see cref="T:System.String" /> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            if (!IsEnabled(logLevel))
                return;

            string message = formatter(state, exception);
            if (String.IsNullOrEmpty(message) && exception == null)
                return;

            var builder = exception == null ? _client.CreateLog(_source, message, logLevel.ToLogLevel()) : _client.CreateException(exception);
            builder.Target.Date = DateTimeOffset.Now;

            if (!String.IsNullOrEmpty(message))
                builder.SetMessage(message);

            if (exception != null)
                builder.SetSource(_source);

            // Add event id, if available
            if (eventId.Id != 0)
                builder.SetProperty("EventId", eventId.Id);

            // If within a scope, add scope's reference id
            if (ExceptionlessLoggingScope.Current != null)
                builder.SetEventReference("Parent", ExceptionlessLoggingScope.Current.Id);

            // The logging framework passes in FormattedLogValues, which implements IEnumerable<KeyValuePair<string, object>>;
            // add each property and value as individual objects for proper visibility in Exceptionless
            if (state is IEnumerable<KeyValuePair<string, object>> stateProps) {
                foreach (var prop in stateProps) {
                    // Logging the message template is superfluous
                    if (prop.Key != "{OriginalFormat}")
                        builder.SetProperty(prop.Key, prop.Value);
                }
            } else {
                // Otherwise, attach the entire object, using its type as the name
                builder.AddObject(state);
            }

            builder.Submit();
        }
    }
}