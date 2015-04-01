using System;
using Exceptionless.Models;

namespace Exceptionless {
    public class ConfigurationUpdatedEventArgs : EventArgs {
        public ConfigurationUpdatedEventArgs(ClientConfiguration configuration) {
            Configuration = configuration;
        }

        public ClientConfiguration Configuration { get; private set; }
    }
}