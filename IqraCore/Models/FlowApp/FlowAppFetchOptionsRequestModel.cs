using System.Text.Json;

namespace IqraCore.Models.FlowApp
{
    public class FlowAppFetchOptionsRequestModel
    {
        public string? IntegrationId { get; set; }
        public JsonElement Context { get; set; }
    }
}
