namespace IqraCore.Entities.Business
{
    public class BusinessAppContext
    {
        public BusinessAppContextBranding Branding { get; set; }
        public List<BusinessAppContextBranch> Branches { get; set; }
        public List<BusinessAppContextService> Services { get; set; }
        public List<BusinessAppContextProduct> Products { get; set; }
    }
}
