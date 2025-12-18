using MongoDB.Bson;

namespace IqraCore.Entities.Business
{
    public class BusinessAppPostAnalysis
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public BusinessAppPostAnalysisGeneral General { get; set; } = new BusinessAppPostAnalysisGeneral();
        public BusinessAppPostAnalysisConfiguration Configuration { get; set; } = new BusinessAppPostAnalysisConfiguration();
        public BusinessAppPostAnalysisSummary Summary { get; set; } = new BusinessAppPostAnalysisSummary();
        public BusinessAppPostAnalysisTagging Tagging { get; set; } = new BusinessAppPostAnalysisTagging();
        public BusinessAppPostAnalysisExtraction Extraction { get; set; } = new BusinessAppPostAnalysisExtraction();

        // References
        public List<string> InboundRoutingReferences { get; set; } = new List<string>();
        public List<string> TelephonyCampaignReferences { get; set; } = new List<string>();
        public List<string> WebCampaignReferences { get; set; } = new List<string>();
    }
}
