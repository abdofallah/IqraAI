using IqraCore.Cloud.Entities.WhiteLabel;
using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Entities.Validation;
using IqraCore.Entities.WhiteLabel;
using Microsoft.AspNetCore.Http;

namespace IqraCore.Interfaces.Validation
{
    public class ValidateUserAndBusinessResult
    {
        public UserData? userData { get; set; }
        public BusinessData? businessData { get; set; }
        public object? userWhiteLabelCustomerData { get; set; }
    }

    public class ValidateUserResult
    {
        public UserData? userData { get; set; }
        public object? userWhiteLabelCustomerData { get; set; }
        public UserApiKey? userApiKeyData { get; set; }
    }

    public interface ISessionValidationAndPermissionHelper
    {
        Task<UserData?> GetUserDataAsync(string email);

        Task<FunctionReturnResult<string?>> ValidateUserSessionAsync(
            HttpRequest Request
        );
        Task<FunctionReturnResult<ValidateUserResult>> ValidateUserSessionWithPermissions(
            HttpRequest Request,
            WhiteLabelContext? whiteLabelContext = null,
            // User Permissions
            bool checkUserIsAdmin = false,
            bool checkUserDisabled = true,
            // User Business Permissions
            bool checkUserBusinessesDisabled = false,
            bool checkUserBusinessesAddingEnabled = false,
            bool checkUserBusinessesEditingEnabled = false,
            bool checkUserBusinessesDeletingEnabled = false,
            // WhiteLabel Permissions
            bool checkUserWhiteLabelDisabled = false,
            bool checkUserWhiteLabelEditingDisabled = false
        );
        Task<FunctionReturnResult<WhiteLabelSessionData?>> ValidateWhiteLabelCustomerSessionAsync(
            HttpRequest Request,
            WhiteLabelContext whiteLabelContext
        );

        Task<FunctionReturnResult<ValidateUserAndBusinessResult?>> ValidateUserSessionAndBusinessWithPermissions(
            HttpRequest Request,
            long businessId,
            WhiteLabelContext? whiteLabelContext = null,
            // User Permissions
            bool checkUserIsAdmin = false,
            bool checkUserDisabled = true,
            // User Businesses Permissions
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
        );

        Task<FunctionReturnResult<ValidateUserResult?>> ValidateUserAPIWithPermissions(
            HttpRequest Request,
            bool checkUserApiAccessManagementRestriction = false,
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
        );
        Task<FunctionReturnResult<ValidateUserAndBusinessResult?>> ValidateUserAPIAndBusinessWithPermissions(
            HttpRequest Request,
            long businessId,
            bool checkUserApiAccessManagementRestriction = false,      
            bool checkAPIKeyBusinessRestriction = true,
            // User Permissions
            bool checkUserIsAdmin = false,
            bool checkUserDisabled = true,
            // User Businesses Permissions
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
        );
    }
}
