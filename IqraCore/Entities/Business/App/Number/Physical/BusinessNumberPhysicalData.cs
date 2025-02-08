using IqraCore.Entities.Helper.Business;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberPhysicalData : BusinessNumberData
    {
        public BusinessNumberPhysicalData(BusinessNumberData data) : base(data)
        {
        }

        public override BusinessNumberProviderEnum Provider { get; set; } = BusinessNumberProviderEnum.Physical;

        public BusinessNumberPhysicalStatusEnum Status { get; set; } = BusinessNumberPhysicalStatusEnum.Unknown;
    }
}