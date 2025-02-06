using IqraCore.Entities.Helper.Number;

namespace IqraCore.Entities.Number
{
    public class NumberTwilioData : NumberData
    {
        public override NumberProviderEnum Provider { get; set; } = NumberProviderEnum.Twilio;

        public NumberTwilioStatusEnum Status { get; set; } = NumberTwilioStatusEnum.Unknown;
    }
}
