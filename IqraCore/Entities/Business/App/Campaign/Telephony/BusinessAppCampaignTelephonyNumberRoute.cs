namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaignTelephonyNumberRoute
    {
        public Dictionary<string, string> RouteNumberList { get; set; } = new Dictionary<string, string>();
        public string DefaultNumberId { get; set; } = string.Empty;
    }
}