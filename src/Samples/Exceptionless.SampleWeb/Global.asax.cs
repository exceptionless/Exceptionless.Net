using System;
using System.Web;

namespace Exceptionless.SampleWeb {
    public partial class Global : HttpApplication {
        protected void Application_Start(object sender, EventArgs e) {
            ExceptionlessClient.Default.Configuration.UseTraceLogger();
            ExceptionlessClient.Default.Configuration.UseReferenceIds();
            ExceptionlessClient.Default.SubmittingEvent += OnSubmittingEvent;
        }

        private void OnSubmittingEvent(object sender, EventSubmittingEventArgs e) {
            // you can get access to the report here
            e.Event.Tags.Add("WebTag");
        }
    }
}