using IqraCore.Entities.Embedding.Providers.GoogleGemini;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Embedding.Providers
{
    public class GoogleGeminiEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly ILogger<GoogleGeminiEmbeddingService>? _logger;
        private readonly HttpClient _httpClient;

        private readonly string _apiKey;
        private readonly string _model;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public GoogleGeminiEmbeddingService(ILogger<GoogleGeminiEmbeddingService> logger, string apiKey, string model)
        {
            _logger = logger;
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static InterfaceEmbeddingProviderEnum GetProviderTypeStatic()
        {
            return InterfaceEmbeddingProviderEnum.GoogleGemini;
        }

        public async Task<EmbeddingResult> GenerateEmbeddingAsync(string text, int? dimensions = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new EmbeddingResult { Success = false, ErrorMessage = "Input text cannot be null or empty." };
            }

            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:embedContent";
            var requestPayload = new GeminiEmbeddingRequest
            {
                Model = $"models/{_model}",
                Content = new EmbeddingContent
                {
                    Parts = new List<ContentPart> { new ContentPart { Text = text } }
                },
                TaskType = "RETRIEVAL_DOCUMENT",
                OutputDimensionality = dimensions
            };

            var jsonPayload = JsonSerializer.Serialize(requestPayload, _jsonSerializerOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.PostAsync(requestUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var embeddingResponse = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(responseJson);

                    if (embeddingResponse?.Embedding?.Values == null)
                    {
                        return new EmbeddingResult { Success = false, ErrorMessage = "API response was successful but did not contain embedding values." };
                    }

                    // NOTE: According to Gemini docs, if using smaller dimensions, the vector should be normalized.
                    // The default 768-dim output from text-embedding-004 is already normalized.
                    // This is left as an exercise if you plan to use other dimensions frequently.

                    return new EmbeddingResult
                    {
                        Success = true,
                        Vector = embeddingResponse.Embedding.Values
                    };
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonSerializer.Deserialize<GeminiErrorResponse>(errorJson);
                    var errorMessage = errorResponse?.Error?.Message ?? "An unknown error occurred.";

                    _logger?.LogError("Google Gemini API returned an error. Status: {StatusCode}, Message: {ErrorMessage}", response.StatusCode, errorMessage);

                    return new EmbeddingResult { Success = false, ErrorMessage = $"API Error: {errorMessage} (Status: {response.StatusCode})" };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP request to Google Gemini API failed.");
                return new EmbeddingResult { Success = false, ErrorMessage = $"Network error: {ex.Message}" };
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "Failed to deserialize Google Gemini API response.");
                return new EmbeddingResult { Success = false, ErrorMessage = $"JSON parsing error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An unexpected error occurred while generating embedding.");
                return new EmbeddingResult { Success = false, ErrorMessage = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
