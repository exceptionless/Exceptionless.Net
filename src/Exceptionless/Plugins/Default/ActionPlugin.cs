using System;
using Exceptionless.Logging;

namespace Exceptionless.Plugins.Default {
    public class ActionPlugin : IEventPlugin {
        private readonly Action<EventPluginContext> _pluginAction;

        [Android.Preserve]
        public ActionPlugin(Action<EventPluginContext> pluginAction) {
            _pluginAction = pluginAction;
        }

        public void Run(EventPluginContext context) {
            try {
                _pluginAction(context);
            } catch (Exception ex) {
                context.Log.FormattedError(typeof(ActionPlugin), ex, "An error occurred while running an custom plugin: {0}", ex.Message);
            }
        }
    }
}