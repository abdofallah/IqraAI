using IqraCore.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    [BsonKnownTypes(typeof(BusinessAppToolConfigurationInputSchemeaDateTime))]
    public class BusinessAppToolConfigurationInputSchemea
    {
        public BusinessAppToolConfigurationInputSchemea() { }

        public BusinessAppToolConfigurationInputSchemea(BusinessAppToolConfigurationInputSchemea data)
        {
            Id = data.Id;
            Name = data.Name;
            Description = data.Description;
            Type = data.Type;
            IsArray = data.IsArray;
            IsRequired = data.IsRequired;
        }

        public string Id { get; set; }

        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();
        public BusinessAppToolConfigurationInputSchemeaTypeEnum Type { get; set; } = BusinessAppToolConfigurationInputSchemeaTypeEnum.Unknown;
        public bool IsArray { get; set; } = false;
        public bool IsRequired { get; set; } = false;
    }

    public enum BusinessAppToolConfigurationInputSchemeaTypeEnum
    { 
        Unknown = 0,
        String = 1,
        Number = 2,
        Boolean = 3,
        DateTime = 4
    }
}
