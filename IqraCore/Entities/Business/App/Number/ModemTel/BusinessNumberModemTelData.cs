using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberModemTelData : BusinessNumberData
    {
        public BusinessNumberModemTelData()
        {
        }

        public override TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.ModemTel;

        public string ModemTelPhoneNumberId { get; set; } = "";
    }
}