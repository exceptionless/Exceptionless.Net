using System;
using System.IO;
using System.Xml;
using Exceptionless.Models;

namespace Exceptionless.SampleWindows {
    public class SampleLoader {
        public SampleLoader(string sampleFolder) {
            SampleFolder = sampleFolder;
        }

        public string SampleFolder { get; private set; }

        public static string FindSamples() {
            string folder = Path.GetFullPath("Samples");
            if (Directory.Exists(folder))
                return folder;

            folder = Path.GetFullPath(Path.Combine("..", "Samples"));
            if (Directory.Exists(folder))
                return folder;

            folder = Path.GetFullPath(Path.Combine("..", "..", "Samples"));
            if (Directory.Exists(folder))
                return folder;

            return null;
        }

        public int Submitted { get; set; }

        public void Load() {
            var sampleRoot = new DirectoryInfo(SampleFolder);
            if (!sampleRoot.Exists)
                return;

            foreach (DirectoryInfo directory in sampleRoot.GetDirectories()) {
                FileInfo[] caseReports = directory.GetFiles("*.Error.xml", SearchOption.TopDirectoryOnly);
                if (caseReports.Length == 0)
                    continue;

                FileInfo caseReport = caseReports[0];
                LoadSample(caseReport);
            }
        }

        private void LoadSample(FileInfo caseReport) {
            if (!caseReport.Exists)
                return;

            Event report = null;

            using (FileStream fs = caseReport.OpenRead()) {
                using (XmlReader xr = new XmlTextReader(fs)) {
                    //report = Event.Load(xr);
                }
            }

            if (report == null)
                return;

            InitializeReport(report);

            ExceptionlessClient.Default.SubmitEvent(report);
            Submitted++;
        }

        private void InitializeReport(Event report) {
            if (report.Data == null)
                report.Data = new DataDictionary();
            if (report.Tags == null)
                report.Tags = new TagSet();

            // reset values
            report.ReferenceId = String.Empty;
            report.Date = DateTimeOffset.Now;
        }
    }
}