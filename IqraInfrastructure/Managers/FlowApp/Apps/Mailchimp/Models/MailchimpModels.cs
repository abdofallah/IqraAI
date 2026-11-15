using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp.Models
{
    // --- Add / update member (PUT upsert) ---

    public class UpsertMemberRequest
    {
        [JsonPropertyName("email_address")]
        public string EmailAddress { get; set; } = string.Empty;

        [JsonPropertyName("status_if_new")]
        public string StatusIfNew { get; set; } = "subscribed";

        [JsonPropertyName("merge_fields")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object?>? MergeFields { get; set; }
    }

    public class MemberResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email_address")]
        public string? EmailAddress { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    // --- Lists / audiences (fetcher) ---

    public class ListsResponse
    {
        [JsonPropertyName("lists")]
        public List<AudienceInfo> Lists { get; set; } = new();
    }

    public class AudienceInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
