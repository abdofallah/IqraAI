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

        // UI Facing Permissions
        public BusinessRoutesPermission Routings { get; set; } = new BusinessRoutesPermission();
        public BusinessAgentsPermission Agents { get; set; } = new BusinessAgentsPermission();
        public BusinessScriptsPermission Scripts { get; set; } = new BusinessScriptsPermission();
        public BusinessCachePermission Cache { get; set; } = new BusinessCachePermission();
        public BusnessIntegrationsPermission Integrations { get; set; } = new BusnessIntegrationsPermission();
        public BusinessToolsPermission Tools { get; set; } = new BusinessToolsPermission();
        public BusinessContextPermission Context { get; set; } = new BusinessContextPermission();
        public BusinessConversationsPermission Conversations { get; set; } = new BusinessConversationsPermission();
        public BusinessNumbersPermission Numbers { get; set; } = new BusinessNumbersPermission();
        public BusinessKnowledgeBasesPermission KnowledgeBases { get; set; } = new BusinessKnowledgeBasesPermission();
        public BusinessCampaignsPermission Campaigns { get; set; } = new BusinessCampaignsPermission();
        public BusinessPostAnalysisPermission PostAnalysis { get; set; } = new BusinessPostAnalysisPermission();

        // Non-Client Facing Permissions
        public BusinessRecieveCallPermission RecieveCall { get; set; } = new BusinessRecieveCallPermission();
        public BusinessMakeCallPermission MakeCall { get; set; } = new BusinessMakeCallPermission();
        public BusinessWebSessionPermission WebSession { get; set; } = new BusinessWebSessionPermission();
    }
}
