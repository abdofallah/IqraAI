using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberVonageData : BusinessNumberData
    {
        public override TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.Vonage;
    }
}
