namespace Exceptionless.Models {

    public class ClientConfiguration {
        public ClientConfiguration() {
            Settings = new SettingsDictionary();
        }

        public int Version { get; set; }
        public SettingsDictionary Settings { get; set; }

        public void IncrementVersion() {
            Version++;
        }
    }
}