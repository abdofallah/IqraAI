using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Number
{
    public class BusinessNumberModemTelModel : BusinessNumberModel
    {
        public BusinessNumberModemTelModel(BusinessNumberModemTelData data) : base(data)
        {
            ModemTelPhoneNumberId = data.ModemTelPhoneNumberId;
        }

        public string ModemTelPhoneNumberId { get; set; }
    }
}
