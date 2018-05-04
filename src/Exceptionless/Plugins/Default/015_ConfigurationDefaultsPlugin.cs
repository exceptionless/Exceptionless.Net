using System;

namespace Exceptionless.Plugins.Default {
    [Priority(15)]
    public class ConfigurationDefaultsPlugin : IEventPlugin {
        [Android.Preserve]
        public ConfigurationDefaultsPlugin() {}

        public void Run(EventPluginContext context) {
            foreach (string tag in context.Client.Configuration.DefaultTags)
                context.Event.Tags.Add(tag);

            foreach (var data in context.Client.Configuration.DefaultData) {
                if (!context.Event.Data.ContainsKey(data.Key))
                    context.Event.SetProperty(data.Key, data.Value, client: context.Client);
            }
        }
    }
}