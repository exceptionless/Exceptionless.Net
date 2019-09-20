using System;
using System.Windows.Forms;

namespace Exceptionless.SampleWindows {
    public partial class MultipleForm : Form {
        public MultipleForm() {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e) {
            Close();
        }
    }
}