using IqraCore.Entities.Helper.Number;

namespace IqraCore.Entities.Number
{
    public class NumberTelnyxData : NumberData
    {
        public override NumberProviderEnum Provider { get; set; } = NumberProviderEnum.Telnyx;
    }
}
