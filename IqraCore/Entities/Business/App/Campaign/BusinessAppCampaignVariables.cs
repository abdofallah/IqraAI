namespace IqraCore.Entities.Business.App.Campaign
{
    public class BusinessAppCampaignVariables
    {
        public Dictionary<string, BusinessAppCampaignVariableData> DynamicVariables { get; set; } = new Dictionary<string, BusinessAppCampaignVariableData>();
        public Dictionary<string, BusinessAppCampaignVariableData> Metadata { get; set; } = new Dictionary<string, BusinessAppCampaignVariableData>();
    }

    public class BusinessAppCampaignVariableData
    {
        public bool IsRequried { get; set; }
        public bool IsEmptyOrNullAllowed { get; set; }
        public string? DefaultValue { get; set; }
    }
}