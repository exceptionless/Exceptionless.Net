using System;
using System.Reflection;
using Exceptionless.Plugins.Default;
using Xunit;

namespace Exceptionless.Tests.Plugins {
    public class HeartbeatPluginTests {
        [Theory]
        [InlineData(null, 60)]
        [InlineData(10, 60)]
        [InlineData(29, 60)]
        [InlineData(30, 30)]
        [InlineData(45, 45)]
        [InlineData(75, 75)]
        [InlineData(90, 90)]
        [InlineData(120, 120)]
        [InlineData(300, 300)]
        public void Constructor_WithInterval_UsesIntervalWhenAtLeastThirtySeconds(int? seconds, int expectedSeconds) {
            using var plugin = new HeartbeatPlugin(seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null);

            var field = typeof(HeartbeatPlugin).GetField("_interval", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), (TimeSpan)field.GetValue(plugin)!);
        }
    }
}
