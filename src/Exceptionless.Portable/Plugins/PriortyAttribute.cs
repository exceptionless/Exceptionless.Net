﻿using System;

namespace Exceptionless.Plugins {
    /// <summary>
    /// Used to determine plugin priority.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PriorityAttribute : Attribute {
        public PriorityAttribute(int priority) {
            Priority = priority;
        }

        public int Priority { get; private set; }
    }
}