using System.Text.Json.Serialization;

namespace Exceptionless.Models {

    public class ClientConfiguration {
        public ClientConfiguration() {
            Settings = new SettingsDictionary();
        }

        public int Version { get; set; }

        [JsonInclude]
        public SettingsDictionary Settings { get; internal set; }

        public void IncrementVersion() {
            Version++;
        }
    }
}
