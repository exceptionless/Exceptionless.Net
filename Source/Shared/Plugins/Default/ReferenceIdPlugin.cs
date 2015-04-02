using System;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(20)]
    public class ReferenceIdPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (!String.IsNullOrEmpty(context.Event.ReferenceId) || context.Event.Type != Event.KnownTypes.Error)
                return;

            context.Event.ReferenceId = Guid.NewGuid().ToString("N").Substring(0, 10);
        }
    }
}