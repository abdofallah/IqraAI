using IqraCore.Entities.Embedding.Providers.GoogleGemini;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.Embedding;
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
        private readonly GoogleGeminiEmbeddingServiceConfig _config;

        private readonly string _apiKey;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public GoogleGeminiEmbeddingService(ILogger<GoogleGeminiEmbeddingService> logger, string apiKey, GoogleGeminiEmbeddingServiceConfig config)
        {
            _logger = logger;

            _apiKey = apiKey;
            _config = config;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static InterfaceEmbeddingProviderEnum GetProviderTypeStatic()
        {
            return InterfaceEmbeddingProviderEnum.GoogleGemini;
        }

        public async Task<FunctionReturnResult<float[]?>> GenerateEmbeddingForTextAsync(string text)
        {
            var result = new FunctionReturnResult<float[]?>();  

            try
            {
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_config.Model}:embedContent";
                var requestPayload = new GeminiEmbeddingRequest
                {
                    Model = $"models/{_config.Model}",
                    Content = new EmbeddingContent
                    {
                        Parts = new List<ContentPart> { new ContentPart { Text = text } }
                    },
                    TaskType = "RETRIEVAL_DOCUMENT",
                    OutputDimensionality = _config.VectorDimension
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload, _jsonSerializerOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync(requestUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var embeddingResponse = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(responseJson);

                    if (embeddingResponse == null || embeddingResponse?.Embedding == null || embeddingResponse?.Embedding?.Values == null || embeddingResponse?.Embedding?.Values.Length == 0)
                    {
                        return result.SetFailureResult(
                            "GenerateEmbeddingAsync:EMPTY_EMBEDDING",
                            "API response was successful but did not contain embedding values."
                        );
                    }

                    // NOTE: According to Gemini docs, if using smaller dimensions, the vector should be normalized.
                    // The default 768-dim output from text-embedding-004 is already normalized.
                    // This is left as an exercise if you plan to use other dimensions frequently.

                    return result.SetSuccessResult(embeddingResponse.Embedding.Values);
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonSerializer.Deserialize<GeminiErrorResponse>(errorJson);
                    var errorMessage = errorResponse?.Error?.Message ?? "An unknown error occurred.";

                    return result.SetFailureResult(
                        "GenerateEmbeddingAsync:API_ERROR",
                        $"API Error: {errorMessage} (Status: {response.StatusCode})"
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                return result.SetFailureResult(
                    "GenerateEmbeddingAsync:HTTP_ERROR",
                    $"HTTP request error: {ex.Message}"
                );
            }
            catch (JsonException ex)
            {
                return result.SetFailureResult(
                    "GenerateEmbeddingAsync:JSON_ERROR",
                    $"JSON parsing error: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GenerateEmbeddingAsync:UNEXPECTED_ERROR",
                    $"An unexpected error occurred: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<List<float[]>?>> GenerateEmbeddingForTextListAsync(List<string> texts)
        {
            var result = new FunctionReturnResult<List<float[]>?>();

            try
            {
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_config.Model}:batchEmbedContents";
                var requestPayload = new
                {
                    requests = texts.Select(text => {
                        return new GeminiEmbeddingRequest
                        {
                            Model = $"models/{_config.Model}",
                            Content = new EmbeddingContent
                            {
                                Parts = [new ContentPart { Text = text }]
                            },
                            TaskType = "RETRIEVAL_DOCUMENT",
                            OutputDimensionality = _config.VectorDimension
                        };
                    }).ToList()
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload, _jsonSerializerOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync(requestUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var embeddingResponse = JsonSerializer.Deserialize<GeminiEmbeddingsListResponse>(responseJson);

                    if (embeddingResponse == null || embeddingResponse?.Embeddings == null || embeddingResponse?.Embeddings.Count == 0)
                    {
                        return result.SetFailureResult(
                            "GenerateEmbeddingAsync:EMPTY_EMBEDDING",
                            "API response was successful but did not contain embedding values."
                        );
                    }

                    // NOTE: According to Gemini docs, if using smaller dimensions, the vector should be normalized.
                    // The default 768-dim output from text-embedding-004 is already normalized.
                    // This is left as an exercise if you plan to use other dimensions frequently.

                    var convertedData = embeddingResponse.Embeddings.Select(embedding => embedding.Values).ToList();
                    if (convertedData.Any(d => d == null || d.Length == 0))
                    {
                        return result.SetFailureResult(
                            "GenerateEmbeddingAsync:EMPTY_EMBEDDING",
                            "API response was successful but did not contain embedding values."
                        );
                    }

                    return result.SetSuccessResult(convertedData);
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonSerializer.Deserialize<GeminiErrorResponse>(errorJson);
                    var errorMessage = errorResponse?.Error?.Message ?? "An unknown error occurred.";

                    return result.SetFailureResult(
                        "GenerateEmbeddingAsync:API_ERROR",
                        $"API Error: {errorMessage} (Status: {response.StatusCode})"
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                return result.SetFailureResult(
                    "GenerateEmbeddingAsync:HTTP_ERROR",
                    $"HTTP request error: {ex.Message}"
                );
            }
            catch (JsonException ex)
            {
                return result.SetFailureResult(
                    "GenerateEmbeddingAsync:JSON_ERROR",
                    $"JSON parsing error: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GenerateEmbeddingAsync:UNEXPECTED_ERROR",
                    $"An unexpected error occurred: {ex.Message}"
                );
            }
        }

        public IEmbeddingConfig GetCacheableConfig()
        {
            return _config;
        }

        public InterfaceEmbeddingProviderEnum GetProviderType()
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
