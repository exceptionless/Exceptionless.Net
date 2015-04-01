using System;

namespace Exceptionless {
    public interface ILastReferenceIdManager {
        /// <summary>
        ///     Gets the last error id that was submitted to the server.
        /// </summary>
        /// <returns>The error id</returns>
        string GetLast();

        /// <summary>
        ///     Clears the last error id.
        /// </summary>
        void ClearLast();

        /// <summary>
        ///     Sets the last error id.
        /// </summary>
        /// <param name="errorId"></param>
        void SetLast(string errorId);
    }
}