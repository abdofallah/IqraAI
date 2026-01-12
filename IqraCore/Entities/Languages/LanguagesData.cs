using IqraCore.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Languages
{
    public class LanguagesData
    {
        [BsonId]
        public string Id { get; set; } = "";

        public string LocaleName { get; set; } = "";
        public string Name { get; set; } = "";

        [ExcludeInAllEndpoints]
        public LanguagePromptsData Prompts { get; set; } = new();

        public DateTime? DisabledAt { get; set; } = null;
        public string? PublicDisabledReason { get; set; } = null;

        [ExcludeInAllEndpoints]
        public string? PrivateDisabledReason { get; set; } = null;
    }
}
