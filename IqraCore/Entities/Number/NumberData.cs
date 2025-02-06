using IqraCore.Attributes;
using IqraCore.Entities.Helper.Number;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Number
{
    [BsonKnownTypes(typeof(NumberPhysicalData), typeof(NumberTwilioData), typeof(NumberVonageData), typeof(NumberTelnyxData))]
    public class NumberData
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        public string AssignedToBusinessId { get; set; } = string.Empty;
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
