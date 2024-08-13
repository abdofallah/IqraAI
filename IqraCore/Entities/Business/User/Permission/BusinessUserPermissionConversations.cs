namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionConversations
    {
        public bool TabEnabled { get; set; }

        public BusinessUserPermissionConversationsInboundCall Inbound { get; set; } = new BusinessUserPermissionConversationsInboundCall();
        public BusinessUserPermissionConversationsOutboundCall Outbound { get; set; } = new BusinessUserPermissionConversationsOutboundCall();
        public BusinessUserPermissionConversationsWebsocket Websocket { get; set; } = new BusinessUserPermissionConversationsWebsocket();
    }

    public class BusinessUserPermissionConversationsInboundCall
    {
        public bool TabEnabled { get; set; } = true;
        public bool Delete { get; set; } = true;
        public bool Export { get; set; } = true;
    }

    public class BusinessUserPermissionConversationsOutboundCall
    {
        public bool TabEnabled { get; set; } = true;
        public bool Delete { get; set; } = true;
        public bool Export { get; set; } = true;
    }

    public class BusinessUserPermissionConversationsWebsocket
    {
        public bool TabEnabled { get; set; } = true;
        public bool Delete { get; set; } = true;
        public bool Export { get; set; } = true;
    }
}
