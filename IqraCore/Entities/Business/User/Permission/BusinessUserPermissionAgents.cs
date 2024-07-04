namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionAgents
    {
        public bool AgentsTabEnabled { get; set; }
        public bool AddNewAgent { get; set; }
        public bool EditAgent { get; set; }
        public bool DeleteAgent { get; set; }
        public int MaxAllowedAgents { get; set; }
    }
}
