using IqraCore.Entities.Helper.Telephony;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    [BsonKnownTypes(
        typeof(BusinessNumberModemTelData),
        typeof(BusinessNumberTwilioData),
        typeof(BusinessNumberVonageData),
        typeof(BusinessNumberTelnyxData),
        typeof(BusinessNumberSipData)
    )]
    public class BusinessNumberData
    {
        public BusinessNumberData() { }

        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string CountryCode { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        public string? RouteId { get; set; } = null;
        public string RegionId { get; set; } = string.Empty;
        public string RegionServerId { get; set; } = string.Empty;

        public string IntegrationId { get; set; } = string.Empty;

        public bool VoiceEnabled { get; set; } = false;
        public bool SmsEnabled { get; set; } = false;

        public virtual TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.Unknown;

        public List<BusinessNumberScriptSMSNodeReference> ScriptSMSNodeReferences { get; set; } = new List<BusinessNumberScriptSMSNodeReference>();
        public List<string> TelephonyCampaignDefaultNumberRouteReferences { get; set; } = new List<string>();
        public List<BusinessNumberTelephonyCampaignNumbersRouteReference> TelephonyCampaignNumbersRouteReferences { get; set; } = new List<BusinessNumberTelephonyCampaignNumbersRouteReference>();
    }

    public class BusinessNumberScriptSMSNodeReference
    {
        public string ScriptId { get; set; } = string.Empty;
        public string NodeReference { get; set; } = string.Empty;
    }

    public class BusinessNumberTelephonyCampaignNumbersRouteReference
    {
        public string CampaignId { get; set; } = string.Empty;
        public string PhoneCode { get; set; } = string.Empty;
    }
}
