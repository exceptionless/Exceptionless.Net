using System;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
    [Priority(30)]
    public class SubmissionMethodPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            string submissionMethod = context.ContextData.GetSubmissionMethod();
            if (!String.IsNullOrEmpty(submissionMethod))
                context.Event.AddObject(submissionMethod, Event.KnownDataKeys.SubmissionMethod);
        }
    }
}