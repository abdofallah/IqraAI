using IqraCore.Entities.Helper.Number;

namespace IqraCore.Entities.Number
{
    public class NumberPhysicalData : NumberData
    {
        public override NumberProviderEnum Provider { get; set; } = NumberProviderEnum.Physical;

        public NumberPhysicalStatusEnum Status { get; set; } = NumberPhysicalStatusEnum.Unknown;
    }
}