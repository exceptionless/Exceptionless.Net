using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Exceptionless.Extensions;
using Exceptionless.Models;
using Exceptionless.Models.Data;

namespace Exceptionless {
    public static class EventExtensions {
        public static Error GetError(this Event ev, IJsonSerializer serializer = null) {
            return ev.GetDataValue<Error>(Event.KnownDataKeys.Error, serializer);
        }

        public static SimpleError GetSimpleError(this Event ev, IJsonSerializer serializer = null) {
            return ev.GetDataValue<SimpleError>(Event.KnownDataKeys.SimpleError, serializer);
        }

        public static RequestInfo GetRequestInfo(this Event ev, IJsonSerializer serializer = null) {
            return ev.GetDataValue<RequestInfo>(Event.KnownDataKeys.RequestInfo, serializer);
        }

        public static EnvironmentInfo GetEnvironmentInfo(this Event ev, IJsonSerializer serializer = null) {
            return ev.GetDataValue<EnvironmentInfo>(Event.KnownDataKeys.EnvironmentInfo, serializer);
        }

        /// <summary>
        /// Indicates whether the event has been marked as critical.
        /// </summary>
        public static bool IsCritical(this Event ev) {
            return ev.Tags != null && ev.Tags.Contains(Event.KnownTags.Critical);
        }

        /// <summary>
        /// Marks the event as being a critical occurrence.
        /// </summary>
        public static void MarkAsCritical(this Event ev) {
            if (ev.Tags == null)
                ev.Tags = new TagSet();

            ev.Tags.Add(Event.KnownTags.Critical);
        }

        /// <summary>
        /// Returns true if the event type is not found.
        /// </summary>
        public static bool IsNotFound(this Event ev) {
            return ev.Type == Event.KnownTypes.NotFound;
        }

        /// <summary>
        /// Returns true if the event type is error.
        /// </summary>
        public static bool IsError(this Event ev) {
            return ev.Type == Event.KnownTypes.Error;
        }

        /// <summary>
        /// Returns true if the event type is log.
        /// </summary>
        public static bool IsLog(this Event ev) {
            return ev.Type == Event.KnownTypes.Log;
        }

        /// <summary>
        /// Returns true if the event type is feature usage.
        /// </summary>
        public static bool IsFeatureUsage(this Event ev) {
            return ev.Type == Event.KnownTypes.FeatureUsage;
        }

        /// <summary>
        /// Returns true if the event type is session start.
        /// </summary>
        public static bool IsSessionStart(this Event ev) {
            return ev.Type == Event.KnownTypes.Session;
        }

        /// <summary>
        /// Adds the request info to the event.
        /// </summary>
        public static void AddRequestInfo(this Event ev, RequestInfo request) {
            if (request == null)
                return;

            ev.Data[Event.KnownDataKeys.RequestInfo] = request;
        }

        /// <summary>
        /// Sets the version that the event happened on.
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="version">The version.</param>
        public static void SetVersion(this Event ev, string version) {
            if (String.IsNullOrWhiteSpace(version))
                return;

            ev.Data[Event.KnownDataKeys.Version] = version.Trim();
        }

        /// <summary>
        /// Gets the user info object from extended data.
        /// </summary>
        public static UserInfo GetUserIdentity(this Event ev, IJsonSerializer serializer = null) {
            return ev.GetDataValue<UserInfo>(Event.KnownDataKeys.UserInfo, serializer);
        }

        /// <summary>
        /// Sets the user's identity (ie. email address, username, user id) that the event happened to.
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="identity">The user's identity that the event happened to.</param>
        public static void SetUserIdentity(this Event ev, string identity) {
            ev.SetUserIdentity(identity, null);
        }

        /// <summary>
        /// Sets the user's identity (ie. email address, username, user id) and name that the event happened to.
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="identity">The user's identity that the event happened to.</param>
        /// <param name="name">The user's friendly name that the event happened to.</param>
        public static void SetUserIdentity(this Event ev, string identity, string name) {
            if (String.IsNullOrWhiteSpace(identity) && String.IsNullOrWhiteSpace(name))
                return;

            ev.SetUserIdentity(new UserInfo(identity, name));
        }

        /// <summary>
        /// Sets the user's identity (ie. email address, username, user id) and name that the event happened to.
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="userInfo">The user's identity that the event happened to.</param>
        public static void SetUserIdentity(this Event ev, UserInfo userInfo) {
            if (userInfo == null)
                return;

            ev.Data[Event.KnownDataKeys.UserInfo] = userInfo;
        }

        /// <summary>
        /// Gets the user description from extended data.
        /// </summary>
        public static UserDescription GetUserDescription(this Event ev, IJsonSerializer serializer = null) {
            return ev.GetDataValue<UserDescription>(Event.KnownDataKeys.UserDescription, serializer);
        }

        /// <summary>
        /// Sets the user's description of the event.
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="emailAddress">The email address</param>
        /// <param name="description">The user's description of the event.</param>
        public static void SetUserDescription(this Event ev, string emailAddress, string description) {
            if (String.IsNullOrWhiteSpace(emailAddress) && String.IsNullOrWhiteSpace(description))
                return;

            ev.Data[Event.KnownDataKeys.UserDescription] = new UserDescription(emailAddress, description);
        }

        /// <summary>
        /// Sets the user's description of the event.
        /// </summary>
        /// <param name="ev">The event.</param>
        /// <param name="description">The user's description.</param>
        public static void SetUserDescription(this Event ev, UserDescription description) {
            if (description == null || (String.IsNullOrWhiteSpace(description.EmailAddress) && String.IsNullOrWhiteSpace(description.Description)))
                return;

            ev.Data[Event.KnownDataKeys.UserDescription] = description;
        }

        /// <summary>
        /// Sets the event geo coordinates. Can be either "lat,lon" or an IP address that will be used to auto detect the geo coordinates.
        /// </summary>
        /// <param name="ev">The event.</param>
        /// <param name="coordinates">The event coordinates.</param>
        public static void SetGeo(this Event ev, string coordinates) {
            if (String.IsNullOrWhiteSpace(coordinates)) {
                ev.Geo = null;
                return;
            }

            if (coordinates.Contains(",") || coordinates.Contains(".") || coordinates.Contains(":"))
                ev.Geo = coordinates;
            else
                throw new ArgumentException("Must be either lat,lon or an IP address.", nameof(coordinates));
        }

        /// <summary>
        /// Sets the event geo coordinates.
        /// </summary>
        /// <param name="ev">The event.</param>
        /// <param name="latitude">The event latitude.</param>
        /// <param name="longitude">The event longitude.</param>
        public static void SetGeo(this Event ev, double latitude, double longitude) {
            if (latitude < -90.0 || latitude > 90.0)
                throw new ArgumentOutOfRangeException(nameof(latitude), "Must be a valid latitude value between -90.0 and 90.0.");
            if (longitude < -180.0 || longitude > 180.0)
                throw new ArgumentOutOfRangeException(nameof(longitude), "Must be a valid longitude value between -180.0 and 180.0.");

            ev.Geo = latitude.ToString("#0.0#######", CultureInfo.InvariantCulture) + "," + longitude.ToString("#0.0#######", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Adds one or more tags to the event.
        /// </summary>
        /// <param name="ev">The event.</param>
        /// <param name="tags">The tags to be added to the event.</param>
        public static void AddTags(this Event ev, params string[] tags) {
            if (tags == null || tags.Length == 0)
                return;

            ev.Tags.AddRange(tags.Where(t => !String.IsNullOrWhiteSpace(t)).Select(t => t.Trim()));
        }

        /// <summary>
        /// Sets the event reference id.
        /// </summary>
        /// <param name="ev">The event.</param>
        /// <param name="referenceId">The event reference id.</param>
        public static void SetReferenceId(this Event ev, string referenceId) {
            if (!IsValidIdentifier(referenceId))
                throw new ArgumentException("ReferenceId must contain between 8 and 100 alphanumeric or '-' characters.", nameof(referenceId));

            ev.ReferenceId = referenceId;
        }

        /// <summary>
        /// Returns the event reference id.
        /// </summary>
        /// <param name="ev">The event.</param>
        /// <param name="name">Reference name</param>
        /// <returns></returns>
        public static string GetEventReference(this Event ev, string name) {
            if (ev == null || String.IsNullOrEmpty(name))
                return null;

            return ev.Data.GetString($"@ref:{name}");
        }

        /// <summary>
        /// Allows you to reference a parent event by its <seealso cref="Event.ReferenceId" /> property. This allows you to have parent and child relationships.
        /// </summary>
        /// <param name="ev">The event.</param>
        /// <param name="name">Reference name</param>
        /// <param name="id">The reference id that points to a specific event</param>
        public static void SetEventReference(this Event ev, string name, string id) {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (!IsValidIdentifier(id) || String.IsNullOrEmpty(id))
                throw new ArgumentException("Id must contain between 8 and 100 alphanumeric or '-' characters.", nameof(id));

            ev.SetProperty($"@ref:{name}", id);
        }

        private static bool IsValidIdentifier(string value) {
            if (value == null)
                return true;

            if (value.Length < 8 || value.Length > 100)
                return false;

            return value.IsValidIdentifier();
        }

        /// <summary>
        /// Changes default stacking behavior
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="signatureData">Key value pair that determines how the event is stacked.</param>
        public static void SetManualStackingInfo(this Event ev, IDictionary<string, string> signatureData) {
            if (signatureData == null || signatureData.Count == 0)
                return;

            ev.Data[Event.KnownDataKeys.ManualStackingInfo] = new ManualStackingInfo(signatureData);
        }

        /// <summary>
        /// Changes default stacking behavior
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="title">The stack title.</param>
        /// <param name="signatureData">Key value pair that determines how the event is stacked.</param>
        public static void SetManualStackingInfo(this Event ev, string title, IDictionary<string, string> signatureData) {
            if (String.IsNullOrWhiteSpace(title) || signatureData == null || signatureData.Count == 0)
                return;

            ev.Data[Event.KnownDataKeys.ManualStackingInfo] = new ManualStackingInfo(title, signatureData);
        }

        /// <summary>
        /// Changes default stacking behavior by setting the stacking info.
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="manualStackingKey">The manual stacking key.</param>
        public static void SetManualStackingKey(this Event ev, string manualStackingKey) {
            if (String.IsNullOrWhiteSpace(manualStackingKey))
                return;

            ev.Data[Event.KnownDataKeys.ManualStackingInfo] = new ManualStackingInfo(null, new Dictionary<string, string> { { "ManualStackingKey", manualStackingKey } });
        }

        /// <summary>
        /// Changes default stacking behavior by setting the stacking info.
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="title">The stack title.</param>
        /// <param name="manualStackingKey">The manual stacking key.</param>
        public static void SetManualStackingKey(this Event ev, string title, string manualStackingKey) {
            if (String.IsNullOrWhiteSpace(title) || String.IsNullOrWhiteSpace(manualStackingKey))
                return;

            ev.Data[Event.KnownDataKeys.ManualStackingInfo] = new ManualStackingInfo(title, new Dictionary<string, string> { { "ManualStackingKey", manualStackingKey } });
        }

        public static T GetDataValue<T>(this Event ev, string key, IJsonSerializer serializer = null) {
            if(ev == null || String.IsNullOrEmpty(key) || !ev.Data.ContainsKey(key))
                return default;

            try {
                return ev.Data.GetValue<T>(key, serializer);
            } catch (Exception) { }

            return default;
        }
    }

    /// <summary>
    /// A class that contains info about objects that will be added to the error report's ExtendedData collection.
    /// </summary>
    public class ExtendedDataInfo {
        public ExtendedDataInfo() {
            ExcludedPropertyNames = new List<string>();
        }

        /// <summary>
        /// The name to use for the ExtendedData entry.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The object that should be serialized and added to the ExtendedData of the error.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// The maximum depth to go into the object graph when serializing the data.
        /// </summary>
        public int? MaxDepthToSerialize { get; set; }

        /// <summary>
        /// The names of any properties that should be excluded.
        /// </summary>
        public ICollection<string> ExcludedPropertyNames { get; set; }

        /// <summary>
        /// Specifies if properties that throw serialization errors should be ignored.
        /// </summary>
        public bool IgnoreSerializationErrors { get; set; }
    }
}