namespace IqraCore.Entities.Business
{
    public class BusinessUserPermission
    {     
        public BusinessUserPermissionRouting BusinessUserPermissionRouting { get; set; } = new BusinessUserPermissionRouting();
        public BusinessUserPermissionTools BusinessUserPermissionTools { get; set; } = new BusinessUserPermissionTools();
        public BusinessUserPermissionAgents BusinessUserPermissionAgents { get; set; } = new BusinessUserPermissionAgents();
        public BusinessUserPermissionContext BusinessUserPermissionContext { get; set; } = new BusinessUserPermissionContext();
        public BusinessUserPermissionMakeCalls BusinessUserPermissionMakeCalls { get; set; } = new BusinessUserPermissionMakeCalls();
        public BusinessUserPermissionConversations BusinessUserPermissionConversations { get; set; } = new BusinessUserPermissionConversations();
        public BusinessUserPermissionSettings BusinessUserPermissionSettings { get; set; } = new BusinessUserPermissionSettings();
    }
}