using System.Text.Json.Serialization;

namespace IqraCore.Entities.LLM.Providers.GroqCloud
{
    public class GroqCloudUsageInfo
    {
        [JsonPropertyName("usage")]
        public GroqCloudStreamUsage Usage { get; set; }
    }
}
