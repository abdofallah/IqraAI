namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessUserPermissionContext
    {
        public bool ContextTabEnabled { get; set; }
        
        public BusinessUserPermissionContextBranding BrandingPermission { get; set; }
        public BusinessUserPermissionContextBranches BranchesPermission { get; set; }
        public BusinessUserPermissionContextServices ServicesPermission { get; set; }
        public BusinessUserPermissionContextProducts ProductsPermission { get; set; }
    }

    public class BusinessUserPermissionContextBranding
    {
        public bool BrandingTabEnabled { get; set; }
        public bool EditBranding { get; set; }
    }

    public class BusinessUserPermissionContextBranches
    {
        public bool BranchesTabEnabled { get; set; }
        public bool EditBranches { get; set; }
        public bool AddBranches { get; set; }
        public bool DeleteBranches { get; set; }
        public int MaxAllowedBranches { get; set; }
    }

    public class BusinessUserPermissionContextServices
    {
        public bool ServicesTabEnabled { get; set; }
        public bool EditServices { get; set; }
        public bool AddServices { get; set; }
        public bool DeleteServices { get; set; }
        public int MaxAllowedServices { get; set; }
    }

    public class BusinessUserPermissionContextProducts
    {
        public bool ProductsTabEnabled { get; set; }
        public bool EditProducts { get; set; }
        public bool AddProducts { get; set; }
        public bool DeleteProducts { get; set; }
        public int MaxAllowedProducts { get; set; }
    }
}
