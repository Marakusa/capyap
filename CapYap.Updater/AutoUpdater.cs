using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CapYap.Updater
{
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    public class GitHubAsset
    {
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";

        [JsonPropertyName("tag_name")]
        public string Name { get; set; } = "";
    }

    public class AutoUpdater
    {
        private readonly string _repoOwner;
        private readonly string _repoName;
        private readonly string _currentVersion;

        private static readonly HttpClient _http = new();

        public AutoUpdater(string repoOwner, string repoName, string currentVersion)
        {
            _repoOwner = repoOwner;
            _repoName = repoName;
            _currentVersion = currentVersion;
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CapYapUpdater", currentVersion));
        }

        public async Task<bool> CheckAndUpdateAsync()
        {
            var latest = await GetLatestReleaseAsync();
            if (latest == null) return false;

            if (!UpToDate(latest.TagName))
            {
                Console.WriteLine($"New version {latest.TagName} found! Downloading...");
                var asset = latest.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe"));
                if (asset != null)
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), asset.Name);
                    await DownloadFileAsync(asset.BrowserDownloadUrl, tempPath);

                    // If it's a zip, extract and replace
                    if (asset.Name.EndsWith(".exe"))
                    {
                        Console.WriteLine("Downloaded installer. Run installer...");
                        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                    }
                    else
                    {
                        Console.WriteLine("No installer found.");
                        return false;
                    }

                    return true;
                }
            }

            Console.WriteLine("No updates found.");
            return false;
        }

        private async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            string url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
            var json = await _http.GetStringAsync(url);
            return JsonSerializer.Deserialize<GitHubRelease>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private bool UpToDate(string latestTag)
        {
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
