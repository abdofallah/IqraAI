using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;

namespace ProjectIqraFrontend.Middlewares
{
    public class ValidateAPIUserResult
    {
        public UserApiKey? apiKeyData { get; set; }
        public UserData? userData { get; set; }
    }

    public class ValidateAPIUserAndBusinessResult
    {
        public UserApiKey? apiKeyData { get; set; }
        public UserData? userData { get; set; }
        public BusinessData? businessData { get; set; }
    }

    public class UserAPIValidationHelper
    {
        private readonly UserApiKeyManager _userApiKeyManager;
        private readonly BusinessManager _businessManager;

        public UserAPIValidationHelper(UserApiKeyManager userApiKeyManager, BusinessManager businessManager)
        {
            _userApiKeyManager = userApiKeyManager;
            _businessManager = businessManager;
        }

        public async Task<FunctionReturnResult<ValidateAPIUserResult?>> ValidateUserAPIAsync(HttpRequest Request, bool checkUserDisabled = true)
        {
            var result = new FunctionReturnResult<ValidateAPIUserResult?>();

            // Validate session
            var authorizationToken = Request.Headers["Authorization"].ToString();
            var apiKey = authorizationToken.Replace("Token ", "");

            var apiKeyValidaiton = await _userApiKeyManager.ValidateUserApiKeyAsync(apiKey);
            if (!apiKeyValidaiton.IsValid || apiKeyValidaiton.User == null || apiKeyValidaiton.ApiKey == null)
            {
                return result.SetFailureResult(
                    "ValidateUserAPIAsync:INVALID_API_KEY",
                    "Validation failed for the api key."
                );
            }

            var userData = apiKeyValidaiton.User;
            var apiKeyData = apiKeyValidaiton.ApiKey;

            // Get and validate user
            if (checkUserDisabled && userData.Permission.DisableUserAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserAPIAsync:USER_DISABLED",
                    $"User is disabled: {userData.Permission.UserDisabledReason}"
                );
            }

            return result.SetSuccessResult(new ValidateAPIUserResult()
            {
                userData = userData,
                apiKeyData = apiKeyData
            });
        }

        public async Task<FunctionReturnResult<ValidateAPIUserAndBusinessResult?>> ValidateAPIUserAndBusinessSessionAsync(
            HttpRequest Request,
            long businessId,

            bool checkUserDisabled = true,

            bool checkAPIKeyBusinessRestriction = true,

            bool checkBusinessesDisabled = true,
            bool checkBusinessesAddingEnabled = false,
            bool checkBusinessesEditingEnabled = false,
            bool checkBusinessDeletingEnabled = false
        )
        {
            var result = new FunctionReturnResult<ValidateAPIUserAndBusinessResult?>();

            var userSessionValidationResult = await ValidateUserAPIAsync(Request, checkUserDisabled);
            if (!userSessionValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateAPIUserAndBusinessSessionAsync:{userSessionValidationResult.Code}",
                    userSessionValidationResult.Message
                );
            }
            var userData = userSessionValidationResult.Data!.userData!;
            var apiKeyData = userSessionValidationResult.Data!.apiKeyData!;

            // Check API Key Restricted
            if (checkAPIKeyBusinessRestriction && apiKeyData.RestrictedToBusinessIds.Count > 0 && !apiKeyData.RestrictedToBusinessIds.Contains(businessId))
            {
                return result.SetFailureResult(
                    "ValidateAPIUserAndBusinessSessionAsync:RESTRICTED_API_KEY",
                    "API Key is restricted to a different business."
                );
            }

            // Check Business Editing Enabled
            if (checkBusinessesDisabled && userData.Permission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "ValidateAPIUserAndBusinessSessionAsync:BUSINESSES_DISABLED",
                    $"Bussinesses are disabled for the user: {userData.Permission.Business.DisableBusinessesReason}"
                );
            }

            // Check Business Adding Enabled
            if (checkBusinessesAddingEnabled && userData.Permission.Business.AddBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateAPIUserAndBusinessSessionAsync:BUSINESSES_ADDING_DISABLED",
                    $"Bussinesses adding is disabled for the user: {userData.Permission.Business.AddBusinessDisableReason}"
                );
            }

            // Check Business Editing Enabled
            if (checkBusinessesEditingEnabled && userData.Permission.Business.EditBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateAPIUserAndBusinessSessionAsync:BUSINESSES_EDITING_DISABLED",
                    $"Bussinesses editing is disabled for the user: {userData.Permission.Business.EditBusinessDisableReason}"
                );
            }

            // Check Business Deleting Enabled
            if (checkBusinessDeletingEnabled && userData.Permission.Business.DeleteBusinessDisableAt != null)
            {
                return result.SetFailureResult(
                    "ValidateAPIUserAndBusinessSessionAsync:BUSINESSES_DELETING_DISABLED",
                    $"Bussinesses deleting is disabled for the user: {userData.Permission.Business.DeleteBusinessDisableReason}"
                );
            }

            // Get and validate business
            var businessGetResult = await _businessManager.GetUserBusinessById(businessId, userData.Email);
            if (!businessGetResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateAPIUserAndBusinessSessionAsync:{businessGetResult.Code}",
                    businessGetResult.Message
                );
            }

            return result.SetSuccessResult(
                new ValidateAPIUserAndBusinessResult() {
                    apiKeyData = apiKeyData,
                    userData = userData,
                    businessData = businessGetResult.Data
                }
            );
        }
    }
}
