using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Number
{
    public class BusinessNumberTwilioModel : BusinessNumberModel
    {
        public BusinessNumberTwilioModel(BusinessNumberTwilioData data) : base(data)
        {
            TwilioPhoneNumberId = data.TwilioPhoneNumberId;
        }

        public string TwilioPhoneNumberId { get; set; }
    }
}
