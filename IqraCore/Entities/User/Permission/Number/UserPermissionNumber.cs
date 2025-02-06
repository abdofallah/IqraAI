namespace IqraCore.Entities.User
{
    public class UserPermissionNumber
    {
        public DateTime? DisableNumbersAt { get; set; } = null;
        public string? DisableNumbersReason { get; set; } = null;

        public DateTime? AddNumberDisabledAt { get; set; } = null;
        public string? AddNumberDisableReason { get; set; } = null;

        public DateTime? EditNumberDisabledAt { get; set; } = null;
        public string? EditNumberDisableReason { get; set; } = null;

        public DateTime? DeleteNumberDisableAt { get; set; } = null;
        public string? DeleteNumberDisableReason { get; set; } = null;
    }
}
