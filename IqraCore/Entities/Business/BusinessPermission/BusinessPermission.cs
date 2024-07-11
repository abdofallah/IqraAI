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
        public BusinessToolsPermission Tools { get; set; } = new BusinessToolsPermission();
    }
}
