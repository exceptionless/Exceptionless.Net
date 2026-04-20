using NLog.Config;
using NLog.Layouts;

namespace Exceptionless.NLog {
    [NLogConfigurationItem]
    public class ExceptionlessField {
        public string Name { get; set; }

        public Layout Layout { get; set; }
    }
}
