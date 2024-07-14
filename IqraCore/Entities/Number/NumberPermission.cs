namespace IqraCore.Entities.Number
{
    public class NumberPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;

        public DateTime? DisabledInboundCallingAt { get; set; } = null;
        public string? DisabledInboundCallingReason { get; set; } = null;

        public DateTime? DisabledOutboundCallingAt { get; set; } = null;
        public string? DisabledOutboundCallingReason { get; set; } = null;
    }
}
