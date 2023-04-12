using System;
using System.Collections.Generic;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using NLog;

namespace Exceptionless.NLog {
    public static class LogEventBuilderExtensions {
        /// <summary>
        /// Sets the logger name of the logging event.
        /// </summary>
        /// <param name="builder">The log builder object.</param>
        /// <param name="loggerName">The logger name of the logging event.</param>
        /// <returns>current <see cref="LogEventBuilder"/> for chaining calls.</returns>
        internal static LogEventBuilder LoggerName(this LogEventBuilder builder, string loggerName = null) {
            if (builder.LogEvent != null)
                builder.LogEvent.LoggerName = loggerName;
            
            return builder;
        }

        /// <summary>
        /// Marks the event as being a critical occurrence.
        /// </summary>
        [CLSCompliant(false)]
        public static LogEventBuilder Critical(this LogEventBuilder builder, bool isCritical = true) {
            return isCritical ? builder.Tag("Critical") : builder;
        }

        /// <summary>
        /// Adds one or more tags to the event.
        /// </summary>
        /// <param name="builder">The log builder object.</param>
        /// <param name="tags">The tags to be added to the event.</param>
        [CLSCompliant(false)]
        public static LogEventBuilder Tag(this LogEventBuilder builder, params string[] tags) {
            if (builder.LogEvent != null)
                builder.LogEvent.AddTags(tags);

            return builder;
        }

        /// <summary>
        /// Sets the user's identity (ie. email address, username, user id) that the event happened to.
        /// </summary>
        /// <param name="builder">The log builder object.</param>
        /// <param name="identity">The user's identity that the event happened to.</param>
        [CLSCompliant(false)]
        public static LogEventBuilder Identity(this LogEventBuilder builder, string identity) {
            return builder.Identity(identity, null);
        }

        /// <summary>
        /// Sets the user's identity (ie. email address, username, user id) that the event happened to.
        /// </summary>
        /// <param name="builder">The log builder object.</param>
        /// <param name="identity">The user's identity that the event happened to.</param>
        /// <param name="name">The user's friendly name that the event happened to.</param>
        [CLSCompliant(false)]
        public static LogEventBuilder Identity(this LogEventBuilder builder, string identity, string name) {
            if (String.IsNullOrWhiteSpace(identity) && String.IsNullOrWhiteSpace(name))
                return builder;

            return builder.Property(Event.KnownDataKeys.UserInfo, new UserInfo(identity, name));
        }

        [CLSCompliant(false)]
        public static LogEventBuilder ContextProperty(this LogEventBuilder builder, string key, object value) {
            if (builder.LogEvent != null)
                builder.LogEvent.SetContextDataProperty(key, value);

            return builder;
        }

        /// <summary>
        /// Marks the event as being a unhandled occurrence and sets the submission method.
        /// </summary>
        /// <param name="builder">The log builder object.</param>
        /// <param name="submissionMethod">The submission method.</param>
        [CLSCompliant(false)]
        public static LogEventBuilder MarkUnhandled(this LogEventBuilder builder, string submissionMethod = null) {
            if (builder.LogEvent != null)
                return builder;

            builder.LogEvent.SetContextDataProperty(IsUnhandledError, true);
            if (!String.IsNullOrEmpty(submissionMethod))
                builder.LogEvent.SetContextDataProperty(SubmissionMethod, submissionMethod);

            return builder;
        }

        internal static HashSet<string> GetTags(this LogEventInfo ev) {
            if (ev.Properties.ContainsKey(Tags) && ev.Properties[Tags] is HashSet<string>)
                return (HashSet<string>)ev.Properties[Tags];

            return null;
        }

        private static void AddTags(this LogEventInfo ev, params string[] tags) {
            if (tags == null || tags.Length == 0)
                return;

            var list = ev.GetTags() ?? new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (string tag in tags)
                list.Add(tag);

            ev.Properties[Tags] = list;
        }

        internal static IDictionary<string, object> GetContextData(this LogEventInfo ev) {
            if (ev.Properties.ContainsKey(ContextData) && ev.Properties[ContextData] is IDictionary<string, object>)
                return (IDictionary<string, object>)ev.Properties[ContextData];

            return null;
        }

        private static void SetContextDataProperty(this LogEventInfo ev, string key, object value) {
            if (String.IsNullOrEmpty(key))
                return;

            var contextData = ev.GetContextData() ?? new Dictionary<string, object>();
            contextData[key] = value;

            ev.Properties[ContextData] = contextData;
        }

        private const string IsUnhandledError = "@@_IsUnhandledError";
        private const string SubmissionMethod = "@@_SubmissionMethod";
        private const string Tags = "Tags";
        private const string ContextData = "ContextData";
    }
}
