using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.User;
using IqraCore.Models.User.GetMasterUserDataModel;
using IqraInfrastructure.Helpers.User;
using IqraInfrastructure.Repositories.User;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.User
{
    public class UserApiKeyManager
    {
        private readonly ILogger<UserApiKeyManager> _logger;
        private readonly UserRepository _userRepository;
        private readonly UserApiKeyProcessor _apiKeyProcessor;

        public UserApiKeyManager(ILogger<UserApiKeyManager> logger, UserRepository userRepository, UserApiKeyProcessor apiKeyProcessor)
        {
            _logger = logger;
            _userRepository = userRepository;
            _apiKeyProcessor = apiKeyProcessor;
        }

        public async Task<FunctionReturnResult<UserApiKeyCreateModel?>> CreateUserApiKeyAsync(UserData user, string friendlyName, List<long> restrictedBusinessIds)
        {
            var result = new FunctionReturnResult<UserApiKeyCreateModel?>();

            try
            {
                var newKeyObjectId = ObjectId.GenerateNewId().ToString();

                // 1. Generate the full, final API key using the processor
                string rawApiKey = _apiKeyProcessor.Generate(user, newKeyObjectId);

                // 2. Encrypt the raw key for storage (for direct comparison later)
                string encryptedKeyForStorage = _apiKeyProcessor.EncryptForStorage(rawApiKey);

                // 3. The DisplayName is the public part that's safe to show
                string displayName = rawApiKey.Substring(0, 5) + "..." + rawApiKey.Substring(rawApiKey.Length - 5);

                var newKey = new UserApiKey
                {
                    Id = newKeyObjectId,
                    FriendlyName = friendlyName,
                    EncryptedKey = encryptedKeyForStorage, // Store the encrypted version of the full key
                    DisplayName = displayName,
                    CreatedUtc = DateTime.UtcNow,
                    LastUsedUtc = null,
                    RestrictedToBusinessIds = restrictedBusinessIds ?? new List<long>()
                };

                // Add key to the user's document in the database
                var filter = Builders<UserData>.Filter.Eq(u => u.Email, user.Email);
                var update = Builders<UserData>.Update.AddToSet(u => u.UserApiKeys, newKey);
                bool updateResult = await _userRepository.UpdateUser(filter, update);
                if (!updateResult)
                {
                    return result.SetFailureResult(
                        "CREATE_API_KEY:DB_UPDATE_FAILED",
                        "Could not save the new API key."
                    );
                }

                var createdModel = new UserApiKeyCreateModel
                {
                    RawApiKey = rawApiKey,
                    CreatedKey = new UserApiKeyModel(newKey)
                };

                return result.SetSuccessResult(createdModel);
            }
            catch (Exception ex)
            {
                // Handle exception...
                _logger.LogError(ex, "Exception occurred while creating an API key for {Email}", user.Email);
                return result.SetFailureResult("CREATE_API_KEY:EXCEPTION", "An unexpected error occurred.");
            }
        }

        public async Task<FunctionReturnResult> DeleteUserApiKeyAsync(string userEmail, string userApiKeyId)
        {
            var result = new FunctionReturnResult();

            try
            {
                var filter = Builders<UserData>.Filter.Eq(u => u.Email, userEmail);
                var update = Builders<UserData>.Update.PullFilter(u => u.UserApiKeys, k => k.Id == userApiKeyId);

                var updateResult = await _userRepository.UpdateUser(filter, update);
                if (!updateResult)
                {
                    _logger.LogWarning("Attempted to delete an API key with ID {ApiKeyId} for user {Email}, but it was not found or not modified.", userApiKeyId, userEmail);
                    // We can return success even if not found, as the desired state is achieved.
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deleting API key {ApiKeyId} for {Email}", userApiKeyId, userEmail);
                return result.SetFailureResult("DELETE_API_KEY:EXCEPTION", "An unexpected error occurred.");
            }
        }

        public async Task<(bool IsValid, UserData? User, UserApiKey? ApiKey)> ValidateUserApiKeyAsync(string rawApiKey)
        {
            if (string.IsNullOrEmpty(rawApiKey) || !rawApiKey.StartsWith("iu_"))
            {
                return (false, null, null);
            }

            var keyParts = rawApiKey.Split('_');
            if (keyParts.Length != 3)
            {
                return (false, null, null);
            }

            var emailHash = keyParts[1];
            var encryptedPayload = keyParts[2];

            // Route: Find the user by their email hash
            var user = await _userRepository.GetUserByEmailHashAsync(emailHash);
            if (user == null)
            {
                return (false, null, null); // User not found
            }

            // Decrypt Payload to get the Key ID (kid)
            string kid;
            try
            {
                string payloadJson = _apiKeyProcessor.DecryptPayload(encryptedPayload);
                var payload = JsonSerializer.Deserialize<ApiKeyPayload>(payloadJson);
                if (payload == null || string.IsNullOrEmpty(payload.kid))
                {
                    return (false, null, null); // Invalid payload
                }
                kid = payload.kid;
            }
            catch
            {
                return (false, null, null); // Decryption or deserialization failed
            }

            // Find the specific key metadata in the user's list
            var keyMetadata = user.UserApiKeys.FirstOrDefault(k => k.Id == kid);
            if (keyMetadata == null)
            {
                return (false, user, null); // Key has been revoked
            }

            // VERIFY: Decrypt the stored key and compare it to the incoming key
            try
            {
                string storedFullKey = _apiKeyProcessor.DecryptForStorage(keyMetadata.EncryptedKey);

                // This is the proof of possession. Use a constant-time comparison for security.
                if (CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(storedFullKey),
                        Encoding.UTF8.GetBytes(rawApiKey)))
                {
                    return (true, user, keyMetadata);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt stored API key for kid {KeyId}", kid);
            }

            // If comparison fails or decryption throws an error
            return (false, user, keyMetadata);
        }

        public string HashUserEmail(string userEmail) => _apiKeyProcessor.ComputeEmailHash(userEmail);
    }
}
