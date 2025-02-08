using IqraCore.Entities.Helper.Business;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberTwilioData : BusinessNumberData
    {
        public override BusinessNumberProviderEnum Provider { get; set; } = BusinessNumberProviderEnum.Twilio;

        public BusinessNumberTwilioStatusEnum Status { get; set; } = BusinessNumberTwilioStatusEnum.Unknown;
    }
}
