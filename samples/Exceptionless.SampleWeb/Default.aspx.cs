using System;
using System.IO;
using System.Web;
using System.Web.UI;

namespace Exceptionless.SampleWeb {
    public partial class _Default : Page {
        protected void Page_Load(object sender, EventArgs e) {
            Response.Cookies.Add(new HttpCookie("Blah", "blah"));
            //ExceptionlessClient.Default.UpdateConfiguration(true);
            ExceptionlessClient.Default.Configuration.DefaultTags.Add("Blah");
            Trace.Write("Default.aspx load");
        }

        protected void ErrorButton_Click(object sender, EventArgs e) {
            string text = File.ReadAllText("blah.txt");
        }
    }
}