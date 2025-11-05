using IqraCore.Entities.Helpers;
using IqraCore.Models.User;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserApiKeyController : ControllerBase
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserApiKeyManager _userApiKeyManager;

        public UserApiKeyController(UserSessionValidationHelper userSessionValidationHelper, UserApiKeyManager userApiKeyManager)
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _userApiKeyManager = userApiKeyManager;
        }

        [HttpPost("/app/api-keys/create")]
        public async Task<FunctionReturnResult<UserApiKeyCreateModel?>> CreateUserApiKey([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<UserApiKeyCreateModel?>();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled: true);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"CreateUserApiKey:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

                string? friendlyName = formData["FriendlyName"];
                if (string.IsNullOrWhiteSpace(friendlyName))
                {
                    return result.SetFailureResult(
                        "CreateUserApiKey:INVALID_NAME",
                        "Friendly name is required."
                    );
                }

                var restrictedBusinessIdsRaw = formData["RestrictedBusinessIds[]"];
                var restrictedBusinessIds = new List<long>();
                foreach (var idStr in restrictedBusinessIdsRaw)
                {
                    if (!long.TryParse(idStr, out long parsedId))
                    {
                        return result.SetFailureResult(
                            "CreateUserApiKey:INVALID_ID",
                            $"Invalid business ID '{idStr}'."
                        );
                    }

                    if (!userData.Businesses.Contains(parsedId))
                    {
                        return result.SetFailureResult(
                            "CreateUserApiKey:PERMISSION_DENIED",
                            $"You do not have permission to use business ID {parsedId}."
                        );
                    }
                    restrictedBusinessIds.Add(parsedId);
                }

                var creationResult = await _userApiKeyManager.CreateUserApiKeyAsync(userData, friendlyName, restrictedBusinessIds);
                if (!creationResult.Success)
                {
                    return result.SetFailureResult(
                        $"CreateUserApiKey:{creationResult.Code}",
                        creationResult.Message
                    );
                }

                return result.SetSuccessResult(creationResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "CreateUserApiKey:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/api-keys/delete")]
        public async Task<FunctionReturnResult> DeleteUserApiKey([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled: true);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteUserApiKey:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

                string? userApiKeyId = formData["apiKeyId"];
                if (string.IsNullOrWhiteSpace(userApiKeyId))
                {
                    return result.SetFailureResult(
                        "DeleteUserApiKey:INVALID_ID",
                        "API Key ID is required."
                    );
                }

                if (!userData.UserApiKeys.Any(key => key.Id == userApiKeyId))
                {
                    return result.SetFailureResult(
                        "DeleteUserApiKey:NOT_FOUND",
                        "The specified API Key does not exist or you do not have permission to delete it."
                    );
                }

                var deletionResult = await _userApiKeyManager.DeleteUserApiKeyAsync(userData.Email, userApiKeyId);
                if (!deletionResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteUserApiKey:{deletionResult.Code}",
                        deletionResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteUserApiKey:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }
    }
}