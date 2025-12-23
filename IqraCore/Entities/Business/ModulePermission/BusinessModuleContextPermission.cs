using IqraCore.Entities.Business.ModulePermission;
using IqraCore.Entities.Business.ModulePermission.ENUM;

namespace IqraCore.Entities.Business
{
    public class BusinessModuleContextPermission
    {
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> ContextPermissions { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();

        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Branding { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Branches { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Services { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Products { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
    }
}
