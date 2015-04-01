using System;
using System.Windows.Forms;
using Exceptionless.Dependency;
using Exceptionless.Extras.Utility;
using Exceptionless.Models;
using Exceptionless.Storage;

namespace Exceptionless.Dialogs {
    public sealed partial class CrashReportForm : Form {
        public ExceptionlessClient Client { get; internal set; }
        public Event Event { get; internal set; }


        public CrashReportForm(ExceptionlessClient client, Event ev) {
            InitializeComponent();

            Client = client;
            Event = ev;
            Text = String.Format("{0} Error", AssemblyHelper.GetAssemblyTitle());
            InformationHeaderLabel.Text = String.Format("{0} has encountered a problem and needs to close.  We are sorry for the inconvenience.", AssemblyHelper.GetAssemblyTitle());

            var userInfo = ev.GetUserIdentity();
            if (userInfo != null && !String.IsNullOrEmpty(userInfo.Identity)) {
                EmailAddressTextBox.Text = userInfo.Identity;
            } else {
                var storage = client.Configuration.Resolver.Resolve<PersistedDictionary>();
                string emailAddress;
                if (storage != null && storage.TryGetValue("EmailAddress", out emailAddress))
                    EmailAddressTextBox.Text = emailAddress;
            }

            var userDescription = Event.GetUserDescription();
            if (userDescription != null)
                DescriptionTextBox.Text = userDescription.Description;
        }

        private void ExitButton_Click(object sender, EventArgs e) {
            Close();
        }

        private void OnSubmitReportButtonClick(object sender, EventArgs e) {
            Cursor = Cursors.WaitCursor;
            SendReportButton.Enabled = false;
            ExitButton.Enabled = false;

            if (!String.IsNullOrWhiteSpace(EmailAddressTextBox.Text)) {
                var storage = Client.Configuration.Resolver.Resolve<PersistedDictionary>();
                if (storage != null)
                    storage["EmailAddress"] = EmailAddressTextBox.Text;
            }

            Event.SetUserDescription(EmailAddressTextBox.Text, DescriptionTextBox.Text);

            Cursor = Cursors.Default;
            SendReportButton.Enabled = true;
            ExitButton.Enabled = true;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}