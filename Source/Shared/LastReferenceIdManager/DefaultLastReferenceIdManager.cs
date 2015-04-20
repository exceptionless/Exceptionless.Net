using System;

namespace Exceptionless {
    internal class DefaultLastReferenceIdManager : ILastReferenceIdManager {
        private string _lastReferenceId;

        /// <summary>
        /// Gets the last event's reference id that was submitted to the server.
        /// </summary>
        /// <returns>The client id</returns>
        public string GetLast() {
            return _lastReferenceId;
        }

        /// <summary>
        /// Clears the last event's reference id.
        /// </summary>
        public void ClearLast() {
            _lastReferenceId = null;
        }

        /// <summary>
        /// Sets the last event's reference id.
        /// </summary>
        /// <param name="eventId"></param>
        public void SetLast(string eventId) {
            _lastReferenceId = eventId;
        }
    }
}