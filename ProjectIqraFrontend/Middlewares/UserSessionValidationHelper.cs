using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;

namespace ProjectIqraFrontend.Middlewares
{
    public class ValidateUserAndBusinessResult
    {
        public UserData? userData { get; set; }
        public BusinessData? businessData { get; set; }
    }

    public class UserSessionValidationHelper
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public UserSessionValidationHelper(UserManager userManager, BusinessManager businessManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
        }

        public async Task<FunctionReturnResult<string?>> ValidateUserSessionAsync(HttpRequest Request)
        {
            var result = new FunctionReturnResult<string?>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult("ValidateUserSessionAsync:INVALID_SESSION_DATA", "Invalid session data");
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                return result.SetFailureResult("ValidateUserSessionAsync:SESSION_VALIDATION_FAILED", "Session validation failed");
            }

            return result.SetSuccessResult(userEmail);
        }

        public async Task<FunctionReturnResult<UserData>> ValidateUserSessionAndGetUserAsync(HttpRequest Request, bool checkUserDisabled = true)
        {
            var result = new FunctionReturnResult<UserData>();

            // Validate session
            var validateUserSessionResult = await ValidateUserSessionAsync(Request);
            if (!validateUserSessionResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionAndGetUserAsync:{validateUserSessionResult.Code}",
                    validateUserSessionResult.Message
                );
            }
            var userEmail = validateUserSessionResult.Data!;

            // Get and validate user
            var userData = await _userManager.GetFullUserByEmail(userEmail);
            if (userData == null)
            {
                return result.SetFailureResult("ValidateUserSessionAndGetUserAsync:USER_DATA_NOT_FOUND", "User not found");
            }

            if (checkUserDisabled && userData.Permission.DisableUserAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAsync:USER_DISABLED",
                    $"User is disabled: {userData.Permission.UserDisabledReason}"
                );
            }

            return result.SetSuccessResult(userData);
        }

        public async Task<FunctionReturnResult<ValidateUserAndBusinessResult?>> ValidateUserSessionAndGetUserAndBusinessAsync(
            HttpRequest Request,
            long businessId,

            bool checkUserDisabled = true,       
            
            bool checkUserBusinessesDisabled = true,
            bool checkUserBusinessesAddingEnabled = false,
            bool checkUserBusinessesEditingEnabled = false,
            bool checkUserBusinessesDeletingEnabled = false,

            bool checkBusinessIsDisabled = true,
            bool checkBusinessCanBeEdited = false,
            bool checkBusinessCanBeDeleted = false
        )
        {
            var result = new FunctionReturnResult<ValidateUserAndBusinessResult?>();

            var userSessionValidationResult = await ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled);
            if (!userSessionValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionAndGetUserAndBusinessAsync:{userSessionValidationResult.Code}",
                    userSessionValidationResult.Message
                );
            }
            var userData = userSessionValidationResult.Data!;

            // Check User Businesses Full Enabled
            if (checkUserBusinessesDisabled && userData.Permission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:USER_BUSINESSES_DISABLED",
                    $"Bussinesses are disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.DisableBusinessesReason) ? "" : ": " + userData.Permission.Business.DisableBusinessesReason)}"
                );
            }

            // Check User Businesses Adding Enabled
            if (checkUserBusinessesAddingEnabled && userData.Permission.Business.AddBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:USER_BUSINESSES_ADDING_DISABLED",
                    $"Bussinesses adding is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.AddBusinessDisableReason) ? "" : ": " + userData.Permission.Business.AddBusinessDisableReason)}"
                );
            }

            // Check User Businesses Editing Enabled
            if (checkUserBusinessesEditingEnabled && userData.Permission.Business.EditBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:USER_BUSINESSES_EDITING_DISABLED",
                    $"Bussinesses editing is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.EditBusinessDisableReason) ? "" : ": " + userData.Permission.Business.EditBusinessDisableReason)}"
                );
            }

            // Check User Businesses Deleting Enabled
            if (checkUserBusinessesDeletingEnabled && userData.Permission.Business.DeleteBusinessDisableAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:USER_BUSINESSES_DELETING_DISABLED",
                    $"Bussinesses deleting is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.DeleteBusinessDisableReason) ? "" : ": " + userData.Permission.Business.DeleteBusinessDisableReason)}"
                );
            }

            // Get and validate business
            var businessGetResult = await _businessManager.GetUserBusinessById(businessId, userData.Email);
            if (!businessGetResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionAndGetUserAndBusinessAsync:{businessGetResult.Code}",
                    businessGetResult.Message
                );
            }
            var businessData = businessGetResult.Data!;

            // Check Business Full Disabled
            if (checkBusinessIsDisabled && businessData.Permission.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_DISABLED",
                    $"Business is disabled{(string.IsNullOrWhiteSpace(businessData.Permission.DisabledFullReason) ? "" : ": " + businessData.Permission.DisabledFullReason)}"
                );
            }

            // Check Business Editing Disabled
            if (checkBusinessCanBeEdited && businessData.Permission.DisabledEditingAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_EDITING_DISABLED",
                    $"Business editing is disabled{(string.IsNullOrWhiteSpace(businessData.Permission.DisabledEditingReason) ? "" : ": " + businessData.Permission.DisabledEditingReason)}"
                );
            }

            // Check Business Deleting Disabled
            if (checkBusinessCanBeDeleted && businessData.Permission.DisabledDeletingAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_DELETING_DISABLED",
                    $"Business deleting is disabled{(string.IsNullOrWhiteSpace(businessData.Permission.DisabledDeletingReason) ? "" : ": " + businessData.Permission.DisabledDeletingReason)}"
                );
            }

            return result.SetSuccessResult(new ValidateUserAndBusinessResult() { userData = userData, businessData = businessData });
        }
    }
}
