using System;
using System.Linq;
using Exceptionless.Dependency;
using Exceptionless.Plugins.Default;
using Exceptionless.Logging;

namespace Exceptionless.Plugins {
    public static class EventPluginManager {
        /// <summary>
        /// Called when the event object is created and can be used to add information to the event.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="ev">Event that was created.</param>
        public static void Run(EventPluginContext context) {
            foreach (IEventPlugin plugin in context.Client.Configuration.Plugins.Select(e => e.Plugin).ToList()) {
                try {
                    plugin.Run(context);
                    if (context.Cancel) {
                        ExceptionlessLogExtensions.FormattedInfo(context.Resolver.GetLog(), plugin.GetType(), "Event submission cancelled by plugin: id={0} type={1}", context.Event.ReferenceId, context.Event.Type);
                        return;
                    }
                } catch (Exception ex) {
                    context.Resolver.GetLog().FormattedError(typeof(EventPluginManager), ex, "An error occurred while running {0}.Run(): {1}", plugin.GetType().FullName, ex.Message);
                }
            }
        }

        public static void AddDefaultPlugins(ExceptionlessConfiguration config) {
            config.AddPlugin<ConfigurationDefaultsPlugin>();
            config.AddPlugin<EnvironmentInfoPlugin>();
            config.AddPlugin<SimpleErrorPlugin>();
            config.AddPlugin<SubmissionMethodPlugin>();
        }
    }
}
