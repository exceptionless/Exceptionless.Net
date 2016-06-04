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
            foreach (IEventPlugin plugin in context.Client.Configuration.Plugins.Select(e => e.Plugin).ToList()) {
                try {
                    plugin.Run(context);
                    if (context.Cancel) {
                        context.Log.FormattedInfo(plugin.GetType(), "Event submission cancelled by plugin: id={0} type={1}", context.Event.ReferenceId, context.Event.Type);
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
#if !PORTABLE && !NETSTANDARD1_2
            config.AddPlugin<ErrorPlugin>();
#else
            config.AddPlugin<SimpleErrorPlugin>();
#endif
            config.AddPlugin<DuplicateCheckerPlugin>();
            config.AddPlugin<EnvironmentInfoPlugin>();
#if !PORTABLE && !NETSTANDARD1_2
            config.AddPlugin<VersionPlugin>();
#endif
            config.AddPlugin<SubmissionMethodPlugin>();
            config.AddPlugin<CancelSessionsWithNoUserPlugin>();
        }
    }
}
