using IqraCore.Entities.Business.ModulePermission;
using IqraCore.Entities.Business.ModulePermission.ENUM;

namespace IqraCore.Entities.Business
{
    public class BusinessModuleConversationsPermission
    {
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> ConversationPermissions { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();

        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Inbound { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Outbound { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> WebSession { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
    }
}
