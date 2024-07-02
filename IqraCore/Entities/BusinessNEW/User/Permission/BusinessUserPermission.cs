namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessUserPermission
    {     
        public BusinessUserPermissionRouting BusinessUserPermissionRouting { get; set; }
        public BusinessUserPermissionTools BusinessUserPermissionTools { get; set; }
        public BusinessUserPermissionAgents BusinessUserPermissionAgents { get; set; }
        public BusinessUserPermissionContext BusinessUserPermissionContext { get; set; }
        public BusinessUserPermissionMakeCalls BusinessUserPermissionMakeCalls { get; set; }
        public BusinessUserPermissionConversations BusinessUserPermissionConversations { get; set; }
        public BusinessUserPermissionSettings BusinessUserPermissionSettings { get; set; }
    }
}