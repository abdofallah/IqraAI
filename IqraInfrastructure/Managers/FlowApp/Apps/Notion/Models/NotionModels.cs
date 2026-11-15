using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Notion.Models
{
    // --- Search (for the databases fetcher) ---

    public class SearchRequest
    {
        [JsonPropertyName("filter")]
        public SearchFilter Filter { get; set; } = new();

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; } = 100;

        [JsonPropertyName("start_cursor")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? StartCursor { get; set; }
    }

    public class SearchFilter
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = "database";

        [JsonPropertyName("property")]
        public string Property { get; set; } = "object";
    }

    public class SearchResponse
    {
        [JsonPropertyName("results")]
        public List<NotionObject> Results { get; set; } = new();

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        [JsonPropertyName("next_cursor")]
        public string? NextCursor { get; set; }
    }

    public class NotionObject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public List<RichText>? Title { get; set; }
    }

    public class RichText
    {
        [JsonPropertyName("plain_text")]
        public string PlainText { get; set; } = string.Empty;
    }

    // --- Create page response ---

    public class CreatePageResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
