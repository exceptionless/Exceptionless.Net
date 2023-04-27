namespace Exceptionless.Plugins {
    public interface IEventPlugin {
        /// <summary>
        /// Runs the plugin.
        /// </summary>
        /// <param name="context">Context information.</param>
        void Run(EventPluginContext context);
    }
}