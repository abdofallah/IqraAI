using IqraCore.Entities.Helper.Number;

namespace IqraCore.Entities.Number
{
    public class NumberPhysical : NumberData
    {
        public NumberPhysicalHostTypeEnum HostType { get; set; } = NumberPhysicalHostTypeEnum.Unknown;
    }
}
