using System;
using System.IO;
using System.Windows.Forms;
using Exceptionless.SampleWindows;

namespace Tester {
    public partial class FilterForm : Form {
        public FilterForm() {
            InitializeComponent();
        }

        private void FilterForm_Load(object sender, EventArgs e) {}

        private void configButton_Click(object sender, EventArgs e) {}

        private void ignoredButton_Click(object sender, EventArgs e) {
            string path = Path.GetRandomFileName();

            //try to open a file
            //simulate filenotfound exception
            string buffer = File.ReadAllText(path);
        }

        private void acceptedButton_Click(object sender, EventArgs e) {
            FilterTest.RunTest();
        }
    }
}

namespace Exceptionless.SampleWindows {
    public class FilterTest {
        public static void RunTest() {
            string path = Path.GetRandomFileName();

            //try to open a file
            //simulate filenotfound exception
            string buffer = File.ReadAllText(path);
        }
    }
}