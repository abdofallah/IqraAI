namespace IqraCore.Entities.Business.App.Campaign
{
    public class BusinessAppCampaignVariables
    {
        public List<BusinessAppCampaignVariableData> DynamicVariables { get; set; } = new List<BusinessAppCampaignVariableData>();
        public List<BusinessAppCampaignVariableData> Metadata { get; set; } = new List<BusinessAppCampaignVariableData>();
    }

    public class BusinessAppCampaignVariableData
    {
        public string Key { get; set; }
        public bool IsRequried { get; set; }
        public bool IsEmptyOrNullAllowed { get; set; }
    }
}