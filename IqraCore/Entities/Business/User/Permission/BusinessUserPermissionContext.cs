namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionContext
    {
        public bool ContextTabEnabled { get; set; } = true;
        
        public BusinessUserPermissionContextBranding BrandingPermission { get; set; } = new BusinessUserPermissionContextBranding();
        public BusinessUserPermissionContextBranches BranchesPermission { get; set; } = new BusinessUserPermissionContextBranches();
        public BusinessUserPermissionContextServices ServicesPermission { get; set; } = new BusinessUserPermissionContextServices();
        public BusinessUserPermissionContextProducts ProductsPermission { get; set; } = new BusinessUserPermissionContextProducts();
    }

    public class BusinessUserPermissionContextBranding
    {
        public bool BrandingTabEnabled { get; set; } = true;
        public bool EditBranding { get; set; } = true;
    }

    public class BusinessUserPermissionContextBranches
    {
        public bool BranchesTabEnabled { get; set; } = true;
        public bool EditBranches { get; set; } = true;
        public bool AddBranches { get; set; } = true;
        public bool DeleteBranches { get; set; } = true;
        public int MaxAllowedBranches { get; set; } = -1;
    }

    public class BusinessUserPermissionContextServices
    {
        public bool ServicesTabEnabled { get; set; } = true;
        public bool EditServices { get; set; } = true;
        public bool AddServices { get; set; } = true;
        public bool DeleteServices { get; set; } = true;
        public int MaxAllowedServices { get; set; } = -1;
    }

    public class BusinessUserPermissionContextProducts
    {
        public bool ProductsTabEnabled { get; set; } = true;
        public bool EditProducts { get; set; } = true;
        public bool AddProducts { get; set; } = true;
        public bool DeleteProducts { get; set; } = true;
        public int MaxAllowedProducts { get; set; } = -1;
    }
}
