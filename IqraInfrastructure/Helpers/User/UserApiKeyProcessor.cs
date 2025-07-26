using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IqraCore.Entities.User;
using IqraCore.Utilities;

namespace IqraInfrastructure.Helpers.User
{
    internal class ApiKeyPayload
    {
        public string kid { get; set; }
        public string nonce { get; set; }
    }

    public class UserApiKeyProcessor
    {
        private const string KeyPrefix = "iu_";
        private readonly string _pepper;
        private readonly AES256EncryptionService _userApiKeyEncryptionService;
        private readonly AES256EncryptionService _userApiKeyPayloadEncryptionService;

        public UserApiKeyProcessor(string userApiKeyPepper, AES256EncryptionService userApiKeyEncryptionService, AES256EncryptionService userApiKeyPayloadEncryptionService)
        {
            _pepper = userApiKeyPepper;
            _userApiKeyEncryptionService = userApiKeyEncryptionService;
            _userApiKeyPayloadEncryptionService = userApiKeyPayloadEncryptionService;
        }

        /// <summary>
        /// Generates the final, user-facing API key.
        /// </summary>
        public string Generate(UserData user, string newKeyId)
        {
            string emailHash = ComputeEmailHash(user.Email);

            var payload = new ApiKeyPayload
            {
                kid = newKeyId,
                nonce = GenerateRandomString(32)
            };
            string payloadJson = JsonSerializer.Serialize(payload);
            string encryptedPayload = _userApiKeyPayloadEncryptionService.Encrypt(payloadJson);

            return $"{KeyPrefix}{emailHash}_{encryptedPayload}";
        }

        /// <summary>
        /// Decrypts the payload part of an incoming API key.
        /// This is the inverse of the payload encryption in the Generate method.
        /// </summary>
        public string DecryptPayload(string encryptedPayload)
        {
            return _userApiKeyPayloadEncryptionService.Decrypt(encryptedPayload);
        }

        /// <summary>
        /// A separate method to encrypt the full key for storage.
        /// This ensures what we store is what we compare against.
        /// </summary>
        public string EncryptForStorage(string rawApiKey)
        {
            return _userApiKeyEncryptionService.Encrypt(rawApiKey);
        }

        /// <summary>
        /// Decrypts the full API key from the database for final verification.
        /// This is the inverse of the EncryptForStorage method.
        /// </summary>
        public string DecryptForStorage(string encryptedFullKey)
        {
            return _userApiKeyEncryptionService.Decrypt(encryptedFullKey);
        }

        public string ComputeEmailHash(string email)
        {
            using (var sha256 = SHA256.Create())
            {
                var pepperedEmailBytes = Encoding.UTF8.GetBytes(email.ToLowerInvariant() + _pepper);
                var hashBytes = sha256.ComputeHash(pepperedEmailBytes);
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }
        private string GenerateRandomString(int length)
        {
            const string charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(charset[randomBytes[i] % charset.Length]);
            }
            return result.ToString();
        }
    }
}