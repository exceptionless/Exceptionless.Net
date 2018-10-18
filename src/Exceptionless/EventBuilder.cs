using System;
using System.Collections.Generic;
using Exceptionless.Plugins;
using Exceptionless.Models;

namespace Exceptionless {
    public class EventBuilder {
        public EventBuilder(Event ev, ExceptionlessClient client = null, ContextData pluginContextData = null) {
            Client = client ?? ExceptionlessClient.Default;
            Target = ev;
            PluginContextData = pluginContextData ?? new ContextData();
        }

        /// <summary>
        /// Any contextual data objects to be used by Exceptionless plugins to gather additional
        /// information for inclusion in the event.
        /// </summary>
        public ContextData PluginContextData { get; private set; }

        public ExceptionlessClient Client { get; set; }

        public Event Target { get; private set; }

        /// <summary>
        /// Sets the event type.
        /// </summary>
        /// <param name="type">The event type.</param>
        public EventBuilder SetType(string type) {
            Target.Type = type;
            return this;
        }

        /// <summary>
        /// Sets the event source.
        /// </summary>
        /// <param name="source">The event source.</param>
        public EventBuilder SetSource(string source) {
            Target.Source = source;
            return this;
        }

        /// <summary>
        /// Sets the event reference id.
        /// </summary>
        /// <param name="referenceId">The event reference id.</param>
        public EventBuilder SetReferenceId(string referenceId) {
            Target.SetReferenceId(referenceId);
            return this;
        }

        /// <summary>
        /// Allows you to reference a parent event by its <seealso cref="Event.ReferenceId" /> property. This allows you to have parent and child relationships.
        /// </summary>
        /// <param name="name">Reference name</param>
        /// <param name="id">The reference id that points to a specific event</param>
        public EventBuilder SetEventReference(string name, string id) {
            Target.SetEventReference(name, id);
            return this;
        }

        /// <summary>
        /// Sets the event message.
        /// </summary>
        /// <param name="message">The event message.</param>
        public EventBuilder SetMessage(string message) {
            Target.Message = message;
            return this;
        }

        /// <summary>
        /// Sets the event exception object.
        /// </summary>
        /// <param name="ex">The exception</param>
        public EventBuilder SetException(Exception ex) {
            PluginContextData.SetException(ex);
            return this;
        }

        /// <summary>
        /// Sets the event geo coordinates. Can be either "lat,lon" or an IP address that will be used to auto detect the geo coordinates.
        /// </summary>
        /// <param name="coordinates">The event coordinates.</param>
        public EventBuilder SetGeo(string coordinates) {
            Target.SetGeo(coordinates);
            return this;
        }

        /// <summary>
        /// Sets the event geo coordinates.
        /// </summary>
        /// <param name="latitude">The event latitude.</param>
        /// <param name="longitude">The event longitude.</param>
        public EventBuilder SetGeo(double latitude, double longitude) {
            Target.SetGeo(latitude, longitude);
            return this;
        }

        /// <summary>
        /// Sets the event value.
        /// </summary>
        /// <param name="value">The value of the event.</param>
        public EventBuilder SetValue(decimal value) {
            Target.Value = value;
            return this;
        }

        /// <summary>
        /// Adds one or more tags to the event.
        /// </summary>
        /// <param name="tags">The tags to be added to the event.</param>
        public EventBuilder AddTags(params string[] tags) {
            Target.AddTags(tags);
            return this;
        }

        /// <summary>
        /// Sets an extended property value to include with the event. Use either <paramref name="excludedPropertyNames" /> or
        /// <see cref="Exceptionless.Json.ExceptionlessIgnoreAttribute" /> to exclude data from being included in the event report.
        /// </summary>
        /// <param name="name">The name of the object to add.</param>
        /// <param name="value">The data object to add.</param>
        /// <param name="maxDepth">The max depth of the object to include.</param>
        /// <param name="excludedPropertyNames">Any property names that should be excluded.</param>
        /// <param name="ignoreSerializationErrors">Specifies if properties that throw serialization errors should be ignored.</param>
        public EventBuilder SetProperty(string name, object value, int? maxDepth = null, ICollection<string> excludedPropertyNames = null, bool ignoreSerializationErrors = false) {
            if (value != null)
                Target.AddObject(value, name, maxDepth, excludedPropertyNames, ignoreSerializationErrors);

            return this;
        }

        /// <summary>
        /// Adds the object to extended data. Use either <paramref name="excludedPropertyNames" /> or
        /// <see cref="Exceptionless.Json.ExceptionlessIgnoreAttribute" /> to exclude data from being included in the event.
        /// </summary>
        /// <param name="data">The data object to add.</param>
        /// <param name="name">The name of the object to add.</param>
        /// <param name="maxDepth">The max depth of the object to include.</param>
        /// <param name="excludedPropertyNames">Any property names that should be excluded.</param>
        /// <param name="ignoreSerializationErrors">Specifies if properties that throw serialization errors should be ignored.</param>
        public EventBuilder AddObject(object data, string name = null, int? maxDepth = null, ICollection<string> excludedPropertyNames = null, bool ignoreSerializationErrors = false) {
            if (data != null)
                Target.AddObject(data, name, maxDepth, excludedPropertyNames, ignoreSerializationErrors);

            return this;
        }

        /// <summary>
        /// Marks the event as being a critical occurrence.
        /// </summary>
        public EventBuilder MarkAsCritical() {
            Target.MarkAsCritical();
            return this;
        }

        /// <summary>
        /// Submits the event report.
        /// </summary>
        public void Submit() {
            Client.SubmitEvent(Target, PluginContextData);
        }
    }
}