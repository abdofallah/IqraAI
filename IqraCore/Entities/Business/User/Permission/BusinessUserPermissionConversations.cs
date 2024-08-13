namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionConversations
    {
        public bool TabEnabled { get; set; } = false;

        public BusinessUserPermissionConversationsInboundCall Inbound { get; set; } = new BusinessUserPermissionConversationsInboundCall();
        public BusinessUserPermissionConversationsOutboundCall Outbound { get; set; } = new BusinessUserPermissionConversationsOutboundCall();
        public BusinessUserPermissionConversationsWebsocket Websocket { get; set; } = new BusinessUserPermissionConversationsWebsocket();
    }

    public class BusinessUserPermissionConversationsInboundCall
    {
        public bool TabEnabled { get; set; } = false;
        public bool Delete { get; set; } = false;
        public bool Export { get; set; } = false;
    }

    public class BusinessUserPermissionConversationsOutboundCall
    {
        public bool TabEnabled { get; set; } = false;
        public bool Delete { get; set; } = false;
        public bool Export { get; set; } = false;
    }

    public class BusinessUserPermissionConversationsWebsocket
    {
        public bool TabEnabled { get; set; } = false;
        public bool Delete { get; set; } = false;
        public bool Export { get; set; } = false;
    }
}
