using IqraCore.Entities.Embedding.Providers.GoogleGemini;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.Rerank.Providers.GoogleGemini;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Rerank.Providers
{
    public class GoogleGeminiRerankService : IRerankService, IDisposable
    {
        private readonly ILogger<GoogleGeminiRerankService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model; // e.g., "rerank-english-001"

        public GoogleGeminiRerankService(ILogger<GoogleGeminiRerankService> logger, string apiKey, string model)
        {
            _logger = logger;
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static InterfaceRerankProviderEnum GetProviderTypeStatic()
        {
            return InterfaceRerankProviderEnum.GoogleGemini;
        }

        public async Task<RerankResult> RerankAsync(string query, List<string> documents, int topN)
        {
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:rankContent";

            var requestPayload = new GeminiRerankRequest
            {
                Model = $"models/{_model}",
                Query = query,
                TopN = topN,
                Contents = documents.Select(doc => new RerankContent
                {
                    Parts = new List<RerankContentPart> { new RerankContentPart { Text = doc } }
                }).ToList()
            };

            var jsonPayload = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.PostAsync(requestUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var rerankResponse = JsonSerializer.Deserialize<GeminiRerankResponse>(responseJson);

                    if (rerankResponse?.RankedDocuments == null)
                    {
                        return new RerankResult { Success = false, ErrorMessage = "API response was successful but did not contain ranked documents." };
                    }

                    var resultDocs = rerankResponse.RankedDocuments.Select(rankedDoc =>
                    {
                        var docText = rankedDoc.Content.Parts.First().Text;
                        return new RerankedDocument
                        {
                            Text = docText,
                            RelevanceScore = rankedDoc.Score,
                            OriginalIndex = documents.IndexOf(docText) // Find original index by matching text content
                        };
                    }).ToList();

                    return new RerankResult { Success = true, RerankedDocuments = resultDocs };
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonSerializer.Deserialize<GeminiErrorResponse>(errorJson);
                    var errorMessage = errorResponse?.Error?.Message ?? "An unknown error occurred.";
                    _logger.LogError("Google Gemini Rerank API error. Status: {StatusCode}, Message: {Msg}", response.StatusCode, errorMessage);
                    return new RerankResult { Success = false, ErrorMessage = $"API Error: {errorMessage} (Status: {response.StatusCode})" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while reranking.");
                return new RerankResult { Success = false, ErrorMessage = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
