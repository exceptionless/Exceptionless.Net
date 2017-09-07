using Exceptionless.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Exceptionless.Extensions.Logging
{
    public class ExceptionlessLogger : ILogger
    {
        static readonly Dictionary<LogLevel, string> LOG_LEVELS = MapLogLevelsToExceptionlessNames();

        static Dictionary<LogLevel, string> MapLogLevelsToExceptionlessNames()
        {
            Dictionary<LogLevel, string> mappings = new Dictionary<LogLevel, string>(7);
            mappings.Add(LogLevel.Critical, global::Exceptionless.Logging.LogLevel.Fatal.ToString());
            mappings.Add(LogLevel.Debug, global::Exceptionless.Logging.LogLevel.Debug.ToString());
            mappings.Add(LogLevel.Error, global::Exceptionless.Logging.LogLevel.Error.ToString());
            mappings.Add(LogLevel.Information, global::Exceptionless.Logging.LogLevel.Info.ToString());
            mappings.Add(LogLevel.None, global::Exceptionless.Logging.LogLevel.Off.ToString());
            mappings.Add(LogLevel.Trace, global::Exceptionless.Logging.LogLevel.Trace.ToString());
            mappings.Add(LogLevel.Warning, global::Exceptionless.Logging.LogLevel.Warn.ToString());

            return mappings;
        }

        ExceptionlessClient _Client;
        string _Source;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessLogger"/> class.
        /// </summary>
        /// <param name="client">The <see cref="ExceptionlessClient"/> to be used by the logger.</param>
        /// <param name="source">The source to tag events with, typically the category.</param>
        public ExceptionlessLogger(ExceptionlessClient client, string source)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            _Client = client;
            _Source = source;
        }

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>The identifier for the scope.
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="state"></param>
        /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            // Typically a string will be used as the state for a scope, but the BeginScope extension provides
            // a FormattedLogValues and ASP.NET provides multiple scope objects, so just use ToString()
            string description = state.ToString();

            // If there's data other than a simple string, it'll be added to the event
            object stateObj = state is string ? null : (object)state;

            // Log scope creation as an event so that there is a parent to tie events together
            ExceptionlessLoggingScope scope = new ExceptionlessLoggingScope(description);
            LogScope(scope, stateObj);

            // Add to stack to support nesting within execution context
            ExceptionlessLoggingScope.Push(scope);
            return scope;
        }

        /// <summary>
        /// Checks if the given <see cref="LogLevel"/> is enabled.
        /// </summary>
        /// <param name="logLevel">The level to be checked.</param>
        /// <returns>Returns true if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Create basic event from client
            EventBuilder eb = exception == null ? _Client.CreateEvent() : _Client.CreateException(exception);
            eb.SetProperty(Event.KnownDataKeys.Level, LOG_LEVELS[logLevel]);

            // Get formatted message
            eb.SetMessage(formatter.Invoke(state, exception));

            // Add category as source, if available
            if (_Source != null)
                eb.SetSource(_Source);

            // Add event id, if available
            if (eventId.Id != 0)
                eb.SetProperty("Event Id", eventId.Id);

            // If within a scope, add scope's reference id
            if (ExceptionlessLoggingScope.Current != null)
                eb.SetEventReference("Parent", ExceptionlessLoggingScope.Current.Id);

            // The logging framework passes in FormattedLogValues, which implements IEnumerable<KeyValuePair<string, object>>;
            // add each property and value as individual objects for proper visibility in Exceptionless
            IEnumerable<KeyValuePair<string, object>> stateProps = state as IEnumerable<KeyValuePair<string, object>>;
            if (stateProps != null)
            {
                foreach (KeyValuePair<string, object> prop in stateProps)
                {
                    // Logging the message template is superfluous
                    if (prop.Key != "{OriginalFormat}")
                        eb.AddObject(prop.Value, prop.Key);
                }
            }
            // Otherwise, attach the entire object, using its type as the name
            else
            {
                eb.AddObject(state);
            }

            // Add to client's queue
            eb.Submit();
        }

        /// <summary>
        /// Writes a scope creation entry.
        /// </summary>
        /// <param name="newScope">The <see cref="ExceptionlessLoggingScope"/> being created.</param>
        private void LogScope(ExceptionlessLoggingScope newScope, object state)
        {
            EventBuilder eb = _Client.CreateLog($"Creating scope: {newScope.Description}.", global::Exceptionless.Logging.LogLevel.Other);

            // Set event reference id to that of scope object
            eb.SetReferenceId(newScope.Id);

            // If this is a nested scope, add parent's reference id
            if (ExceptionlessLoggingScope.Current != null)
                eb.SetEventReference("Parent", ExceptionlessLoggingScope.Current.Id);

            if (state != null) 
            {
                IEnumerable<KeyValuePair<string, object>> stateProps = state as IEnumerable<KeyValuePair<string, object>>;
                if (stateProps != null) 
                {
                    foreach (KeyValuePair<string, object> prop in stateProps) 
                    {
                        // Logging the message template is superfluous
                        if (prop.Key != "{OriginalFormat}")
                            eb.AddObject(prop.Value, prop.Key);
                    }
                }
                else 
                {
                    eb.AddObject(state);
                }
            }

            eb.Submit();
        }
    }
}
