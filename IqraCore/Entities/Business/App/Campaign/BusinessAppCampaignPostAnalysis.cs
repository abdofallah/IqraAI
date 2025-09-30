namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaignPostAnalysis
    {
        public string? PostAnalysisId { get; set; } = null;
        public List<BusinessAppCampaignPostAnalysisContextVariable>? ContextVariables { get; set; } = null;
    }

    public class BusinessAppCampaignPostAnalysisContextVariable
    { 
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
