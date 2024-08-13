namespace IqraCore.Entities.Business
{
    public class BusinessUserPermission
    {     
        public BusinessUserPermissionRouting Routing { get; set; } = new BusinessUserPermissionRouting();
        public BusinessUserPermissionTools Tools { get; set; } = new BusinessUserPermissionTools();
        public BusinessUserPermissionAgents Agents { get; set; } = new BusinessUserPermissionAgents();
        public BusinessUserPermissionContext Context { get; set; } = new BusinessUserPermissionContext();
        public BusinessUserPermissionMakeCalls MakeCalls { get; set; } = new BusinessUserPermissionMakeCalls();
        public BusinessUserPermissionConversations Conversations { get; set; } = new BusinessUserPermissionConversations();
        public BusinessUserPermissionSettings Settings { get; set; } = new BusinessUserPermissionSettings();
    }
}