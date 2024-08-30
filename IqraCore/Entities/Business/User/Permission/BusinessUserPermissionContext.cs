namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionContext
    {
        public bool TabEnabled { get; set; } = false;
        
        public BusinessUserPermissionContextBranding Branding { get; set; } = new BusinessUserPermissionContextBranding();
        public BusinessUserPermissionContextBranches Branches { get; set; } = new BusinessUserPermissionContextBranches();
        public BusinessUserPermissionContextServices Services { get; set; } = new BusinessUserPermissionContextServices();
        public BusinessUserPermissionContextProducts Products { get; set; } = new BusinessUserPermissionContextProducts();
    }

    public class BusinessUserPermissionContextBranding
    {
        public bool TabEnabled { get; set; } = false;
        public bool Edit { get; set; } = false;
    }

    public class BusinessUserPermissionContextBranches
    {
        public bool TabEnabled { get; set; } = false;
        public bool Edit { get; set; } = false;
        public bool Add { get; set; } = false;
        public bool Delete { get; set; } = false;
    }

    public class BusinessUserPermissionContextServices
    {
        public bool TabEnabled { get; set; } = false;
        public bool Edit { get; set; } = false;
        public bool Add { get; set; } = false;
        public bool Delete { get; set; } = false;
    }

    public class BusinessUserPermissionContextProducts
    {
        public bool TabEnabled { get; set; } = false;
        public bool Edit { get; set; } = false;
        public bool Add { get; set; } = false;
        public bool Delete { get; set; } = false;
    }
}
