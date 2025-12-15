using IqraCore.Entities.Helper.Server;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Region
{
    [BsonKnownTypes(typeof(RegionProxyServerData), typeof(RegionBackendServerData))]
    public class RegionServerData
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Endpoint { get; set; } = string.Empty;
        public bool UseSSL { get; set; } = true;
        public string APIKey { get; set; } = string.Empty;
        public int SIPPort { get; set; } = 5060;

        public virtual ServerTypeEnum Type { get; set; } = ServerTypeEnum.Unknown;

        public DateTime? DisabledAt { get; set; } = null;
        public bool IsDevelopmentServer { get; set; } = false;
    }
}
