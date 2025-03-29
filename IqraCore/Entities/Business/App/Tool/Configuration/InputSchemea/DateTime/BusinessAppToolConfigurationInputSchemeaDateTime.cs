namespace IqraCore.Entities.Business
{ 
    public class BusinessAppToolConfigurationInputSchemeaDateTime : BusinessAppToolConfigurationInputSchemea
    {
        public BusinessAppToolConfigurationInputSchemeaDateTime() : base() { }
        public BusinessAppToolConfigurationInputSchemeaDateTime(BusinessAppToolConfigurationInputSchemea baseData) : base(baseData) { }
        public string DateTimeFormat { get; set; } = string.Empty;
    }
}
