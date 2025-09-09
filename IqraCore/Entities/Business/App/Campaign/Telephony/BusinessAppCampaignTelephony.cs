using IqraCore.Entities.Helper.Campaign;

namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaignTelephony : BusinessAppCampaignBase
    {
        public override BusinessAppCampaignTypeENUM Type { get; set; } = BusinessAppCampaignTypeENUM.Telephony;

        public BusinessAppCampaignTelephonyNumberRoute NumberRoute { get; set; } = new BusinessAppCampaignTelephonyNumberRoute();
    }
}
