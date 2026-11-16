using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.PostAnalysis
{
    public class BusinessAppPostAnalysisModel
    {
        public BusinessAppPostAnalysisModel(BusinessAppPostAnalysis data)
        {
            Id = data.Id;
            General = data.General;
            Configuration = data.Configuration;
            Summary = data.Summary;
            Tagging = data.Tagging;
            Extraction = data.Extraction;
        }

        public string Id { get; set; }
        public BusinessAppPostAnalysisGeneral General { get; set; }
        public BusinessAppPostAnalysisConfiguration Configuration { get; set; }
        public BusinessAppPostAnalysisSummary Summary { get; set; }
        public BusinessAppPostAnalysisTagging Tagging { get; set; }
        public BusinessAppPostAnalysisExtraction Extraction { get; set; }
    }
}
