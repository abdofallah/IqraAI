namespace IqraCore.Entities.User
{
    public class UserPermissionBusiness 
    {
        public DateTime? DisableBusinessesAt { get; set; } = null;
        public string? DisableBusinessesReason { get; set; } = null;

        public DateTime? AddBusinessDisabledAt { get; set; } = null;
        public string? AddBusinessDisableReason { get; set; } = null;

        public DateTime? EditBusinessDisabledAt { get; set; } = null;
        public string? EditBusinessDisableReason { get; set; } = null;

        public DateTime? DeleteBusinessDisableAt { get; set; } = null;
        public string? DeleteBusinessDisableReason { get; set; } = null;
    }
}
