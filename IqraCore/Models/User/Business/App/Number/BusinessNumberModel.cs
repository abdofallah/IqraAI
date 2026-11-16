using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Models.User.Business.App.Number
{
    public class BusinessNumberModel
    {
        public BusinessNumberModel(BusinessNumberData data)
        {
            Id = data.Id;
            CountryCode = data.CountryCode;
            Number = data.Number;
            RouteId = data.RouteId;
            RegionId = data.RegionId;
            RegionServerId = data.RegionServerId;
            IntegrationId = data.IntegrationId;
            VoiceEnabled = data.VoiceEnabled;
            SmsEnabled = data.SmsEnabled;
            Provider = data.Provider;
        }

        public static BusinessNumberModel Create(BusinessNumberData data)
        {
            return data switch
            {
                BusinessNumberSipData sip => new BusinessNumberSipModel(sip),
                BusinessNumberTwilioData twilio => new BusinessNumberTwilioModel(twilio),
                BusinessNumberModemTelData modemTel => new BusinessNumberModemTelModel(modemTel),
                _ => new BusinessNumberModel(data)
            };
        }

        public string Id { get; set; }
        public string CountryCode { get; set; }
        public string Number { get; set; }
        public string? RouteId { get; set; }
        public string RegionId { get; set; }
        public string RegionServerId { get; set; }
        public string IntegrationId { get; set; }
        public bool VoiceEnabled { get; set; }
        public bool SmsEnabled { get; set; }
        public TelephonyProviderEnum Provider { get; set; }
    }
}
