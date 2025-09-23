using Newtonsoft.Json;

namespace CapYap.Settings
{
    public class AppSettings
    {
        [JsonProperty]
        public bool AutoStart { get; set; } = true;

        [JsonProperty]
        public string Theme { get; set; } = "theme_dark";
    }

    public class WindowSettings
    {
        [JsonProperty]
        public int Width { get; set; } = 1720;

        [JsonProperty]
        public int Height { get; set; } = 880;

        [JsonProperty]
        public bool Maximized { get; set; } = false;
    }

    public class UploadSettings
    {
        [JsonProperty]
        public int CompressionQuality { get; set; } = 92;

        [JsonProperty]
        public int AnimCompressionQuality { get; set; } = 70;

        [JsonProperty]
        public int CompressionLevel { get; set; } = 6;

        [JsonProperty]
        public int UploadFormat { get; set; } = 0;
    }

    public class UserSettings : AppSettingsModel
    {
        [JsonProperty]
        public AppSettings AppSettings { get; set; } = new();

        [JsonProperty]
        public UploadSettings UploadSettings { get; set; } = new();

        [JsonProperty]
        public WindowSettings WindowSettings { get; set; } = new();

        internal UserSettings() : base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "usersettings.json"))
        {
        }

        internal UserSettings(string path) : base(path)
        {
        }
    }

    public static class UserSettingsManager
    {
        private static readonly string _filePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "usersettings.json");

        private static UserSettings? _currentUserSettings;
        public static UserSettings Current
        {
            get
            {
                _currentUserSettings ??= Load();

                return _currentUserSettings;
            }
        }

        private static UserSettings Load()
        {
            if (!File.Exists(_filePath))
                return new UserSettings(_filePath);

            var json = File.ReadAllText(_filePath);
            var data = JsonConvert.DeserializeObject<UserSettings>(json) ?? new UserSettings();
            data.SetPath(_filePath);
            return data;
        }
    }
}
