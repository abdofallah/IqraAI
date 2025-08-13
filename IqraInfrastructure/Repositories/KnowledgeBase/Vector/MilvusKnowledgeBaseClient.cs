using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Repositories.KnowledgeBase.Vector
{
    /// <summary>
    /// Configuration options for connecting to Milvus.
    /// To be registered in appsettings.json and loaded via IOptions<MilvusOptions>.
    /// </summary>
    public class MilvusOptions
    {
        public required string Endpoint { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required int ExpiryCheckIntervalSeconds { get; set; }
        public required int CollectionStaleTimeoutMinutes { get; set; }
    }

    #region Data Transfer Objects (DTOs)

    // Generic wrapper for Milvus API responses.
    public record MilvusResponse<T>(int Code, T Data);
    public record MilvusEmptyResponse(int Code);


    // DTOs for Collection Management
    public record CreateCollectionRequest(
        string collectionName,
        int dimension,
        string metricType,
        string primaryFieldName,
        string idType,
        bool autoId,
        string vectorFieldName
    );

    public record DropCollectionRequest(string collectionName);
    public record LoadCollectionRequest(string collectionName);
    public record ReleaseCollectionRequest(string collectionName);

    // DTOs for Index Management
    public record CreateIndexRequest(string collectionName, List<IndexParameter> indexParams);
    public record DropIndexRequest(string collectionName, string indexName);
    public record IndexParameter(
        string indexType,
        string metricType,
        string fieldName,
        string indexName,
        Dictionary<string, object> @params);

    // DTOs for Entity (Data) Management
    public record InsertRequest(string collectionName, [property: JsonPropertyName("data")] List<Dictionary<string, object>> Data);
    public record InsertResponseData(int insertCount, List<object> insertIds);

    public record DeleteRequest(string collectionName, string filter);

    public record SearchRequest(
        string collectionName,
        [property: JsonPropertyName("data")] List<ReadOnlyMemory<float>> vectors,
        string annsField,
        int limit,
        List<string> outputFields,
        string? filter = null
    // Note: Other parameters like 'searchParams' can be added here if needed.
    );

    // The Search response is a list of dictionaries, so we use this flexible type.
    public record MilvusSearchResponse(int Code, List<Dictionary<string, object>> Data);

    #endregion

    /// <summary>
    /// A low-level client for interacting with the Milvus v2.5.x REST API.
    /// This class handles the direct HTTP communication.
    /// </summary>
    public class MilvusKnowledgeBaseClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MilvusKnowledgeBaseClient> _logger;
        private readonly MilvusOptions _options;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public MilvusKnowledgeBaseClient(IHttpClientFactory httpClientFactory, MilvusOptions options, ILogger<MilvusKnowledgeBaseClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient("MilvusClient");
            _options = options;
            _logger = logger;

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_options.Endpoint);
            var authToken = $"{_options.Username}:{_options.Password}";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            // Configure JSON serialization to match Milvus API expectations
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // --- Collection Endpoints ---

        public async Task<bool> CreateCollectionAsync(CreateCollectionRequest request, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<MilvusEmptyResponse>("/v2/vectordb/collections/create", request, cancellationToken);
            return response?.Code == 0;
        }

        public async Task<bool> DropCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            var request = new DropCollectionRequest(collectionName);
            var response = await PostAsync<MilvusEmptyResponse>("/v2/vectordb/collections/drop", request, cancellationToken);
            return response?.Code == 0;
        }

        public async Task<bool> LoadCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            var request = new LoadCollectionRequest(collectionName);
            var response = await PostAsync<MilvusEmptyResponse>("/v2/vectordb/collections/load", request, cancellationToken);
            return response?.Code == 0;
        }

        public async Task<bool> ReleaseCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            var request = new ReleaseCollectionRequest(collectionName);
            var response = await PostAsync<MilvusEmptyResponse>("/v2/vectordb/collections/release", request, cancellationToken);
            return response?.Code == 0;
        }

        // --- Index Endpoints ---

        public async Task<bool> CreateIndexAsync(CreateIndexRequest request, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<MilvusEmptyResponse>("/v2/vectordb/indexes/create", request, cancellationToken);
            return response?.Code == 0;
        }

        public async Task<bool> DropIndexAsync(DropIndexRequest request, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<MilvusEmptyResponse>("/v2/vectordb/indexes/drop", request, cancellationToken);
            return response?.Code == 0;
        }

        // --- Entity Endpoints ---

        public async Task<MilvusResponse<InsertResponseData>?> InsertAsync(InsertRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<MilvusResponse<InsertResponseData>>("/v2/vectordb/entities/insert", request, cancellationToken);
        }

        public async Task<bool> DeleteAsync(DeleteRequest request, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<MilvusEmptyResponse>("/v2/vectordb/entities/delete", request, cancellationToken);
            return response?.Code == 0;
        }

        public async Task<MilvusSearchResponse?> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<MilvusSearchResponse>("/v2/vectordb/entities/search", request, cancellationToken);
        }

        /// <summary>
        /// Generic helper method to execute POST requests to the Milvus API.
        /// </summary>
        private async Task<TResponse?> PostAsync<TResponse>(string endpoint, object payload, CancellationToken cancellationToken) where TResponse : class
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, payload, _jsonSerializerOptions, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Milvus API request to {Endpoint} failed with status code {StatusCode}. Response: {Response}", endpoint, response.StatusCode, errorContent);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonSerializerOptions, cancellationToken);

                // Milvus uses a 'code' field in the response body to indicate success (0) or failure.
                var codeProperty = typeof(TResponse).GetProperty("Code");
                if (codeProperty != null)
                {
                    var code = (int?)codeProperty.GetValue(result);
                    if (code.HasValue && code.Value != 0)
                    {
                        _logger.LogWarning("Milvus API request to {Endpoint} succeeded with HTTP status, but returned an error code: {MilvusCode}. Response: {Response}",
                           endpoint, code, await response.Content.ReadAsStringAsync(cancellationToken));
                    }
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request exception occurred when calling Milvus endpoint {Endpoint}.", endpoint);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to serialize request or deserialize response for Milvus endpoint {Endpoint}.", endpoint);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected exception occurred when calling Milvus endpoint {Endpoint}.", endpoint);
                return null;
            }
        }
    }
}
