using IqraCore.Entities.Helper.Campaign;

namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaignWeb : BusinessAppCampaignBase
    {
        public override BusinessAppCampaignTypeENUM Type { get; set; } = BusinessAppCampaignTypeENUM.Web;

        public BusinessAppCampaignWebRegionRoute RegionRoute { get; set; } = new BusinessAppCampaignWebRegionRoute();
    }
}
