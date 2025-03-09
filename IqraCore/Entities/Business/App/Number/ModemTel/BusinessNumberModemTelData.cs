using IqraCore.Entities.Helper.Business;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberModemTelData : BusinessNumberData
    {
        public BusinessNumberModemTelData(BusinessNumberData data) : base(data)
        {
        }

        public override BusinessNumberProviderEnum Provider { get; set; } = BusinessNumberProviderEnum.ModemTel;

        public BusinessNumberModemTelStatusEnum Status { get; set; } = BusinessNumberModemTelStatusEnum.Unknown;

        public string PhoneNumberId { get; set; } = "";
    }
}