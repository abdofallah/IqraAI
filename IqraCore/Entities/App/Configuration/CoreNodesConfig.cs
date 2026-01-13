using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.App.Configuration
{
    [BsonIgnoreExtraElements]
    public class CoreNodesConfig
    {
        public string BackgroundNodeEndpoint { get; set; }
        public bool BackgroundNodeUseSSL { get; set; }
        public string BackgroundNodeApiKey { get; set; }
    }
}
