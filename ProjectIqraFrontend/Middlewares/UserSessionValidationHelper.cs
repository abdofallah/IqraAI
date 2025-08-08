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

        public async Task<FunctionReturnResult<UserData>> ValidateUserSessionAsync(HttpRequest Request, bool checkUserDisabled = true)
        {
            var result = new FunctionReturnResult<UserData>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult("ValidateSessionAsync:INVALID_SESSION_DATA", "Invalid session data");
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                return result.SetFailureResult("ValidateSessionAsync:SESSION_VALIDATION_FAILED", "Session validation failed");
            }

            // Get and validate user
            var userData = await _userManager.GetUserByEmail(userEmail);
            if (userData == null)
            {
                return result.SetFailureResult("ValidateSessionAsync:USER_DATA_NOT_FOUND", "User not found");
            }

            if (checkUserDisabled && userData.Permission.DisableUserAt != null)
            {
                return result.SetFailureResult(
                    "ValidateSessionAsync:USER_DISABLED",
                    $"User is disabled: {userData.Permission.UserDisabledReason}"
                );
            }

            return result.SetSuccessResult(userData);
        }

        public async Task<FunctionReturnResult<ValidateUserAndBusinessResult?>> ValidateUserAndBusinessSessionAsync(
            HttpRequest Request,
            long businessId,

            bool checkUserDisabled = true,
            
            bool checkBusinessesDisabled = true,
            bool checkBusinessesAddingEnabled = false,
            bool checkBusinessesEditingEnabled = false,
            bool checkBusinessDeletingEnabled = false
        )
        {
            var result = new FunctionReturnResult<ValidateUserAndBusinessResult?>();

            var userSessionValidationResult = await ValidateUserSessionAsync(Request, checkUserDisabled);
            if (!userSessionValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserAndBusinessSessionAsync:{userSessionValidationResult.Code}",
                    userSessionValidationResult.Message
                );
            }
            var userData = userSessionValidationResult.Data;

            // Check Business Editing Enabled
            if (checkBusinessesDisabled && userData.Permission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "ValidateSessionAsync:BUSINESSES_DISABLED",
                    $"Bussinesses are disabled for the user: {userData.Permission.Business.DisableBusinessesReason}"
                );
            }

            // Check Business Adding Enabled
            if (checkBusinessesAddingEnabled && userData.Permission.Business.AddBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateSessionAsync:BUSINESSES_ADDING_DISABLED",
                    $"Bussinesses adding is disabled for the user: {userData.Permission.Business.AddBusinessDisableReason}"
                );
            }

            // Check Business Editing Enabled
            if (checkBusinessesEditingEnabled && userData.Permission.Business.EditBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateSessionAsync:BUSINESSES_EDITING_DISABLED",
                    $"Bussinesses editing is disabled for the user: {userData.Permission.Business.EditBusinessDisableReason}"
                );
            }

            // Check Business Deleting Enabled
            if (checkBusinessDeletingEnabled && userData.Permission.Business.DeleteBusinessDisableAt != null)
            {
                return result.SetFailureResult(
                    "ValidateSessionAsync:BUSINESSES_DELETING_DISABLED",
                    $"Bussinesses deleting is disabled for the user: {userData.Permission.Business.DeleteBusinessDisableReason}"
                );
            }

            // Get and validate business
            var businessGetResult = await _businessManager.GetUserBusinessById(businessId, userData.Email);
            if (!businessGetResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateSessionAsync:{businessGetResult.Code}",
                    businessGetResult.Message
                );
            }

            return result.SetSuccessResult(new ValidateUserAndBusinessResult() { userData = userData, businessData = businessGetResult.Data });
        }
    }
}
