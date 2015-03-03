#region Copyright 2014 Exceptionless

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
//     http://www.apache.org/licenses/LICENSE-2.0

#endregion

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
        /// <param name="errorId"></param>
        public void SetLast(string errorId) {
            _lastReferenceId = errorId;
        }
    }
}