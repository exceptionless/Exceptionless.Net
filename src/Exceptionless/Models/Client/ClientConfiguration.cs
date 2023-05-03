namespace Exceptionless.Models {
    [Json.JsonObject(NamingStrategyType = typeof(Json.Serialization.SnakeCaseNamingStrategy))]
    public class ClientConfiguration {
        public ClientConfiguration() {
            Settings = new SettingsDictionary();
        }

        public int Version { get; set; }
        public SettingsDictionary Settings { get; private set; }

        public void IncrementVersion() {
            Version++;
        }
    }
}