namespace IqraCore.Entities.Business
{
    public class BusinessPermission
    {
        public DateTime? DisableFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisableEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;

        public DateTime? DisableDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }
}
