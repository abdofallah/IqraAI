using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.ResembleAI
{
    public class ResembleBillingResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("items")]
        public ResembleBillingItems? Items { get; set; }
    }

    public class ResembleBillingItems
    {
        [JsonPropertyName("synth")]
        public string? SynthUsageSeconds { get; set; } // It comes as string "8450"
    }
}
