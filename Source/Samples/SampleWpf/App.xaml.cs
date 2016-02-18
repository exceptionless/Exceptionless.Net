using System;
using System.Windows;

namespace Exceptionless.SampleWpf {
    public partial class App : Application {
        private void Application_Startup(object sender, StartupEventArgs e) {
            ExceptionlessClient.Default.Configuration.UseTraceLogger();
            ExceptionlessClient.Default.Configuration.UseSessions(useSessionIdManagement: true);
            ExceptionlessClient.Default.Register();
        }
    }
}