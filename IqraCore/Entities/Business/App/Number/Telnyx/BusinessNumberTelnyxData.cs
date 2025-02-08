using IqraCore.Entities.Helper.Business;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberTelnyxData : BusinessNumberData
    {
        public override BusinessNumberProviderEnum Provider { get; set; } = BusinessNumberProviderEnum.Telnyx;
    }
}
