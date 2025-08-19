using IqraCore.Interfaces.RAG;
using IqraCore.Models.RAG;
using System.Text;
using UtfUnknown;

namespace IqraInfrastructure.Managers.RAG.Extractors
{
    public class TextExtractor : IExtractor
    {
        private readonly string _filePath;

        public TextExtractor(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<List<ExtractorDocumentModel>> ExtractAsync()
        {
            var encoding = GetEncoding(_filePath);
            string content = await File.ReadAllTextAsync(_filePath, encoding);

            var doc = new ExtractorDocumentModel
            {
                PageContent = content,
                Metadata = new Dictionary<string, object> { { "source", _filePath } }
            };

            return new List<ExtractorDocumentModel> { doc };
        }

        private static Encoding GetEncoding(string filePath)
        {
            var cdet = CharsetDetector.DetectFromFile(filePath);
            return cdet.Detected.Encoding != null ? cdet.Detected.Encoding : Encoding.UTF8;
        }
    }
}
