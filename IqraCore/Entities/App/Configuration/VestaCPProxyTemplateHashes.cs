using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.App.Configuration
{
    [BsonIgnoreExtraElements]
    public class VestaCPProxyTemplateHashes
    {
        public Dictionary<string, string> TemplateHashes { get; set; }
    }
}
