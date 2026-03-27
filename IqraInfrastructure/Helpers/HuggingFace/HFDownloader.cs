using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Helpers.HuggingFace
{
    public class HFDownloader
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        public static async Task DownloadModelAsync(string repoId, List<string>? filesToDownload, string outputFolder, bool replaceIfShaMismatch = false, string? hfToken = null)
        {
            if (string.IsNullOrWhiteSpace(repoId)) throw new ArgumentException("Repository ID cannot be null.", nameof(repoId));
            if (string.IsNullOrWhiteSpace(outputFolder)) throw new ArgumentException("Output folder cannot be null.", nameof(outputFolder));

            if (filesToDownload == null || filesToDownload.Count == 0)
            {
                filesToDownload = await GetRepositoryFilesAsync(repoId, hfToken);
            }

            Directory.CreateDirectory(outputFolder);

            foreach (var fileName in filesToDownload)
            {
                await DownloadFileAsync(repoId, fileName, outputFolder, replaceIfShaMismatch, hfToken);
            }
        }

        private static async Task DownloadFileAsync(string repoId, string fileName, string outputFolder, bool replaceIfShaMismatch, string? hfToken)
        {
            string downloadUrl = $"https://huggingface.co/{repoId}/resolve/main/{fileName}";

            Console.WriteLine($"Checking: {downloadUrl}");

            string safeFileName = Path.Combine(fileName.Split('/'));
            string localFilePath = Path.Combine(outputFolder, safeFileName);

            if (replaceIfShaMismatch && File.Exists(localFilePath))
            {
                string? remoteHash = await GetRemoteFileHashAsync(repoId, fileName, hfToken);
                if (!string.IsNullOrEmpty(remoteHash))
                {
                    string localHash = CalculateLocalHash(localFilePath, remoteHash.Length);

                    if (remoteHash.Equals(localHash, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Already latest: {downloadUrl}");
                        return;
                    }
                }
            }

            Console.WriteLine($"Downloading: {downloadUrl}");

            string? localDirectory = Path.GetDirectoryName(localFilePath);
            if (!string.IsNullOrWhiteSpace(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            if (!string.IsNullOrWhiteSpace(hfToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            await contentStream.CopyToAsync(fileStream);

            Console.WriteLine($"Downloaded Successfully: {downloadUrl}");
        }

        private static async Task<string?> GetRemoteFileHashAsync(string repoId, string fileName, string? hfToken)
        {
            string apiUrl = $"https://huggingface.co/api/models/{repoId}/paths-info/main";

            var payload = new { paths = new[] { fileName } };
            string jsonContent = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            if (!string.IsNullOrWhiteSpace(hfToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);
            }

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            string jsonString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            // The API returns an array of PathInfo objects
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var fileInfo = root[0];

                // 1. If the file is tracked by LFS (even if backed by Xet), it maintains an "lfs" block.
                // The lfs.oid is always the standard SHA-256 hash.
                if (fileInfo.TryGetProperty("lfs", out JsonElement lfs) &&
                    lfs.TryGetProperty("oid", out JsonElement lfsOid))
                {
                    return lfsOid.GetString(); // 64-char standard SHA-256
                }

                // 2. Otherwise, it is a standard small Git blob, and the hash is in "oid".
                if (fileInfo.TryGetProperty("oid", out JsonElement oid))
                {
                    return oid.GetString(); // 40-char Git SHA-1
                }
            }

            return null;
        }

        private static string CalculateLocalHash(string filePath, int expectedHashLength)
        {
            if (!File.Exists(filePath)) return string.Empty;

            using var fileStream = File.OpenRead(filePath);

            // If the hash is 64 characters long, it's the standard LFS SHA-256
            // We no longer need custom Xet hashing logic since the API provides the original SHA-256!
            if (expectedHashLength == 64)
            {
                using var sha256 = SHA256.Create();
                byte[] hashBytes = sha256.ComputeHash(fileStream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            // If the hash is 40 characters long, Hugging Face tracked it as a standard Git blob (Git SHA-1)
            if (expectedHashLength == 40)
            {
                long fileSize = fileStream.Length;
                string header = $"blob {fileSize}\0";
                byte[] headerBytes = Encoding.ASCII.GetBytes(header);

                using var sha1 = SHA1.Create();
                sha1.TransformBlock(headerBytes, 0, headerBytes.Length, headerBytes, 0);

                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sha1.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                }
                sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                if (sha1.Hash == null) return string.Empty;
                return BitConverter.ToString(sha1.Hash).Replace("-", "").ToLowerInvariant();
            }

            return string.Empty;
        }

        private static async Task<List<string>> GetRepositoryFilesAsync(string repoId, string? hfToken)
        {
            string apiUrl = $"https://huggingface.co/api/models/{repoId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

            if (!string.IsNullOrWhiteSpace(hfToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);
            }

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;
            var files = new List<string>();

            if (root.TryGetProperty("siblings", out JsonElement siblings))
            {
                foreach (var sibling in siblings.EnumerateArray())
                {
                    if (sibling.TryGetProperty("rfilename", out JsonElement rfilename))
                    {
                        var fileName = rfilename.GetString();
                        if (!string.IsNullOrWhiteSpace(fileName))
                        {
                            files.Add(fileName);
                        }
                    }
                }
            }
            return files;
        }
    }
}