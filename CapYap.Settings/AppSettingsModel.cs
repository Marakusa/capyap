using Newtonsoft.Json;

namespace CapYap.Settings
{
    public abstract class AppSettingsModel
    {
        private string _filePath;

        internal AppSettingsModel()
        {
            _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "usersettings.json");
        }
        internal AppSettingsModel(string path)
        {
            _filePath = path;
        }

        internal void SetPath(string filePath)
        {
            _filePath = filePath;
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var options = new JsonSerializerSettings { Formatting = Formatting.Indented };
            string data = JsonConvert.SerializeObject(this, options);
            File.WriteAllText(_filePath, data);
        }
    }
}
