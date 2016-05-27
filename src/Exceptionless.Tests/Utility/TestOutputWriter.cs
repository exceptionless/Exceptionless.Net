﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Utility {
    public class TestOutputWriter : TextWriter {
        private readonly ITestOutputHelper _output;

        public TestOutputWriter(ITestOutputHelper output) {
            _output = output;
        }

        public override Encoding Encoding {
            get { return Encoding.UTF8; }
        }

        public override void WriteLine(string value) {
            try {
                _output.WriteLine(value);
            } catch (Exception ex) {
                Trace.WriteLine(ex);
            }
        }

        public override void WriteLine() {
            WriteLine(String.Empty);
        }
    }
}