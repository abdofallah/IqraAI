using IqraCore.Interfaces.RAG;
using IqraCore.Models.RAG;
using System.Net.Http.Headers;
using System.Text.Json;

namespace IqraInfrastructure.Managers.RAG.Extractors
{
    public class UnstructuredElement
    {
        public string Text { get; set; } = string.Empty;
    }

    public class UnstructuredApiExtractor : IExtractor
    {
        private readonly string _filePath;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _strategy;

        public UnstructuredApiExtractor(string filePath, IHttpClientFactory httpClientFactory, string strategy = "hi_res")
        {
            _filePath = filePath;
            _httpClientFactory = httpClientFactory;
            _strategy = strategy;
        }

        public async Task<List<ExtractorDocumentModel>> ExtractAsync()
        {
            HttpClient httpClient = _httpClientFactory.CreateClient("UnstructuredClient");

            using var form = new MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(_filePath);
            using var fileContent = new ByteArrayContent(fileBytes);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "files", Path.GetFileName(_filePath));
            form.Add(new StringContent(_strategy), "strategy");

            var response = await httpClient.PostAsync("general/v0/general", form);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Unstructured API request failed: {response.StatusCode} - {error}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var elements = JsonSerializer.Deserialize<List<UnstructuredElement>>(jsonResponse);

            if (elements == null)
            {
                return new List<ExtractorDocumentModel>();
            }

            var combinedText = string.Join("\n\n", elements.Select(e => e.Text));

            var doc = new ExtractorDocumentModel
            {
                PageContent = combinedText,
                Metadata = new Dictionary<string, object> { { "source", _filePath } }
            };

            return new List<ExtractorDocumentModel> { doc };
        }
    }
}
