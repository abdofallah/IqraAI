using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Trello.Models
{
    public class TrelloBoard
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("closed")]
        public bool Closed { get; set; }
    }

    public class TrelloList
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("closed")]
        public bool Closed { get; set; }
    }

    public class TrelloCard
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("shortUrl")]
        public string? ShortUrl { get; set; }
    }
}
