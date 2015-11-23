using System;

namespace Exceptionless.Plugins.Default {
    [Priority(10)]
    public class ConfigurationDefaultsPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            foreach (string tag in context.Client.Configuration.DefaultTags)
                context.Event.Tags.Add(tag);

            foreach (var data in context.Client.Configuration.DefaultData)
                context.Event.SetProperty(data.Key, data.Value, excludedPropertyNames: context.Client.Configuration.DataExclusions);
        }
    }
}