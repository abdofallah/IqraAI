using System.Text.Json.Serialization;

namespace IqraCore.Entities.LLM.Providers.GroqCloud
{
    public record GroqCloudRequest(
            [property: JsonPropertyName("model")] string Model,
            [property: JsonPropertyName("messages")] List<GroqCloudMessage> Messages,
            [property: JsonPropertyName("temperature")] float Temperature,
            [property: JsonPropertyName("max_tokens")] int MaxTokens, // API docs now prefer max_completion_tokens, but examples use max_tokens. Let's use max_tokens for broader compatibility for now, but keep an eye on this. **Update**: Doc explicitly deprecates max_tokens. Use max_completion_tokens.
                                                                      // [property: JsonPropertyName("max_tokens")] int MaxTokens,
            [property: JsonPropertyName("max_completion_tokens")] int MaxCompletionTokens,
            [property: JsonPropertyName("top_p")] float TopP,
            [property: JsonPropertyName("stream")] bool Stream,
            [property: JsonPropertyName("stop")] string? Stop = null // Optional: Add if needed later
        );
}
