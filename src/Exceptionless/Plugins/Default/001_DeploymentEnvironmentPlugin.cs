namespace Exceptionless.Plugins.Default {
    [Priority(1)]
    public sealed class DeploymentEnvironmentPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (!context.Event.HasEnvironmentOverride)
                context.Event.Environment = context.Client.Configuration.Environment;
        }
    }
}
