using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.FlowApp.Apps.HubSpot.Models
{
    // --- Search ---

    public class SearchContactsRequest
    {
        [JsonPropertyName("filterGroups")]
        public List<FilterGroup> FilterGroups { get; set; } = new();

        [JsonPropertyName("properties")]
        public List<string> Properties { get; set; } = new();

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 1;
    }

    public class FilterGroup
    {
        [JsonPropertyName("filters")]
        public List<Filter> Filters { get; set; } = new();
    }

    public class Filter
    {
        [JsonPropertyName("propertyName")]
        public string PropertyName { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; set; } = "EQ";

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    public class SearchContactsResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("results")]
        public List<HubSpotContact> Results { get; set; } = new();
    }

    public class HubSpotContact
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("properties")]
        public Dictionary<string, object?> Properties { get; set; } = new();
    }

    // --- Create / update ---

    public class ContactWriteRequest
    {
        [JsonPropertyName("properties")]
        public Dictionary<string, object?> Properties { get; set; } = new();
    }
}
