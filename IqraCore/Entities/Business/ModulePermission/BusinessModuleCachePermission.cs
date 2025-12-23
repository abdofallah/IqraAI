using IqraCore.Entities.Business.ModulePermission;
using IqraCore.Entities.Business.ModulePermission.ENUM;

namespace IqraCore.Entities.Business
{
    public class BusinessModuleCachePermission
    {
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> CachePermissions { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();

        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> MessageGroup { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> AudioGroup { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> EmbeddingGroup { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
    }
}
