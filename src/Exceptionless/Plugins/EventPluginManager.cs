using System;
using System.Linq;
using Exceptionless.Plugins.Default;
using Exceptionless.Logging;

namespace Exceptionless.Plugins {
    public static class EventPluginManager {
        /// <summary>
        /// Called when the event object is created and can be used to add information to the event.
        /// </summary>
        /// <param name="context">Context information.</param>
        public static void Run(EventPluginContext context) {
            foreach (IEventPlugin plugin in context.Client.Configuration.Plugins.Select(e => e.Plugin)) {
                try {
                    plugin.Run(context);
                    if (context.Cancel) {
                        context.Log.FormattedInfo(plugin.GetType(), "Event submission cancelled by plugin: refid={0} type={1} message={2}", context.Event.ReferenceId, context.Event.Type, context.Event.Message);
                        return;
                    }
                } catch (Exception ex) {
                    context.Log.FormattedError(typeof(EventPluginManager), ex, "An error occurred while running {0}.Run(): {1}", plugin.GetType().FullName, ex.Message);
                }
            }
        }

        public static void AddDefaultPlugins(ExceptionlessConfiguration config) {
            config.AddPlugin<HandleAggregateExceptionsPlugin>();
            config.AddPlugin<EventExclusionPlugin>();
            config.AddPlugin<ConfigurationDefaultsPlugin>();
            config.AddPlugin<ErrorPlugin>();
            config.AddPlugin<DuplicateCheckerPlugin>();
            config.AddPlugin<EnvironmentInfoPlugin>();
            config.AddPlugin<VersionPlugin>();
            config.AddPlugin<SubmissionMethodPlugin>();
            config.AddPlugin<CancelSessionsWithNoUserPlugin>();
        }
    }
}
