namespace IqraCore.Models.RAG
{
    public class ExtractorDocumentModel
    {
        public string PageContent { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
