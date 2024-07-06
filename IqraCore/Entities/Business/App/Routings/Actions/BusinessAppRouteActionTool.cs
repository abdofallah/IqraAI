namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteActionTool
    {
        public long? SelectedToolId { get; set; } = null;
        public Dictionary<long, string>? Arguements { get; set; } = new Dictionary<long, string>();
    }
}
