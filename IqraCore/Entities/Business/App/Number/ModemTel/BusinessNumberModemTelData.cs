using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberModemTelData : BusinessNumberData
    {
        public BusinessNumberModemTelData(BusinessNumberData data) : base(data)
        {
        }

        public override TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.ModemTel;

        public BusinessNumberModemTelStatusEnum Status { get; set; } = BusinessNumberModemTelStatusEnum.Unknown;

        public string ModemTelPhoneNumberId { get; set; } = "";
    }
}