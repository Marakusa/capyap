using Serilog;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CapYap.Updater
{
    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; } = "";

        [JsonProperty("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    public class GitHubAsset
    {
        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";
    }

    public class AutoUpdater
    {
        private readonly ILogger _log;
        private readonly string _repoOwner;
        private readonly string _repoName;
        private readonly string _currentVersion;

        private static readonly HttpClient _http = new();

        public AutoUpdater(ILogger log, string repoOwner, string repoName, string currentVersion)
        {
            _log = log;
            _repoOwner = repoOwner;
            _repoName = repoName;
            _currentVersion = currentVersion;
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CapYapUpdater", currentVersion));
        }

        public async Task<bool> CheckAndUpdateAsync()
        {
            var latest = await GetLatestReleaseAsync();
            if (latest == null)
            {
                _log.Error("Latest tag not found...");
                return false;
            }

            if (!UpToDate(latest.TagName))
            {
                _log.Information($"New version {latest.TagName} found! Downloading...");
                var asset = latest.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe"));
                _log.Information(JsonConvert.SerializeObject(asset));
                if (asset != null)
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), asset.Name);
                    await DownloadFileAsync(asset.BrowserDownloadUrl, tempPath);

                    // If it's a zip, extract and replace
                    if (asset.Name.EndsWith(".exe"))
                    {
                        _log.Information("Downloaded installer. Run installer...");
                        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                    }
                    else
                    {
                        _log.Error("No installer found.");
                        return false;
                    }

                    return true;
                }
                else
                {
                    _log.Error("No installer found.");
                    return false;
                }
            }

            _log.Information("No updates found.");
            return false;
        }

        private async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            string url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
            var json = await _http.GetStringAsync(url);
            return JsonConvert.DeserializeObject<GitHubRelease>(json);
        }

        private bool UpToDate(string latestTag)
        {
            _log.Information($"Current version: {_currentVersion} Latest version: {latestTag}");
            return latestTag == _currentVersion;
        }

        private async Task DownloadFileAsync(string url, string filePath)
        {
            using var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fs);
        }
    }
}
