using System;
using System.Diagnostics;
using System.Windows.Forms;
using Exceptionless.Configuration;

[assembly: Exceptionless("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw", ServerUrl = "http://localhost:5000")]

namespace Exceptionless.SampleWindows {
    internal static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main() {
            ExceptionlessClient.Default.Configuration.UseTraceLogger();
            ExceptionlessClient.Default.Register();
            ExceptionlessClient.Default.SubmittingEvent += OnSubmittingEvent;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                Application.Run(new MainForm());
            } catch (InvalidOperationException) {
                Debug.WriteLine("Got an InvalidOperationException.");
            }
        }

        private static void OnSubmittingEvent(object sender, EventSubmittingEventArgs e) {
            e.Event.Tags.Add("ExtraTag");

            var exception = e.PluginContextData.GetException();
            if (exception != null && exception.GetType() == typeof(InvalidOperationException))
                e.Cancel = true;
        }
    }
}