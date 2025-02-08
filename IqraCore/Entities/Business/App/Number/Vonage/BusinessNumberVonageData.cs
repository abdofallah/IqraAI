using IqraCore.Entities.Helper.Business;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberVonageData : BusinessNumberData
    {
        public override BusinessNumberProviderEnum Provider { get; set; } = BusinessNumberProviderEnum.Vonage;
    }
}
