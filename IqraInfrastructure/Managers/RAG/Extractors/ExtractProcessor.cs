using IqraCore.Interfaces.RAG;

namespace IqraInfrastructure.Managers.RAG.Extractors
{
    public class ExtractProcessor
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ExtractProcessor(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IExtractor GetExtractor(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found for extraction.", filePath);
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".txt":
                case ".md":
                    return new TextExtractor(filePath);

                case ".pdf":
                    return new PdfExtractor(filePath);

                case ".doc":
                case ".docx":
                case ".ppt":
                case ".pptx":        
                case ".html":
                case ".htm":
                case ".xml":
                case ".eml":
                case ".msg":
                case ".xlsx":
                case ".xls":
                case ".csv":
                    return new UnstructuredApiExtractor(filePath, _httpClientFactory);

                default:
                    throw new NotSupportedException($"File type '{extension}' is not supported.");
            }
        }
    }
}
