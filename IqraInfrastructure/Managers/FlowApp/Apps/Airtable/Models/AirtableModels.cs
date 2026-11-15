using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Models
{
    // --- Create / list records ---

    public class CreateRecordRequest
    {
        [JsonPropertyName("fields")]
        public Dictionary<string, object?> Fields { get; set; } = new();

        [JsonPropertyName("typecast")]
        public bool Typecast { get; set; } = true;
    }

    public class AirtableRecord
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("createdTime")]
        public string? CreatedTime { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, object?> Fields { get; set; } = new();
    }

    public class ListRecordsResponse
    {
        [JsonPropertyName("records")]
        public List<AirtableRecord> Records { get; set; } = new();

        [JsonPropertyName("offset")]
        public string? Offset { get; set; }
    }

    // --- Metadata (bases & tables) for fetchers ---

    public class ListBasesResponse
    {
        [JsonPropertyName("bases")]
        public List<BaseInfo> Bases { get; set; } = new();

        [JsonPropertyName("offset")]
        public string? Offset { get; set; }
    }

    public class BaseInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("permissionLevel")]
        public string? PermissionLevel { get; set; }
    }

    public class ListTablesResponse
    {
        [JsonPropertyName("tables")]
        public List<TableInfo> Tables { get; set; } = new();
    }

    public class TableInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("primaryFieldId")]
        public string? PrimaryFieldId { get; set; }
    }
}
