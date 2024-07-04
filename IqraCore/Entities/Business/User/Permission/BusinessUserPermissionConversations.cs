namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionConversations
    {
        public bool ConversationsTabEnabled { get; set; }

        public BusinessUserPermissionConversationsInboundCall InboundCallPermission { get; set; } = new BusinessUserPermissionConversationsInboundCall();
        public BusinessUserPermissionConversationsOutboundCall OutboundCallPermission { get; set; } = new BusinessUserPermissionConversationsOutboundCall();
        public BusinessUserPermissionConversationsWebsocket WebsocketPermission { get; set; } = new BusinessUserPermissionConversationsWebsocket();
    }

    public class BusinessUserPermissionConversationsInboundCall
    {
        public bool InboundCallTabEnabled { get; set; } = true;
        public bool DeleteInboundCall { get; set; } = true;
        public bool ExportInboundCall { get; set; } = true;
    }

    public class BusinessUserPermissionConversationsOutboundCall
    {
        public bool OutboundCallTabEnabled { get; set; } = true;
        public bool DeleteOutboundCall { get; set; } = true;
        public bool ExportOutboundCall { get; set; } = true;
    }

    public class BusinessUserPermissionConversationsWebsocket
    {
        public bool WebsocketTabEnabled { get; set; } = true;
        public bool DeleteWebsocket { get; set; } = true;
        public bool ExportWebsocket { get; set; } = true;
    }
}
