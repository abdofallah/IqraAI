using IqraCore.Entities.Business.ModulePermission;
using IqraCore.Entities.Business.ModulePermission.ENUM;

namespace IqraCore.Entities.Business
{
    public class BusinessModulePermission
    {
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> BusinessPermissions { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();

        // UI Facing Permissions
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Agents { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Scripts { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public BusinessModuleCachePermission Cache { get; set; } = new BusinessModuleCachePermission();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Integrations { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Tools { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public BusinessModuleContextPermission Context { get; set; } = new BusinessModuleContextPermission();
        public BusinessModuleConversationsPermission Conversations { get; set; } = new BusinessModuleConversationsPermission();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> Numbers { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public BusinessModuleKnowledgeBasesPermission KnowledgeBases { get; set; } = new BusinessModuleKnowledgeBasesPermission();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> InboundRoutings { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> TelephonyCampaigns { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> WebCampaigns { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> PostAnalysis { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();

        // Non-Client Facing Permissions
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> RecieveCall { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> MakeCall { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
        public Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem> WebSessionCall { get; set; } = new Dictionary<BusinessModulePermissionType, BusinessModulePermissionItem>();
    }
}
