using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.User;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.User
{
    [ApiController]
    public class AppUserApiKeyController : ControllerBase
    {
        private readonly UserManager _userManager;
        private readonly UserApiKeyManager _userApiKeyManager;

        public AppUserApiKeyController(UserManager userManager, UserApiKeyManager userApiKeyManager)
        {
            _userManager = userManager;
            _userApiKeyManager = userApiKeyManager;
        }

        /// <summary>
        /// A private helper to validate the user's session and retrieve their data,
        /// reducing code duplication in each endpoint.
        /// </summary>
        private async Task<(FunctionReturnResult<T> Result, UserData? User)> ValidateSessionAndGetUserAsync<T>()
        {
            var result = new FunctionReturnResult<T>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SESSION_INVALID";
                result.Message = "Invalid session data. Please log in again.";
                return (result, null);
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "SESSION_VALIDATION_FAILED";
                result.Message = "Session validation failed. Please log in again.";
                return (result, null);
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "USER_NOT_FOUND";
                result.Message = "User not found.";
                return (result, null);
            }

            return (result, user);
        }

        [HttpPost("/app/api-keys/list")]
        public async Task<FunctionReturnResult<List<UserApiKey>>> ListUserApiKeys()
        {
            var (validationResult, user) = await ValidateSessionAndGetUserAsync<List<UserApiKey>>();
            if (user == null)
            {
                return validationResult;
            }

            // TODO better to return a model instead of data from db

            return validationResult.SetSuccessResult(user.UserApiKeys);
        }

        [HttpPost("/app/api-keys/create")]
        public async Task<FunctionReturnResult<CreateUserApiKeyResponseModel?>> CreateUserApiKey([FromForm] IFormCollection formData)
        {
            var (validationResult, user) = await ValidateSessionAndGetUserAsync<CreateUserApiKeyResponseModel?>();
            if (user == null)
            {
                return validationResult;
            }

            string? friendlyName = formData["FriendlyName"];
            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                return validationResult.SetFailureResult("CREATE:INVALID_NAME", "Friendly name is required.");
            }

            var restrictedBusinessIdsRaw = formData["RestrictedBusinessIds[]"];
            var restrictedBusinessIds = new List<long>();
            foreach (var idStr in restrictedBusinessIdsRaw)
            {
                if (long.TryParse(idStr, out long id))
                {
                    // Security Check: Ensure the user owns the business they are restricting the key to.
                    if (!user.Businesses.Contains(id))
                    {
                        return validationResult.SetFailureResult("CREATE:PERMISSION_DENIED", $"You do not have permission to use business ID {id}.");
                    }
                    restrictedBusinessIds.Add(id);
                }
            }

            var creationResult = await _userApiKeyManager.CreateUserApiKeyAsync(user, friendlyName, restrictedBusinessIds);
            if (!creationResult.Success)
            {
                validationResult.Code = creationResult.Code;
                validationResult.Message = creationResult.Message;
                return validationResult;
            }

            // Map the result from the manager to the specific response model the frontend expects.
            var responseModel = new CreateUserApiKeyResponseModel
            {
                Id = creationResult.Data.CreatedKey.Id,
                FriendlyName = creationResult.Data.CreatedKey.FriendlyName,
                DisplayName = creationResult.Data.CreatedKey.DisplayName,
                ApiKey = creationResult.Data.RawApiKey,
                Created = creationResult.Data.CreatedKey.CreatedUtc,
                LastUsed = creationResult.Data.CreatedKey.LastUsedUtc
            };

            return validationResult.SetSuccessResult(responseModel);
        }

        [HttpPost("/app/api-keys/delete")]
        public async Task<FunctionReturnResult> DeleteUserApiKey([FromForm] IFormCollection formData)
        {
            var (validationResult, user) = await ValidateSessionAndGetUserAsync<object>(); // Type param doesn't matter for this one
            if (user == null)
            {
                return validationResult;
            }

            string? userApiKeyId = formData["apiKeyId"];
            if (string.IsNullOrWhiteSpace(userApiKeyId))
            {
                return validationResult.SetFailureResult("DELETE:INVALID_ID", "API Key ID is required.");
            }

            // Security Check: Ensure the key being deleted actually belongs to the user.
            if (!user.UserApiKeys.Any(key => key.Id == userApiKeyId))
            {
                return validationResult.SetFailureResult("DELETE:NOT_FOUND", "The specified API Key does not exist or you do not have permission to delete it.");
            }

            var deletionResult = await _userApiKeyManager.DeleteUserApiKeyAsync(user.Email, userApiKeyId);

            if (!deletionResult.Success)
            {
                validationResult.Code = deletionResult.Code;
                validationResult.Message = deletionResult.Message;
                return validationResult;
            }

            return validationResult.SetSuccessResult();
        }
    }
}