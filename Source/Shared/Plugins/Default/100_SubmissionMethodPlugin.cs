using System;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(100)]
    public class SubmissionMethodPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            string submissionMethod = context.ContextData.GetSubmissionMethod();
            if (!String.IsNullOrEmpty(submissionMethod))
                context.Event.Data[Event.KnownDataKeys.SubmissionMethod] = submissionMethod;
        }
    }
}