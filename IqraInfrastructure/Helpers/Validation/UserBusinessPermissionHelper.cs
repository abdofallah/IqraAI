using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using System.Reflection;
using IqraCore.Entities.Validation;

namespace IqraInfrastructure.Helpers.Validation
{
    public class UserBusinessPermissionHelper : IUserBusinessPermissionHelper
    {
        public FunctionReturnResult CheckBusinessPermission(
            BusinessData businessData,

            bool checkBusinessIsDisabled,
            bool checkBusinessCanBeEdited,
            bool checkBusinessCanBeDeleted
        ) {
            var result = new FunctionReturnResult();

            // Check Business Full Disabled
            if (checkBusinessIsDisabled && businessData.Permission.BusinessPermissions.TryGetValue(BusinessModulePermissionType.Full, out var businessDisabledFullData))
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_DISABLED",
                    $"Business is disabled{(string.IsNullOrWhiteSpace(businessDisabledFullData.PublicReason) ? "" : ": " + businessDisabledFullData.PublicReason)}"
                );
            }

            // Check Business Editing Disabled
            if (checkBusinessCanBeEdited && businessData.Permission.BusinessPermissions.TryGetValue(BusinessModulePermissionType.Editing, out var businessDisabledEditingData))
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_EDITING_DISABLED",
                    $"Business editing is disabled{(string.IsNullOrWhiteSpace(businessDisabledEditingData.PublicReason) ? "" : ": " + businessDisabledEditingData.PublicReason)}"
                );
            }

            // Check Business Deleting Disabled
            if (checkBusinessCanBeDeleted && businessData.Permission.BusinessPermissions.TryGetValue(BusinessModulePermissionType.Deleting, out var businessDisabledDeletingData))
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_DELETING_DISABLED",
                    $"Business deleting is disabled{(string.IsNullOrWhiteSpace(businessDisabledDeletingData.PublicReason) ? "" : ": " + businessDisabledDeletingData.PublicReason)}"
                );
            }

            return result.SetSuccessResult();
        }

        public FunctionReturnResult CheckBusinessModulePermission(BusinessModulePermission rootPermission, ModulePermissionCheckData check)
        {
            var result = new FunctionReturnResult();
            object currentObj = rootPermission;
            string[] pathParts = check.ModulePath.Split('.');

            // Traverse the object tree (e.g. Permission -> Conversations -> Inbound)
            foreach (var part in pathParts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    return result.SetFailureResult("CheckModulePermission:EMPTY_PATH_PART", "Empty permission path part");
                }

                var prop = currentObj.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null)
                {
                    return result.SetFailureResult("CheckModulePermission:INVALID_PATH", "Invalid permission path");
                }

                var value = prop.GetValue(currentObj);
                if (value == null)
                {
                    return result.SetFailureResult("CheckModulePermission:PATH_VALUE_NULL", "Path value is null");
                }

                currentObj = value;
            }

            if (currentObj is not Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> modulePermissionData)
            {
                return result.SetFailureResult("CheckModulePermission:INVALID_PATH_TYPE", "Invalid permission path type");
            }

            if (
                !modulePermissionData.TryGetValue(check.Type, out var modulePermissionItem) ||
                modulePermissionItem.DisabledAt == null
            ) {
                return result.SetSuccessResult();
            }

            string reason = modulePermissionItem.PublicReason != null ? modulePermissionItem.PublicReason : "";
            return result.SetFailureResult(
                $"CheckModulePermission:{check.ModulePath.ToUpper()}_{check.Type.ToString().ToUpper()}",
                $"{check.ModulePath} action '{check.Type}' is disabled{(string.IsNullOrEmpty(reason) ? "" : ": " + reason)}"
            );
        }
    }
}
