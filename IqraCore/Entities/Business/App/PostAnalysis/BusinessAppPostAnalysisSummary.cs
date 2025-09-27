namespace IqraCore.Entities.Business
{
    public class BusinessAppPostAnalysisSummary
    {
        public bool IsActive { get; set; } = true;

        public string Prompt { get; set; } = string.Empty;
        public BusinessAppPostAnalysisSummaryParameters Parameters { get; set; } = new BusinessAppPostAnalysisSummaryParameters();
    }

    public class BusinessAppPostAnalysisSummaryParameters
    {
        public int MaxLength { get; set; } = 150;
        public BusinessAppPostAnalysisSummaryFormat Format { get; set; } = BusinessAppPostAnalysisSummaryFormat.Paragraph;
    }

    public enum BusinessAppPostAnalysisSummaryFormat
    {
        Paragraph,
        BulletPoints
    }
}