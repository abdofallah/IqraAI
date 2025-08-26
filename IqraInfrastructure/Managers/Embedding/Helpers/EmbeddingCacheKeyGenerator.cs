using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.Embedding;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Embedding.Helpers
{
    public static class EmbeddingCacheKeyGenerator
    {
        private static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = false };

        public static string Generate(string text, InterfaceEmbeddingProviderEnum providerType, IEmbeddingConfig config)
        {
            // Serialize the config to a compact, deterministic JSON string.
            string settingsJson = JsonSerializer.Serialize(config, config.GetType(), _serializerOptions);

            // Combine the components into a single string. The pipe delimiter prevents ambiguity.
            string combinedString = $"{text.Trim()}|{(int)providerType}|{settingsJson}";

            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(combinedString);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // Convert to a URL-safe Base64 string by replacing special characters and removing padding.
                string base64 = Convert.ToBase64String(hashBytes);
                string safeBase64 = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
                return safeBase64;
            }
        }
    }
}
