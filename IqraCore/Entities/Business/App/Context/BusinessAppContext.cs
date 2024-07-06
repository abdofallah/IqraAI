namespace IqraCore.Entities.Business
{
    public class BusinessAppContext
    {
        public BusinessAppContextBranding Branding { get; set; } = new BusinessAppContextBranding();
        public List<BusinessAppContextBranch> Branches { get; set; } = new List<BusinessAppContextBranch>();
        public List<BusinessAppContextService> Services { get; set; } = new List<BusinessAppContextService>();
        public List<BusinessAppContextProduct> Products { get; set; } = new List<BusinessAppContextProduct>();
    }
}
