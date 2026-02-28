using IqraCore.Entities.Interfaces;
using IqraCore.Entities.Rerank.Providers.GoogleGemini;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.Rerank.Providers
{
    public class GoogleGeminiRerankService : IRerankService, IDisposable
    {
        private readonly ILogger<GoogleGeminiRerankService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GoogleGeminiRerankService(ILogger<GoogleGeminiRerankService> logger, string apiKey, string model)
        {
            _logger = logger;
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<RerankResult> RerankAsync(string query, List<string> documents, int topN)
        {
            if (string.IsNullOrWhiteSpace(query) || documents == null || documents.Count == 0)
            {
                return new RerankResult { Success = false, ErrorMessage = "Query and documents cannot be empty." };
            }

            // We need to embed the query AND all the documents.
            // Index 0 = Query
            // Index 1 to N = Documents
            var allTexts = new List<string> { query };
            allTexts.AddRange(documents);

            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:batchEmbedContents";

            var requestPayload = new GeminiRerankBatchRequest
            {
                Requests = allTexts.Select(text => new GeminiRerankRequestItem
                {
                    Model = $"models/{_model}",
                    Content = new GeminiRerankContent
                    {
                        Parts = new List<GeminiRerankPart> { new GeminiRerankPart { Text = text } }
                    },
                    TaskType = "SEMANTIC_SIMILARITY" // Critical for reranking logic
                }).ToList()
            };

            var jsonPayload = JsonSerializer.Serialize(requestPayload, _jsonSerializerOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.PostAsync(requestUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var batchResponse = JsonSerializer.Deserialize<GeminiRerankBatchResponse>(responseJson, _jsonSerializerOptions);

                    if (batchResponse?.Embeddings == null || batchResponse.Embeddings.Count != allTexts.Count)
                    {
                        return new RerankResult { Success = false, ErrorMessage = "API response did not contain expected number of embeddings." };
                    }

                    // Calculate Cosine Similarity
                    var queryEmbedding = batchResponse.Embeddings[0].Values;
                    var resultDocs = new List<RerankedDocument>();

                    for (int i = 0; i < documents.Count; i++)
                    {
                        var docEmbedding = batchResponse.Embeddings[i + 1].Values;
                        double similarity = CalculateCosineSimilarity(queryEmbedding, docEmbedding);

                        resultDocs.Add(new RerankedDocument
                        {
                            Text = documents[i],
                            RelevanceScore = similarity,
                            OriginalIndex = i
                        });
                    }

                    // Sort by highest score first, then take TopN
                    var sortedTopDocs = resultDocs
                        .OrderByDescending(d => d.RelevanceScore)
                        .Take(topN)
                        .ToList();

                    return new RerankResult { Success = true, RerankedDocuments = sortedTopDocs };
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var errorMessage = "An unknown error occurred.";
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<GeminiRerankErrorResponse>(errorJson, _jsonSerializerOptions);
                        if (errorResponse?.Error != null) errorMessage = errorResponse.Error.Message;
                    }
                    catch { errorMessage = errorJson; }

                    _logger.LogError("Google Gemini Rerank API error. Status: {StatusCode}, Message: {Msg}", response.StatusCode, errorMessage);
                    return new RerankResult { Success = false, ErrorMessage = $"API Error: {errorMessage} (Status: {response.StatusCode})" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while reranking with Google Gemini.");
                return new RerankResult { Success = false, ErrorMessage = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        private double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                return 0.0;

            double dotProduct = 0.0;
            double normA = 0.0;
            double normB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += Math.Pow(vectorA[i], 2);
                normB += Math.Pow(vectorB[i], 2);
            }

            if (normA == 0 || normB == 0) return 0.0;

            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }


        public static InterfaceRerankProviderEnum GetProviderTypeStatic()
        {
            return InterfaceRerankProviderEnum.GoogleGemini;
        }
        public InterfaceRerankProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
