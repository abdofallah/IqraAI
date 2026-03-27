using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IqraInfrastructure.Helpers.GitHub
{
    public class GitHubDownloader
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static GitHubDownloader()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("IqraVoiceAIAgent", "1.0"));
        }

        /// <summary>
        /// Downloads a specific file from a GitHub repository, with an option to check for SHA mismatches.
        /// </summary>
        public static async Task DownloadSingleFileAsync(
            string ownerRepo,
            string branch,
            string gitFilePath,
            string localFilePath,
            bool replaceIfShaMismatch = false,
            string? githubToken = null)
        {
            Console.WriteLine($"Checking: {gitFilePath}");

            // 1. If we need to check SHA, fetch the remote SHA from GitHub API
            if (replaceIfShaMismatch && File.Exists(localFilePath))
            {
                string? remoteSha = await GetRemoteFileShaAsync(ownerRepo, branch, gitFilePath, githubToken);
                string localSha = CalculateLocalGitSha(localFilePath);

                // If SHAs match exactly, we can skip the download!
                if (!string.IsNullOrEmpty(remoteSha) && remoteSha.Equals(localSha, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Already latest: {gitFilePath}");
                    return;
                }
            }

            Console.WriteLine($"Downloading: {gitFilePath}");

            // 2. Setup directory
            string? localDirectory = Path.GetDirectoryName(localFilePath);
            if (!string.IsNullOrWhiteSpace(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
            }

            // 3. Download the raw file
            string downloadUrl = $"https://raw.githubusercontent.com/{ownerRepo}/refs/heads/{branch}/{gitFilePath}";
            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

            if (!string.IsNullOrWhiteSpace(githubToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            await contentStream.CopyToAsync(fileStream);

            Console.WriteLine($"Downloaded: {gitFilePath}");
        }

        private static async Task<string?> GetRemoteFileShaAsync(string ownerRepo, string branch, string filePath, string? githubToken)
        {
            // The GitHub Contents API returns the Git SHA and other metadata for a specific file
            string apiUrl = $"https://api.github.com/repos/{ownerRepo}/contents/{filePath}?ref={branch}";

            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            if (!string.IsNullOrWhiteSpace(githubToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
            }

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var jsonString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(jsonString);

            if (jsonDoc.RootElement.TryGetProperty("sha", out JsonElement shaElement))
            {
                return shaElement.GetString();
            }

            return null;
        }

        /// <summary>
        /// Calculates the SHA-1 hash using Git's specific algorithm (blob {size}\0{content}).
        /// </summary>
        private static string CalculateLocalGitSha(string filePath)
        {
            if (!File.Exists(filePath)) return string.Empty;

            using var fileStream = File.OpenRead(filePath);
            long fileSize = fileStream.Length;

            // Git prepends a header before hashing the file
            string header = $"blob {fileSize}\0";
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);

            using var sha1 = SHA1.Create();

            // Hash the header
            sha1.TransformBlock(headerBytes, 0, headerBytes.Length, headerBytes, 0);

            // Hash the file contents in chunks to keep memory usage low
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
    }
}