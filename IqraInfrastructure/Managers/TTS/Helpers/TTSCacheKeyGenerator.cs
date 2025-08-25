using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.TTS;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.TTS.Helpers
{
    public static class TTSCacheKeyGenerator
    {
        private static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = false };

        public static string Generate(string text, InterfaceTTSProviderEnum providerType, ITTSConfig config)
        {
            string settingsJson = JsonSerializer.Serialize(config, config.GetType(), _serializerOptions);

            string combinedString = $"{text.Trim().ToLower()}|{(int)providerType}|{settingsJson}";

            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(combinedString);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                string base64 = Convert.ToBase64String(hashBytes);
                string safeBase64 = base64.Replace('+', '-').Replace('/', '_').Replace('\\', '>').TrimEnd('=');
                return safeBase64;
            }
        }
    }
}
