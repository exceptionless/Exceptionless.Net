﻿using System;
using Exceptionless.Dependency;
using Xunit;

namespace Exceptionless.Tests {
    public class DefaultLastReferenceIdManagerTests {
        [Fact]
        public void VerfiyGetSetAndClear() {
            var lastReferenceIdManager = DependencyResolver.Default.GetLastReferenceIdManager();
            Assert.Null(lastReferenceIdManager.GetLast());

            string key = Guid.NewGuid().ToString();
            lastReferenceIdManager.SetLast(key);
            Assert.Equal(key, lastReferenceIdManager.GetLast());
            
            lastReferenceIdManager.ClearLast();
            Assert.Null(lastReferenceIdManager.GetLast());
        }
    }
}