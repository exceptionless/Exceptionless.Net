using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Exceptionless.Models.Collections;
using Exceptionless.Plugins;
using Tester;

namespace Exceptionless.SampleWindows {
    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
            ExceptionlessClient.Default.SubmittingEvent += OnSubmittingEvent;
            ExceptionlessClient.Default.SubmittedEvent += OnSubmittedEvent;
            ExceptionlessClient.Default.Configuration.Settings.Changed += SettingsOnChanged;
        }

        private void SettingsOnChanged(object sender, ChangedEventArgs<KeyValuePair<string, string>> args) {
            if (logTextBox.InvokeRequired) {
                logTextBox.Invoke(new EventHandler<ChangedEventArgs<KeyValuePair<string, string>>>(SettingsOnChanged), sender, args);
                return;
            }

            logTextBox.AppendText("\r\nConfiguration Updated.");
        }

        private void OnSubmittingEvent(object sender, EventSubmittingEventArgs e) {
            if (logTextBox.InvokeRequired) {
                logTextBox.Invoke(new EventHandler<EventSubmittingEventArgs>(OnSubmittingEvent), sender, e);
                return;
            }

            e.Event.Data["BaseDirectory"] = AppDomain.CurrentDomain.BaseDirectory;
            if (e.Event.Message == "Important Exception")
                e.Event.Tags.Add("Important");

            if (!String.IsNullOrEmpty(e.Event.ReferenceId))
                logTextBox.AppendText($"\r\nSubmitting Event: {e.Event.ReferenceId}{Environment.NewLine}");
            else
                logTextBox.AppendText("\r\nSubmitting Event");

            statusLabel.Text = "Submitting Message";
        }

        private void OnSubmittedEvent(object sender, EventSubmittedEventArgs e) {
            statusLabel.Text = String.Empty;
        }

        private void generateExceptionToolStripMenuItem_Click(object sender, EventArgs e) {
            //try to open a file
            string buffer = File.ReadAllText("somefile2.txt");
        }

        private void processQueueToolStripMenuItem_Click(object sender, EventArgs e) {
            ExceptionlessClient.Default.ProcessQueueAsync();
        }

        private void OnRandomExceptionMenuItemClick(object sender, EventArgs e) {
            File.ReadAllText(Path.GetRandomFileName());
        }

        private void OnAttemptDeadlockMenuItemClick(object sender, EventArgs e) {
            for (int i = 0; i < 10; i++) {
                for (int i2 = 0; i2 < 5; i2++) {
                    new Exception("test for deadlock").ToExceptionless().Submit();
                }

                var contextData = new ContextData();
                contextData.MarkAsUnhandledError();
                contextData.SetSubmissionMethod("OnAttemptDeadlockMenuItemClick");

                new ApplicationException("Attempt to deadlock:" + i).ToExceptionless(contextData).SetManualStackingInfo(new Dictionary<string, string> {{ "iteration", i.ToString() }}).Submit();
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e) {
            try {
                File.ReadAllText("somefile.txt");
            } catch (Exception ex) {
                throw new ApplicationException("An ev has occurred and I have wrapped it inside of this ApplicationException.", ex);
            }
        }

        private void ignoredExceptionToolStripMenuItem_Click(object sender, EventArgs e) {
            throw new InvalidOperationException("Some fake exception we will check for and ignore.");
        }

        private void importantExceptionToolStripMenuItem_Click(object sender, EventArgs e) {
            using (ExceptionlessClient.Default.Configuration.DefaultTags.Add("Important")) {
                // Doing really important work here like maybe processing an order.
                throw new OverflowException("Bad things man.");
            }
        }

        private void multipleExceptionsToolStripMenuItem_Click(object sender, EventArgs e) {
            var multiple = new MultipleForm();
            multiple.ShowDialog(this);

            decimal count = multiple.NumericUpDown.Value;

            for (int i = 0; i < count; i++)
                new ApplicationException("Multiple Crash Test.").ToExceptionless().SetUserDescription("some@email.com", "Testing multiple crash reports. " + i).Submit();
        }

        private void showFilterFormToolStripMenuItem_Click(object sender, EventArgs e) {
            var filterForm = new FilterForm();
            filterForm.Show();
        }

        private void MainForm_Load(object sender, EventArgs e) {}

        private void importDemoReportsToolStripMenuItem_Click(object sender, EventArgs e) {
            // find sample folder 
            string folder = SampleLoader.FindSamples();
            if (String.IsNullOrEmpty(folder)) {
                logTextBox.AppendText("Event: Samples directory not found.");
                return;
            }

            var loader = new SampleLoader(folder);
            loader.Load();
        }
    }
}