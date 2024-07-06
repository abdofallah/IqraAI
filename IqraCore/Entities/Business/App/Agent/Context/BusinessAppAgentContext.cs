namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentContext
    {
        public bool UseBranding { get; set; } = true;
        public bool UseBranches { get; set; } = true;
        public bool UseServices { get; set; } = true;
        public bool UseProducts { get; set; } = true;
    }
}
