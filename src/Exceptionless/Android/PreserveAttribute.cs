using System;
using System.Collections.Generic;
using System.Text;

namespace Exceptionless.Android {
    internal sealed class PreserveAttribute : Attribute {
        public bool AllMembers;
        public bool Conditional;
    }
}
