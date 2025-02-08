using IqraCore.Entities.Helper.Business;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    [BsonKnownTypes(typeof(BusinessNumberPhysicalData), typeof(BusinessNumberTwilioData), typeof(BusinessNumberVonageData), typeof(BusinessNumberTelnyxData))]
    public class BusinessNumberData
    {
        public BusinessNumberData() { }
        public BusinessNumberData(BusinessNumberData data)
        {
            this.Id = data.Id;
            this.CountryCode = data.CountryCode;
            this.Number = data.Number;
            this.AssignedToBusinessId = data.AssignedToBusinessId;
            this.RegionId = data.RegionId;
            this.Provider = data.Provider;
        }

        [BsonId]
        public string Id { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        public long? AssignedToBusinessId { get; set; } = null;
        public string RegionId { get; set; } = string.Empty;

        public virtual BusinessNumberProviderEnum Provider { get; set; } = BusinessNumberProviderEnum.Unknown;
    }
}
