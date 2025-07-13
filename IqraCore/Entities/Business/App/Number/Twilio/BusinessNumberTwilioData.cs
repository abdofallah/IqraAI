using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberTwilioData : BusinessNumberData
    {
        public BusinessNumberTwilioData(BusinessNumberData data) : base(data)
        {
        }

        public override TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.Twilio;

        public string TwilioPhoneNumberId { get; set; } = string.Empty;
    }
}
