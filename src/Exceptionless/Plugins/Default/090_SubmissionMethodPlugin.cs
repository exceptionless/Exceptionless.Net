using System;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(90)]
    public class SubmissionMethodPlugin : IEventPlugin {
        [Android.Preserve]
        public SubmissionMethodPlugin() {}

        public void Run(EventPluginContext context) {
            string submissionMethod = context.ContextData.GetSubmissionMethod();
            if (!String.IsNullOrEmpty(submissionMethod))
                context.Event.Data[Event.KnownDataKeys.SubmissionMethod] = submissionMethod;
        }
    }
}