namespace IqraCore.Entities.Business
{
    public class BusinessAppToolConfigurationInputSchemea
    {
        public long Id { get; set; }
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();
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
