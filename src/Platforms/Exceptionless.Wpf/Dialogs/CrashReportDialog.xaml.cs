using System;
using System.Windows;
using System.Windows.Input;
using Exceptionless.Dependency;
using Exceptionless.Extras.Utility;
using Exceptionless.Models;
using Exceptionless.Storage;

namespace Exceptionless.Dialogs {
    /// <summary>
    /// Interaction logic for CrashReportDialog.xaml
    /// </summary>
    public partial class CrashReportDialog : Window {
        public ExceptionlessClient Client { get; internal set; }
        public Event Event { get; internal set; }

        public CrashReportDialog(ExceptionlessClient client, Event ev) {
            InitializeComponent();

            Client = client;
            Event = ev;
            Title = String.Concat(AssemblyHelper.GetAssemblyTitle(), " Error");
            InformationHeaderLabel.Text = String.Concat(AssemblyHelper.GetAssemblyTitle(), " has encountered a problem and needs to close.  We are sorry for the inconvenience.");

            var userInfo = ev.GetUserIdentity(client.Configuration.Resolver.GetJsonSerializer());
            if (userInfo != null && !String.IsNullOrEmpty(userInfo.Identity)) {
                EmailAddressTextBox.Text = userInfo.Identity;
            } else {
                var storage = client.Configuration.Resolver.Resolve<PersistedDictionary>();
                string emailAddress;
                if (storage != null && storage.TryGetValue("EmailAddress", out emailAddress))
                    EmailAddressTextBox.Text = emailAddress;
            }

            var userDescription = Event.GetUserDescription(client.Configuration.Resolver.GetJsonSerializer());
            if (userDescription != null)
                DescriptionTextBox.Text = userDescription.Description;
        }

        private void OnSubmitReportButtonClick(object sender, RoutedEventArgs e) {
            Cursor = Cursors.Wait;

            SubmitReportButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            if (!String.IsNullOrWhiteSpace(EmailAddressTextBox.Text)) {
                var storage = Client.Configuration.Resolver.Resolve<PersistedDictionary>();
                if (storage != null)
                    storage["EmailAddress"] = EmailAddressTextBox.Text;
            }

            Event.SetUserDescription(EmailAddressTextBox.Text, DescriptionTextBox.Text);

            Cursor = Cursors.Arrow;
            SubmitReportButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            DialogResult = true;
            Close();
        }
    }
}