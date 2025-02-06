using IqraCore.Entities.Helper.Number;

namespace IqraCore.Entities.Number
{
    public class NumberVonageData : NumberData
    {
        public override NumberProviderEnum Provider { get; set; } = NumberProviderEnum.Vonage;
    }
}
