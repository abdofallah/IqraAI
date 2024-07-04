namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionConversations
    {
        public bool ConversationsTabEnabled { get; set; }

        public BusinessUserPermissionConversationsInboundCall InboundCallPermission { get; set; }
        public BusinessUserPermissionConversationsOutboundCall OutboundCallPermission { get; set; }
        public BusinessUserPermissionConversationsWebsocket WebsocketPermission { get; set; }
    }

    public class BusinessUserPermissionConversationsInboundCall
    {
        public bool InboundCallTabEnabled { get; set; }
        public bool DeleteInboundCall { get; set; }
        public bool ExportInboundCall { get; set; }
    }

    public class BusinessUserPermissionConversationsOutboundCall
    {
        public bool OutboundCallTabEnabled { get; set; }
        public bool DeleteOutboundCall { get; set; }
        public bool ExportOutboundCall { get; set; }
    }

    public class BusinessUserPermissionConversationsWebsocket
    {
        public bool WebsocketTabEnabled { get; set; }
        public bool DeleteWebsocket { get; set; }
        public bool ExportWebsocket { get; set; }
    }
}
