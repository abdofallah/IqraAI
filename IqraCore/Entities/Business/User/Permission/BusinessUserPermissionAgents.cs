namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionAgents
    {
        public bool AgentsTabEnabled { get; set; } = true;
        public bool AddNewAgent { get; set; } = true;
        public bool EditAgent { get; set; } = true;
        public bool DeleteAgent { get; set; } = true;
        public int MaxAllowedAgents { get; set; } = -1;
    }
}
