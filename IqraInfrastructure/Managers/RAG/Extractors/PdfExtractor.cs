using IqraCore.Models.RAG;
using IqraCore.Interfaces.RAG;

namespace IqraInfrastructure.Managers.RAG.Extractors
{
    public class PdfExtractor : IExtractor
    {
        private readonly string _filePath;

        public PdfExtractor(string filePath)
        {
            _filePath = filePath;
        }

        public Task<List<ExtractorDocumentModel>> ExtractAsync()
        {
            var documents = new List<ExtractorDocumentModel>();

            var pdf = PdfDocument.FromFile(_filePath);
            var allText = pdf.ExtractAllText();

            documents.Add(new ExtractorDocumentModel
            {
                PageContent = allText,
                Metadata = new Dictionary<string, object> { { "source", _filePath } }
            });

            return Task.FromResult(documents);
        }
    }
}
