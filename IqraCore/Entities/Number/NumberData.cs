using IqraCore.Entities.Helper.Number;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Number
{
    public class NumberData
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string MasterUserEmail { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        public long? AssignedToBusinessId { get; set; } = -1;

        public NumberProviderEnum Provider { get; set; } = NumberProviderEnum.Unknown;

        public NumberPermission Permissions {  get; set; } = new NumberPermission();
    }
}
