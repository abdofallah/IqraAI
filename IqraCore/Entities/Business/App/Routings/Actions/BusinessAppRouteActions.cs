namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteActions
    {
        public BusinessAppRouteActionTool CallInitiationFailureTool { get; set; } = new BusinessAppRouteActionTool();
        public BusinessAppRouteActionTool RingingTool { get; set; } = new BusinessAppRouteActionTool();
        public BusinessAppRouteActionTool CallPickedTool { get; set; } = new BusinessAppRouteActionTool();
        public BusinessAppRouteActionTool CallMissedTool { get; set; } = new BusinessAppRouteActionTool();
        public BusinessAppRouteActionTool CallEndedTool { get; set; } = new BusinessAppRouteActionTool();
    }
}
