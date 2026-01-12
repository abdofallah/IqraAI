using IqraCore.Cloud.Entities.WhiteLabel;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.User;
using Microsoft.AspNetCore.Http;
using IqraCore.Entities.Validation;

namespace IqraInfrastructure.Helpers.Validation
{
    public class SessionValidationAndPermissionHelper : ISessionValidationAndPermissionHelper
    {
        private readonly UserApiKeyManager _userApiKeyManager;
        public readonly UserManager _userManager;
        public readonly BusinessManager _businessManager;
        public readonly UserRepository _userRepository;
        public readonly IUserBusinessPermissionHelper _userBusinessPermissionHelper;

        public SessionValidationAndPermissionHelper(
            UserApiKeyManager userApiKeyManager,
            UserManager userManager,
            BusinessManager businessManager,
            UserRepository userRepository,
            IUserBusinessPermissionHelper userBusinessPermissionHelper
        )
        {
            _userApiKeyManager = userApiKeyManager;
            _userManager = userManager;
            _businessManager = businessManager;
            _userRepository = userRepository;
            _userBusinessPermissionHelper = userBusinessPermissionHelper;
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
        public virtual async Task<FunctionReturnResult<WhiteLabelSessionData?>> ValidateWhiteLabelCustomerSessionAsync(HttpRequest Request, WhiteLabelContext whiteLabelContext)
        {
            // handled by cloud module
            throw new NotImplementedException("ValidateWhiteLabelCustomerSessionAsync: Called when it should not have been called");
        }

        public virtual async Task<FunctionReturnResult<ValidateUserResult>> ValidateUserSessionWithPermissions(
            HttpRequest Request,
            WhiteLabelContext? whiteLabelContext = null,
            // User Permissions
            bool checkUserIsAdmin = false,
            bool checkUserDisabled = true,
            // User Businesses Permissions
            bool checkUserBusinessesDisabled = false,
            bool checkUserBusinessesAddingEnabled = false,
            bool checkUserBusinessesEditingEnabled = false,
            bool checkUserBusinessesDeletingEnabled = false,
            // User WhiteLabel Permissions
            bool checkUserWhiteLabelDisabled = false,
            bool checkUserWhiteLabelEditingDisabled = false
        ) {
            var result = new FunctionReturnResult<ValidateUserResult>();

            // Validate session
            string? userEmail = null;
            var validateUserSessionResult = await ValidateUserSessionAsync(Request);
            if (!validateUserSessionResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionWithPermissions:{validateUserSessionResult.Code}",
                    validateUserSessionResult.Message
                );
            }
            userEmail = validateUserSessionResult.Data!;

            // Get and validate user
            UserData? userData = await _userManager.GetFullUserByEmail(userEmail);
            if (userData == null)
            {
                return result.SetFailureResult("ValidateUserSessionWithPermissions:USER_DATA_NOT_FOUND", "User not found");
            }

            // Validate User Permissions
            var validatePermission = ValidateUserPermissions(
                userData.Permission,
                // User Permissions
                checkUserIsAdmin,
                checkUserDisabled,
                // Business Permissions
                checkUserBusinessesDisabled,
                checkUserBusinessesAddingEnabled,
                checkUserBusinessesEditingEnabled,
                checkUserBusinessesDeletingEnabled,
                // WhiteLabel Permissions
                checkUserWhiteLabelDisabled,
                checkUserWhiteLabelEditingDisabled
            );
            if (!validatePermission.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionWithPermissions:{validatePermission.Code}",
                    validatePermission.Message
                );
            }

            return result.SetSuccessResult(new ValidateUserResult()
            {
                userData = userData
            });
        }
        public virtual async Task<FunctionReturnResult<ValidateUserAndBusinessResult?>> ValidateUserSessionAndBusinessWithPermissions(
            HttpRequest Request,
            long businessId,
            WhiteLabelContext? whiteLabelContext = null,
            // User Permissions
            bool checkUserIsAdmin = false,
            bool checkUserDisabled = true,       
            // User Business Permissions
            bool checkUserBusinessesDisabled = true,
            bool checkUserBusinessesAddingEnabled = false,
            bool checkUserBusinessesEditingEnabled = false,
            bool checkUserBusinessesDeletingEnabled = false,
            // Business Permissions
            bool checkBusinessIsDisabled = true,
            bool checkBusinessCanBeEdited = false,
            bool checkBusinessCanBeDeleted = false,
            // Business Module Permissions
            List<ModulePermissionCheckData>? ModulePermissionsToCheck = null
        ) {
            var result = new FunctionReturnResult<ValidateUserAndBusinessResult?>();

            var userSessionValidationResult = await ValidateUserSessionWithPermissions(
                Request: Request,
                whiteLabelContext: null,
                // User Permissions
                checkUserIsAdmin: checkUserIsAdmin,
                checkUserDisabled: checkUserDisabled,
                // User Business Permissions
                checkUserBusinessesDisabled: checkUserBusinessesDisabled,
                checkUserBusinessesAddingEnabled: checkUserBusinessesAddingEnabled,
                checkUserBusinessesEditingEnabled: checkUserBusinessesEditingEnabled,
                checkUserBusinessesDeletingEnabled: checkUserBusinessesDeletingEnabled
            );
            if (!userSessionValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionAndBusinessWithPermissions:{userSessionValidationResult.Code}",
                    userSessionValidationResult.Message
                );
            }
            var userData = userSessionValidationResult.Data!.userData!;

            // Get and validate business
            var businessGetResult = await _businessManager.GetUserBusinessById(businessId, userData.Email);
            if (!businessGetResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionAndBusinessWithPermissions:{businessGetResult.Code}",
                    businessGetResult.Message
                );
            }
            var businessData = businessGetResult.Data!;

            // Check Business Top Level Permissions
            var businessPermissionResult = _userBusinessPermissionHelper.CheckBusinessPermission(businessData, checkBusinessIsDisabled, checkBusinessCanBeEdited, checkBusinessCanBeDeleted);
            if (!businessPermissionResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionAndBusinessWithPermissions:{businessPermissionResult.Code}",
                    businessPermissionResult.Message
                );
            }

            // Check Business Modules Permissions
            if (ModulePermissionsToCheck != null && ModulePermissionsToCheck.Count > 0)
            {
                foreach (var permissionCheckModule in ModulePermissionsToCheck)
                {
                    var modulePermissionResult = _userBusinessPermissionHelper.CheckBusinessModulePermission(businessData.Permission, permissionCheckModule);
                    if (!modulePermissionResult.Success)
                    {
                        return result.SetFailureResult(
                            $"ValidateUserSessionAndBusinessWithPermissions:{modulePermissionResult.Code}",
                            modulePermissionResult.Message
                        );
                    }
                }
            }

            return result.SetSuccessResult(new ValidateUserAndBusinessResult() { userData = userData, businessData = businessData });
        }


        /**
         * 
         * API VALIDATION 
         * 
        **/

        public virtual async Task<FunctionReturnResult<ValidateUserResult?>> ValidateUserAPIWithPermissions(
            HttpRequest Request,
            // User Permissions
            bool checkUserIsAdmin = false,
            bool checkUserDisabled = true,
            // User Businesses Permissions
            bool checkUserBusinessesDisabled = false,
            bool checkUserBusinessesAddingEnabled = false,
            bool checkUserBusinessesEditingEnabled = false,
            bool checkUserBusinessesDeletingEnabled = false,
            // User WhiteLabel Permissions
            bool checkUserWhiteLabelDisabled = false,
            bool checkUserWhiteLabelEditingDisabled = false
        )
        {
            var result = new FunctionReturnResult<ValidateUserResult?>();

            // Validate session
            var authorizationToken = Request.Headers["Authorization"].ToString();
            var apiKey = authorizationToken.Replace("Token ", "");

            var apiKeyValidaiton = await _userApiKeyManager.ValidateUserApiKeyAsync(apiKey);
            if (!apiKeyValidaiton.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserAPIWithPermissions:{apiKeyValidaiton.Code}",
                    apiKeyValidaiton.Message
                );
            }
            var userData = apiKeyValidaiton.Data!.User!;
            var userApiKeyData = apiKeyValidaiton.Data!.ApiKey!;

            // Validate User Permissions
            var validatePermission = ValidateUserPermissions(
                userData.Permission,
                // User Permissions
                checkUserIsAdmin,
                checkUserDisabled,
                // Business Permissions
                checkUserBusinessesDisabled,
                checkUserBusinessesAddingEnabled,
                checkUserBusinessesEditingEnabled,
                checkUserBusinessesDeletingEnabled,
                // WhiteLabel Permissions
                checkUserWhiteLabelDisabled,
                checkUserWhiteLabelEditingDisabled
            );
            if (!validatePermission.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserAPIWithPermissions:{validatePermission.Code}",
                    validatePermission.Message
                );
            }

            return result.SetSuccessResult(new ValidateUserResult()
            {
                userData = userData,
                userApiKeyData = userApiKeyData
            });
        }

        public virtual async Task<FunctionReturnResult<ValidateUserAndBusinessResult?>> ValidateUserAPIAndBusinessWithPermissions(
            HttpRequest Request,
            long businessId,
            bool checkAPIKeyBusinessRestriction = true,
            // User Permissions
            bool checkUserIsAdmin = false,
            bool checkUserDisabled = true,
            // User Business Permissions
            bool checkUserBusinessesDisabled = true,
            bool checkUserBusinessesAddingEnabled = false,
            bool checkUserBusinessesEditingEnabled = false,
            bool checkUserBusinessesDeletingEnabled = false,
            // Business Permissions
            bool checkBusinessIsDisabled = true,
            bool checkBusinessCanBeEdited = false,
            bool checkBusinessCanBeDeleted = false,
            // Business Module Permissions
            List<ModulePermissionCheckData>? ModulePermissionsToCheck = null
        )
        {
            var result = new FunctionReturnResult<ValidateUserAndBusinessResult?>();

            var userSessionValidationResult = await ValidateUserAPIWithPermissions(
                Request: Request,
                // User Permissions
                checkUserIsAdmin: checkUserIsAdmin,
                checkUserDisabled: checkUserDisabled,
                // User Business Permissions
                checkUserBusinessesDisabled: checkUserBusinessesDisabled,
                checkUserBusinessesAddingEnabled: checkUserBusinessesAddingEnabled,
                checkUserBusinessesEditingEnabled: checkUserBusinessesEditingEnabled,
                checkUserBusinessesDeletingEnabled: checkUserBusinessesDeletingEnabled
            );
            if (!userSessionValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserAPIAndBusinessWithPermissions:{userSessionValidationResult.Code}",
                    userSessionValidationResult.Message
                );
            }
            var userData = userSessionValidationResult.Data!.userData!;
            var apiKeyData = userSessionValidationResult.Data!.userApiKeyData!;

            // Check API Key Restricted
            if (checkAPIKeyBusinessRestriction && apiKeyData.RestrictedToBusinessIds.Count > 0 && !apiKeyData.RestrictedToBusinessIds.Contains(businessId))
            {
                return result.SetFailureResult(
                    "ValidateUserAPIAndBusinessWithPermissions:RESTRICTED_API_KEY",
                    "API Key is restricted to a different business."
                );
            }

            // Get and validate business
            var businessGetResult = await _businessManager.GetUserBusinessById(businessId, userData.Email);
            if (!businessGetResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserAPIAndBusinessWithPermissions:{businessGetResult.Code}",
                    businessGetResult.Message
                );
            }
            var businessData = businessGetResult.Data!;

            // Check Business Top Level Permissions
            var businessPermissionResult = _userBusinessPermissionHelper.CheckBusinessPermission(businessData, checkBusinessIsDisabled, checkBusinessCanBeEdited, checkBusinessCanBeDeleted);
            if (!businessPermissionResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserAPIAndBusinessWithPermissions:{businessPermissionResult.Code}",
                    businessPermissionResult.Message
                );
            }

            // Check Business Modules Permissions
            if (ModulePermissionsToCheck != null && ModulePermissionsToCheck.Count > 0)
            {
                foreach (var permissionCheckModule in ModulePermissionsToCheck)
                {
                    var modulePermissionResult = _userBusinessPermissionHelper.CheckBusinessModulePermission(businessData.Permission, permissionCheckModule);
                    if (!modulePermissionResult.Success)
                    {
                        return result.SetFailureResult(
                            $"ValidateUserAPIAndBusinessWithPermissions:{modulePermissionResult.Code}",
                            modulePermissionResult.Message
                        );
                    }
                }
            }

            return result.SetSuccessResult(
                new ValidateUserAndBusinessResult() {
                    userData = userData,
                    businessData = businessData
                }
            );
        }


        /**
         * 
         * Helpers
         * 
        **/

        public FunctionReturnResult ValidateUserPermissions(
            UserPermission userPermission,
            // User Permissions
            bool checkUserIsAdmin,
            bool checkUserDisabled,
            // Business Permissions
            bool checkUserBusinessesDisabled,
            bool checkUserBusinessesAddingEnabled,
            bool checkUserBusinessesEditingEnabled,
            bool checkUserBusinessesDeletingEnabled,
            // WhiteLabel Permissions
            bool checkUserWhiteLabelDisabled,
            bool checkUserWhiteLabelEditingDisabled
        ) {
            var result = new FunctionReturnResult();

            // User Permissions
            if (checkUserIsAdmin && !userPermission.IsAdmin)
            {
                return result.SetFailureResult(
                    "ValidateUserPermissions:USER_NOT_ADMIN",
                    "User is not admin"
                );
            }

            if (checkUserDisabled && userPermission.DisableUserAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserPermissions:USER_DISABLED",
                    $"User is disabled{(string.IsNullOrEmpty(userPermission.UserDisabledPublicReason) ? "" : ": " + userPermission.UserDisabledPublicReason)}"
                );
            }

            // Business Permissions
            if (checkUserBusinessesDisabled && userPermission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserPermissions:BUSINESS_DISABLED",
                    $"Business is disabled{(string.IsNullOrEmpty(userPermission.Business.DisableBusinessesPublicReason) ? "" : ": " + userPermission.Business.DisableBusinessesPublicReason)}"
                );
            }

            if (checkUserBusinessesAddingEnabled && userPermission.Business.AddBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserPermissions:BUSINESS_ADDING_DISABLED",
                    $"Business adding is disabled{(string.IsNullOrEmpty(userPermission.Business.AddBusinessDisablePublicReason) ? "" : ": " + userPermission.Business.AddBusinessDisablePublicReason)}"
                );
            }

            if (checkUserBusinessesEditingEnabled && userPermission.Business.EditBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserPermissions:BUSINESS_EDITING_DISABLED",
                    $"Business editing is disabled{(string.IsNullOrEmpty(userPermission.Business.EditBusinessDisablePublicReason) ? "" : ": " + userPermission.Business.EditBusinessDisablePublicReason)}"
                );
            }

            if (checkUserBusinessesDeletingEnabled && userPermission.Business.DeleteBusinessDisableAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserPermissions:BUSINESS_DELETING_DISABLED",
                    $"Business deleting is disabled{(string.IsNullOrEmpty(userPermission.Business.DeleteBusinessDisablePublicReason) ? "" : ": " + userPermission.Business.DeleteBusinessDisablePublicReason)}"
                );
            }

            // WhiteLabel Permissions
            if (checkUserWhiteLabelDisabled && userPermission.WhiteLabel.DisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserPermissions:WHITE_LABEL_DISABLED",
                    $"White label is disabled{(string.IsNullOrEmpty(userPermission.WhiteLabel.DisabledPublicReason) ? "" : ": " + userPermission.WhiteLabel.DisabledPublicReason)}"
                );
            }

            if (checkUserWhiteLabelEditingDisabled && userPermission.WhiteLabel.DisabledEditingAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserPermissions:WHITE_LABEL_EDITING_DISABLED",
                    $"White label editing is disabled{(string.IsNullOrEmpty(userPermission.WhiteLabel.DisabledEditingPublicReason) ? "" : ": " + userPermission.WhiteLabel.DisabledEditingPublicReason)}"
                );
            }

            return result.SetSuccessResult();
        }
    }
}
