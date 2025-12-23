using IqraCore.Entities.Business.ModulePermission;
using IqraCore.Entities.Business.ModulePermission.ENUM;

namespace IqraCore.Entities.Business
{
    public class BusinessModuleKnowledgeBasesPermission
    {
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> KnowledgeBasePermissions { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();

        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Documents { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
    }
}
