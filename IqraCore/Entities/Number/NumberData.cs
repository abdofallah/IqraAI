using IqraCore.Attributes;
using IqraCore.Entities.Helper.Number;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Number
{
    [BsonKnownTypes(typeof(NumberPhysicalData), typeof(NumberTwilioData), typeof(NumberVonageData), typeof(NumberTelnyxData))]
    public class NumberData
    {
        public NumberData() { }
        public NumberData(NumberData data)
        {
            this.Id = data.Id;
            this.CountryCode = data.CountryCode;
            this.Number = data.Number;
            this.AssignedToBusinessId = data.AssignedToBusinessId;
            this.RegionId = data.RegionId;
            this.Provider = data.Provider;
            this.MasterUserEmail = data.MasterUserEmail;
            this.Permissions = data.Permissions;
        }

        [BsonId]
        public string Id { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        public long? AssignedToBusinessId { get; set; } = null;
        public string RegionId { get; set; } = string.Empty;

        public virtual NumberProviderEnum Provider { get; set; } = NumberProviderEnum.Unknown;

        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/numbers")]
        [IncludeInEndpoint("/app/admin/numbers/{provider}")]
        public string MasterUserEmail { get; set; } = string.Empty;
        
        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/numbers")]
        [IncludeInEndpoint("/app/admin/numbers/{provider}")]
        public NumberPermission Permissions {  get; set; } = new NumberPermission();
    }
}
