namespace Exceptionless {
    public interface ILastReferenceIdManager {
        /// <summary>
        ///  Gets the last event id that was submitted to the server.
        /// </summary>
        /// <returns>The event id</returns>
        string GetLast();

        /// <summary>
        /// Clears the last event id.
        /// </summary>
        void ClearLast();

        /// <summary>
        /// Sets the last event id.
        /// </summary>
        /// <param name="eventId"></param>
        void SetLast(string eventId);
    }
}