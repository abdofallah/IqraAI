namespace IqraCore.Models.FlowApp
{
    public class FlowAppTestActionRequestModel
    {
        public string? IntegrationId { get; set; }
        public Dictionary<string, object?> Inputs { get; set; } = new();
    }
}
