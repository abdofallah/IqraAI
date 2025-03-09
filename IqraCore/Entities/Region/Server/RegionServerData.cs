using IqraCore.Entities.Helper.Region;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Region
{
    [BsonKnownTypes(typeof(RegionProxyServerData), typeof(RegionBackendServerData))]
    public class RegionServerData
    {
        [BsonId]
        public string Endpoint { get; set; } = string.Empty;
        public string APIKey { get; set; } = string.Empty;

        public virtual RegionServerTypeEnum Type { get; set; } = RegionServerTypeEnum.Unknown;

        public DateTime? DisabledAt { get; set; } = null;
    }
}
