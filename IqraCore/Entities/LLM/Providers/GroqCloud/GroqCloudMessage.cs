using System.Text.Json.Serialization;

namespace IqraCore.Entities.LLM.Providers.GroqCloud
{
    public record GroqCloudMessage(
            [property: JsonPropertyName("role")] string Role,
            [property: JsonPropertyName("content")] string Content
        );
}
