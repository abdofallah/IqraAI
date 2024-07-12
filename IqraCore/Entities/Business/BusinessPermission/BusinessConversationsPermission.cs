namespace IqraCore.Entities.Business
{
    public class BusinessConversationsPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public BusinessInboundConversationsPermission Inbound { get; set; } = new BusinessInboundConversationsPermission();
        public BusinessOutboundConversationsPermission Outbound { get; set; } = new BusinessOutboundConversationsPermission();
        public BusinessWebsocketConversationsPermission Websocket { get; set; } = new BusinessWebsocketConversationsPermission();
    }

    public class BusinessInboundConversationsPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledExportingAt { get; set; } = null;
        public string? DisabledExportingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }

    public class BusinessOutboundConversationsPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledExportingAt { get; set; } = null;
        public string? DisabledExportingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }

    public class BusinessWebsocketConversationsPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledExportingAt { get; set; } = null;
        public string? DisabledExportingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }
}
