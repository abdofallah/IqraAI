using IqraCore.Attributes;
using IqraCore.Entities.Helper.Business;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business.WhiteLabelDomain
{
    [BsonKnownTypes(typeof(BusinessWhiteLabelIqraSubDomain), typeof(BusinessWhiteLabelCustomDomain))]
    public class BusinessWhiteLabelDomain
    {
        [BsonId]
        public long Id { get; set; } = -1;

        [ExcludeInAllEndpointsAttribute]
        public long BusinessId { get; set; } = -1;

        public BusinessUserWhiteLabelDomainTypeEnum Type { get; set; } = BusinessUserWhiteLabelDomainTypeEnum.Unknown;
    }
}
