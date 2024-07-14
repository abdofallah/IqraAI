using IqraCore.Entities.Helper.Number;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Number
{
    [BsonKnownTypes(typeof(NumberPhysicalIqra), typeof(NumberPhysicalUser))]
    public class NumberPhysical : NumberData
    {
        public NumberPhysicalHostTypeEnum HostType { get; set; } = NumberPhysicalHostTypeEnum.Unknown;
    }
}
