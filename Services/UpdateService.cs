using System.Reflection;

namespace JenkinsAgent.Services
{
    public static class UpdateService
    {
        // Son sürüm bilgisi için GitHub API endpoint'i
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/mustafagenc/jenkins-agent/releases/latest";

        public static string GetLocalVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        }

        public static async Task<UpdateInfo?> GetRemoteUpdateInfoAsync()
        {
            try
            {
                using var client = new HttpClient();
                // GitHub API User-Agent zorunlu
                client.DefaultRequestHeaders.UserAgent.ParseAdd("JenkinsAgent-Updater");
                var response = await client.GetAsync(LatestReleaseApiUrl);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var info = new UpdateInfo();
                if (doc.RootElement.TryGetProperty("tag_name", out var tag))
                {
                    info.Version = tag.GetString() ?? "";
                }
                // Release assetlerinden ilk .exe dosyasını bul
                if (doc.RootElement.TryGetProperty("assets", out var assets) && assets.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        if (asset.TryGetProperty("browser_download_url", out var urlProp))
                        {
                            var url = urlProp.GetString() ?? "";
                            if (url.EndsWith(".exe"))
                            {
                                info.DownloadUrl = url;
                                break;
                            }
                        }
                    }
                }
                // Eğer exe bulunamazsa release sayfasını ver
                if (string.IsNullOrWhiteSpace(info.DownloadUrl) && doc.RootElement.TryGetProperty("html_url", out var htmlUrl))
                {
                    info.DownloadUrl = htmlUrl.GetString() ?? string.Empty;
                }
                return info;
            }
            catch (Exception ex)
            {
                // throw new Exception($"Failed to fetch update info from GitHub: {ex.Message}");
                return null;
            }
        }

        public static async Task<bool> IsUpdateAvailableAsync()
        {
            var local = GetLocalVersion();
            var remoteInfo = await GetRemoteUpdateInfoAsync();
            if (remoteInfo == null || string.IsNullOrWhiteSpace(remoteInfo.Version)) return false;
            try
            {
                var localVer = new Version(local);
                var remoteVer = new Version(remoteInfo.Version);
                return remoteVer > localVer;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<string?> GetRemoteDownloadUrlAsync()
        {
            var info = await GetRemoteUpdateInfoAsync();
            return info?.DownloadUrl;
        }
    }
}
