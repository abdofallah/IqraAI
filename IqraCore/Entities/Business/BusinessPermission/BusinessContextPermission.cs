namespace IqraCore.Entities.Business
{
    public class BusinessContextPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public BusinessContextBrandingPermission Branding { get; set; } = new BusinessContextBrandingPermission();
        public BusinessContextBranchesPermission Branches { get; set; } = new BusinessContextBranchesPermission();
        public BusinessContextServicesPermission Services { get; set; } = new BusinessContextServicesPermission();
        public BusinessContextProductsPermission Products { get; set; } = new BusinessContextProductsPermission();
    }

    public class BusinessContextBrandingPermission
    {
        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;
    }

    public class BusinessContextBranchesPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledAddingAt { get; set; } = null;
        public string? DisabledAddingReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }

    public class BusinessContextServicesPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledAddingAt { get; set; } = null;
        public string? DisabledAddingReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }

    public class BusinessContextProductsPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledAddingAt { get; set; } = null;
        public string? DisabledAddingReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }
}
