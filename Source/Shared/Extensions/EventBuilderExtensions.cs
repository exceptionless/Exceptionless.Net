using System;
using Exceptionless.Models.Data;

namespace Exceptionless {
    public static class EventBuilderExtensions {
        /// <summary>
        /// Sets the user's identity (ie. email address, username, user id) that the event happened to.
        /// </summary>
        /// <param name="builder">The event builder object.</param>
        /// <param name="identity">The user's identity that the event happened to.</param>
        public static EventBuilder SetUserIdentity(this EventBuilder builder, string identity) {
            builder.Target.SetUserIdentity(identity);
            return builder;
        }

        /// <summary>
        /// Sets the user's identity (ie. email address, username, user id) that the event happened to.
        /// </summary>
        /// <param name="builder">The event builder object.</param>
        /// <param name="identity">The user's identity that the event happened to.</param>
        /// <param name="name">The user's friendly name that the event happened to.</param>
        public static EventBuilder SetUserIdentity(this EventBuilder builder, string identity, string name) {
            builder.Target.SetUserIdentity(identity, name);
            return builder;
        }

        /// <summary>
        /// Sets the user's identity (ie. email address, username, user id) that the event happened to.
        /// </summary>
        /// <param name="builder">The event builder object.</param>
        /// <param name="userInfo">The user's identity that the event happened to.</param>
        public static EventBuilder SetUserIdentity(this EventBuilder builder, UserInfo userInfo) {
            builder.Target.SetUserIdentity(userInfo);
            return builder;
        }

        /// <summary>
        /// Sets the user's description of the event.
        /// </summary>
        /// <param name="builder">The event builder object.</param>
        /// <param name="emailAddress">The user's email address.</param>
        /// <param name="description">The user's description of the event.</param>
        public static EventBuilder SetUserDescription(this EventBuilder builder, string emailAddress, string description) {
            builder.Target.SetUserDescription(emailAddress, description);
            return builder;
        }

        /// <summary>
        /// Sets the version that the event happened on.
        /// </summary>
        /// <param name="builder">The event builder object.</param>
        /// <param name="version">The version.</param>
        public static EventBuilder SetVersion(this EventBuilder builder, string version) {
            builder.Target.SetVersion(version);
            return builder;
        }

        /// <summary>
        /// Changes default stacking behavior by setting the stacking key.
        /// </summary>
        /// <param name="builder">The event builder object.</param>
        /// <param name="manualStackingKey">The manual stacking key.</param>
        public static EventBuilder SetManualStackingKey(this EventBuilder builder, string manualStackingKey) {
            builder.Target.SetManualStackingKey(manualStackingKey);
            return builder;
        }
    }
}