namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionContext
    {
        public bool ContextTabEnabled { get; set; } = true;
        
        public BusinessUserPermissionContextBranding Branding { get; set; } = new BusinessUserPermissionContextBranding();
        public BusinessUserPermissionContextBranches Branches { get; set; } = new BusinessUserPermissionContextBranches();
        public BusinessUserPermissionContextServices Services { get; set; } = new BusinessUserPermissionContextServices();
        public BusinessUserPermissionContextProducts Products { get; set; } = new BusinessUserPermissionContextProducts();
    }

    public class BusinessUserPermissionContextBranding
    {
        public bool TabEnabled { get; set; } = true;
        public bool Edit { get; set; } = true;
    }

    public class BusinessUserPermissionContextBranches
    {
        public bool TabEnabled { get; set; } = true;
        public bool Edit { get; set; } = true;
        public bool Add { get; set; } = true;
        public bool Delete { get; set; } = true;
    }

    public class BusinessUserPermissionContextServices
    {
        public bool TabEnabled { get; set; } = true;
        public bool Edit { get; set; } = true;
        public bool Add { get; set; } = true;
        public bool Delete { get; set; } = true;
    }

    public class BusinessUserPermissionContextProducts
    {
        public bool TabEnabled { get; set; } = true;
        public bool Edit { get; set; } = true;
        public bool Add { get; set; } = true;
        public bool Delete { get; set; } = true;
    }
}
