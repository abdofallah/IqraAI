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

        public List<BusinessNumberAgentScriptSMSNodeReference> AgentScriptSMSNodeReferences { get; set; } = new List<BusinessNumberAgentScriptSMSNodeReference>();

        public virtual TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.Unknown;
    }

    public class BusinessNumberAgentScriptSMSNodeReference
    {
        public string AgentId { get; set; } = string.Empty;
        public string AgentScriptId { get; set; } = string.Empty;
        public string NodeReference { get; set; } = string.Empty;
    }
}
