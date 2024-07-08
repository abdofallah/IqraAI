using IqraCore.Entities.Helper.Number;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Number
{
    public class NumberData
    {
        [BsonId]
        public long Id { get; set; }
        public string MasterUserEmail { get; set; }

        public string CountryCode { get; set; }
        public string Number { get; set; }

        public long? AssignedToBusinessId { get; set; }

        public NumberProviderEnum Provider { get; set; }
    }
}
