using IqraCore.Entities.Helper.Business;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    [BsonKnownTypes(typeof(BusinessNumberModemTelData), typeof(BusinessNumberTwilioData), typeof(BusinessNumberVonageData), typeof(BusinessNumberTelnyxData))]
    public class BusinessNumberData
    {
        public BusinessNumberData() { }
        public BusinessNumberData(BusinessNumberData data)
        {
            this.Id = data.Id;
            this.CountryCode = data.CountryCode;
            this.Number = data.Number;
            this.RouteId = data.RouteId;
            this.RegionId = data.RegionId;
            this.RegionWebhookEndpoint = data.RegionWebhookEndpoint;
            this.Provider = data.Provider;
            this.IntegrationId = data.IntegrationId;
        }

        [BsonId]
        public string Id { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        public string? RouteId { get; set; } = null;
        public string RegionId { get; set; } = string.Empty;
        public string RegionWebhookEndpoint { get; set; } = string.Empty;

        public string IntegrationId { get; set; } = string.Empty;

        public virtual BusinessNumberProviderEnum Provider { get; set; } = BusinessNumberProviderEnum.Unknown;
    }
}
