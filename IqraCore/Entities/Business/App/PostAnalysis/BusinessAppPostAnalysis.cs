namespace IqraCore.Entities.Business
{
    public class BusinessAppPostAnalysis
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public BusinessAppPostAnalysisGeneral General { get; set; } = new BusinessAppPostAnalysisGeneral();
        public BusinessAppPostAnalysisSummary Summary { get; set; } = new BusinessAppPostAnalysisSummary();
        public BusinessAppPostAnalysisTagging Tagging { get; set; } = new BusinessAppPostAnalysisTagging();
        public BusinessAppPostAnalysisExtraction Extraction { get; set; } = new BusinessAppPostAnalysisExtraction();
    }
}
