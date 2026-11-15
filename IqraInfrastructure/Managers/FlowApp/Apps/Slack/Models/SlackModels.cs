using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Slack.Models
{
    // --- Shared envelope ---

    public class SlackApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("warning")]
        public string? Warning { get; set; }
    }

    // --- chat.postMessage ---

    public class PostMessageRequest
    {
        [JsonPropertyName("channel")]
        public string Channel { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("thread_ts")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ThreadTs { get; set; }

        [JsonPropertyName("username")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Username { get; set; }

        [JsonPropertyName("icon_emoji")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IconEmoji { get; set; }
    }

    public class PostMessageResponse : SlackApiResponse
    {
        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("ts")]
        public string? Ts { get; set; }
    }

    // --- conversations.open (for DMs) ---

    public class OpenConversationRequest
    {
        [JsonPropertyName("users")]
        public string Users { get; set; } = string.Empty;
    }

    public class OpenConversationResponse : SlackApiResponse
    {
        [JsonPropertyName("channel")]
        public OpenedChannel? Channel { get; set; }
    }

    public class OpenedChannel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    // --- conversations.list ---

    public class ListChannelsResponse : SlackApiResponse
    {
        [JsonPropertyName("channels")]
        public List<SlackChannel> Channels { get; set; } = new();

        [JsonPropertyName("response_metadata")]
        public ResponseMetadata? ResponseMetadata { get; set; }
    }

    public class SlackChannel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("is_archived")]
        public bool IsArchived { get; set; }
    }

    public class ResponseMetadata
    {
        [JsonPropertyName("next_cursor")]
        public string? NextCursor { get; set; }
    }
}
