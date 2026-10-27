using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace InstantDealingApp
{
    public class UpdateManifest
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonProperty("releaseNotes")]
        public string ReleaseNotes { get; set; } = string.Empty;
    }

    public static class UpdateService
    {
        public const string CurrentVersion = "1.0.1";

        public static async Task<UpdateManifest?> CheckForUpdatesAsync(string repoOwner, string repoName, string branch)
        {
            if (string.IsNullOrWhiteSpace(repoOwner) || string.IsNullOrWhiteSpace(repoName))
            {
                repoOwner = "FaazLaeeq";
                repoName = "InstantDealingApp";
            }
            if (string.IsNullOrWhiteSpace(branch))
            {
                branch = "xrf-multi-test";
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "InstantDealingApp-Updater");
                client.Timeout = TimeSpan.FromSeconds(10);

                // 1. Try raw version.json from the branch
                string rawUrl = $"https://raw.githubusercontent.com/{repoOwner}/{repoName}/{branch}/version.json";
                HttpResponseMessage response = await client.GetAsync(rawUrl);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var manifest = JsonConvert.DeserializeObject<UpdateManifest>(json);
                    if (manifest != null && IsNewerVersion(manifest.Version, CurrentVersion))
                    {
                        return manifest;
                    }
                }

                // 2. Fallback to GitHub Releases API
                string releaseApiUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases/latest";
                HttpResponseMessage relResponse = await client.GetAsync(releaseApiUrl);
                if (relResponse.IsSuccessStatusCode)
                {
                    string relJson = await relResponse.Content.ReadAsStringAsync();
                    dynamic? relData = JsonConvert.DeserializeObject(relJson);
                    if (relData != null)
                    {
                        string tagName = (string)(relData.tag_name ?? "");
                        string cleanTag = tagName.TrimStart('v', 'V');
                        if (IsNewerVersion(cleanTag, CurrentVersion))
                        {
                            string downloadUrl = "";
                            if (relData.assets != null)
                            {
                                foreach (var asset in relData.assets)
                                {
                                    string name = (string)asset.name;
                                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                                        name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                    {
                                        downloadUrl = (string)asset.browser_download_url;
                                        break;
                                    }
                                }
                            }

                            return new UpdateManifest
                            {
                                Version = cleanTag,
                                DownloadUrl = downloadUrl,
                                ReleaseNotes = (string)(relData.body ?? "New release available.")
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] Check update error: {ex.Message}");
            }

            return null;
        }

        public static bool IsNewerVersion(string remoteVerStr, string currentVerStr)
        {
            if (string.IsNullOrWhiteSpace(remoteVerStr)) return false;
            try
            {
                if (Version.TryParse(remoteVerStr.TrimStart('v', 'V'), out Version? remote) &&
                    Version.TryParse(currentVerStr.TrimStart('v', 'V'), out Version? current))
                {
                    return remote > current;
                }
            }
            catch { }
            return false;
        }

        public static async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, Action<int>? onProgress = null)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl)) return false;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "InstantDealingApp-Updater");

                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                string tempDir = Path.GetTempPath();
                string ext = Path.GetExtension(downloadUrl);
                if (string.IsNullOrEmpty(ext)) ext = ".exe";
                string tempFile = Path.Combine(tempDir, $"InstantDealingSetup_Update_{Guid.NewGuid():N}{ext}");

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    byte[] buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        if (totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            int progress = (int)((totalRead * 100) / totalBytes.Value);
                            onProgress?.Invoke(progress);
                        }
                    }
                }

                // If downloaded a zip, extract the exe
                string exeToRun = tempFile;
                if (tempFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string extractDir = Path.Combine(tempDir, "InstantDealingUpdateExtracted_" + Guid.NewGuid().ToString("N"));
                    System.IO.Compression.ZipFile.ExtractToDirectory(tempFile, extractDir);
                    string[] foundExes = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories);
                    if (foundExes.Length > 0)
                    {
                        exeToRun = foundExes[0];
                    }
                }

                // Launch installer and shut down current app
                Process.Start(new ProcessStartInfo
                {
                    FileName = exeToRun,
                    UseShellExecute = true
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] Download error: {ex.Message}");
                return false;
            }
        }
    }
}
