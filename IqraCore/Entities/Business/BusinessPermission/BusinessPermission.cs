namespace IqraCore.Entities.Business
{
    public class BusinessPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;

        public BusinessRoutingPermission Routing { get; set; } = new BusinessRoutingPermission();
        public BusinessAgentsPermission Agents { get; set; } = new BusinessAgentsPermission();
        public BusinessCachePermission Cache { get; set; } = new BusinessCachePermission();
        public BusnessIntegrationsPermission Integrations { get; set; } = new BusnessIntegrationsPermission();
        public BusinessToolsPermission Tools { get; set; } = new BusinessToolsPermission();
        public BusinessContextPermission Context { get; set; } = new BusinessContextPermission();
        public BusinessMakeCallPermission MakeCall { get; set; } = new BusinessMakeCallPermission();
        public BusinessConversationsPermission Conversations { get; set; } = new BusinessConversationsPermission();
        public BusinessNumbersPermission Numbers { get; set; } = new BusinessNumbersPermission();
    }
}
