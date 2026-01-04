using IqraCore.Entities.Business.ModulePermission;
using IqraCore.Entities.Business.ModulePermission.ENUM;

namespace IqraCore.Entities.Business
{
    public class BusinessModuleFlowAppsPermission
    {
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> FlowAppsPermissions { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();

        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Fetchers { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
    }
}