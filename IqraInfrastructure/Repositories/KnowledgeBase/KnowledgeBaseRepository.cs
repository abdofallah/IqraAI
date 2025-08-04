using IqraCore.Entities.Helpers;
using IqraCore.Entities.KnowledgeBase;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IqraInfrastructure.Repositories.KnowledgeBase
{
    /// <summary>
    /// A high-level repository for interacting with the knowledge base.
    /// It uses the MilvusKnowledgeBaseClient to communicate with the Milvus service
    /// and translates between application-specific models and Milvus API DTOs.
    /// </summary>
    public class KnowledgeBaseRepository
    {
        private readonly MilvusKnowledgeBaseClient _milvusClient;
        private readonly ILogger<KnowledgeBaseRepository> _logger;

        // Field names are defined as constants to ensure consistency
        private const string FieldChunkId = "chunk_id";
        private const string FieldDocumentName = "document_name";
        private const string FieldTextChunk = "text_chunk";
        private const string FieldEmbedding = "embedding";

        public KnowledgeBaseRepository(MilvusKnowledgeBaseClient milvusClient, ILogger<KnowledgeBaseRepository> logger)
        {
            _milvusClient = milvusClient;
            _logger = logger;
        }

        /// <summary>
        /// Creates a collection with a predefined schema and then creates an index on the vector field.
        /// This is a correction and improvement over the original implementation.
        /// </summary>
        public async Task<bool> CreateCollectionAsync(string collectionName, int vectorDimension, CancellationToken cancellationToken = default)
        {
            try
            {
                // Note: The Milvus v2.5 REST API does not support creating a collection with a complex schema
                // in a single "quick setup" call. We define the schema and then create the collection.
                // The following schema corrects the mismatch in the original user code.
                var createCollectionRequest = new CreateCollectionRequest(
                    collectionName: collectionName,
                    dimension: vectorDimension,
                    metricType: "IP", // Inner Product, as used in the original code's search
                    primaryFieldName: FieldChunkId,
                    idType: "Int64",
                    autoId: true,
                    vectorFieldName: FieldEmbedding
                // Note: To add more scalar fields, you would need to use a more complex schema definition,
                // which the basic create endpoint may not support. For now, we add 'document_name' and 'text_chunk'
                // via the insert data structure, and Milvus will infer their types.
                );

                bool collectionCreated = await _milvusClient.CreateCollectionAsync(createCollectionRequest, cancellationToken);
                if (!collectionCreated)
                {
                    _logger.LogError("Failed to create collection {CollectionName} via Milvus client.", collectionName);
                    return false;
                }

                _logger.LogInformation("Successfully created collection {CollectionName}.", collectionName);

                // Phase 2: Create the index immediately after collection creation.
                var createIndexRequest = new CreateIndexRequest(
                    collectionName: collectionName,
                    indexParams: new List<IndexParameter>
                    {
                    new IndexParameter(
                        indexType: "HNSW",
                        metricType: "IP", // Must match search metric type
                        fieldName: FieldEmbedding,
                        indexName: $"idx_{FieldEmbedding}", // It's good practice to name indexes
                        @params: new Dictionary<string, object> { { "M", 8 }, { "efConstruction", 64 } }
                    )
                    }
                );

                bool indexCreated = await _milvusClient.CreateIndexAsync(createIndexRequest, cancellationToken);
                if (!indexCreated)
                {
                    _logger.LogError("Failed to create index for collection {CollectionName}.", collectionName);
                    // Optionally, you might want to drop the collection here if index creation fails.
                    return false;
                }

                _logger.LogInformation("Successfully created index on field {FieldName} for collection {CollectionName}.", FieldEmbedding, collectionName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create collection and index for {CollectionName} with error: {Error}", collectionName, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Inserts a batch of document chunks into the specified collection.
        /// </summary>
        public async Task<bool> AddChunksAsync(string collectionName, IEnumerable<KnowledgeBaseChunk> chunks, CancellationToken cancellationToken = default)
        {
            try
            {
                // Translate our application's KnowledgeBaseChunk model into the format Milvus expects: List<Dictionary<string, object>>
                var dataToInsert = chunks.Select(chunk => new Dictionary<string, object>
            {
                { FieldDocumentName, chunk.DocumentName },
                { FieldTextChunk, chunk.TextChunk },
                { FieldEmbedding, chunk.Embedding }
            }).ToList();

                if (!dataToInsert.Any())
                {
                    _logger.LogInformation("AddChunksAsync called with no chunks to insert into {CollectionName}.", collectionName);
                    return true;
                }

                var request = new InsertRequest(collectionName, dataToInsert);
                var result = await _milvusClient.InsertAsync(request, cancellationToken);

                if (result?.Code == 0 && result.Data.insertCount == dataToInsert.Count)
                {
                    _logger.LogInformation("Successfully inserted {Count} chunks into {CollectionName}.", result.Data.insertCount, collectionName);
                    return true;
                }

                _logger.LogError("Failed to insert chunks into {CollectionName}. Milvus response code: {Code}", collectionName, result?.Code);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add chunks to collection {CollectionName}, Error: {Error}", collectionName, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Searches the specified collection.
        /// IMPORTANT: This method now assumes the collection has already been loaded into memory.
        /// The loading/releasing logic is handled by the new session manager (Phase 3).
        /// </summary>
        public async Task<FunctionReturnResult<List<KnowledgeBaseSearchResult>>> SearchAsync(string collectionName, ReadOnlyMemory<float> queryVector, int topK, string? filter = null, CancellationToken cancellationToken = default)
        {
            var result = new FunctionReturnResult<List<KnowledgeBaseSearchResult>>();

            try
            {
                var searchRequest = new SearchRequest(
                    collectionName: collectionName,
                    vectors: new List<ReadOnlyMemory<float>> { queryVector },
                    annsField: FieldEmbedding,
                    limit: topK,
                    outputFields: new List<string> { FieldTextChunk, FieldDocumentName },
                    filter: filter
                );

                var searchResponse = await _milvusClient.SearchAsync(searchRequest, cancellationToken);

                if (searchResponse?.Code != 0 || searchResponse.Data == null)
                {
                    _logger.LogError("Search failed for collection {CollectionName}. Milvus response code: {Code}", collectionName, searchResponse?.Code);
                    return result.SetFailureResult("SEARCH_FAILED", $"Search failed with code: {searchResponse?.Code}");
                }

                var processedResults = new List<KnowledgeBaseSearchResult>();
                // The response `Data` is a List of dictionaries, one for each result.
                foreach (var item in searchResponse.Data)
                {
                    processedResults.Add(new KnowledgeBaseSearchResult
                    {
                        // The keys are "id", "distance", and the names of the output_fields.
                        Score = Convert.ToSingle(item["distance"]),
                        Text = item[FieldTextChunk].ToString() ?? string.Empty,
                        DocumentName = item[FieldDocumentName].ToString() ?? string.Empty
                    });
                }

                return result.SetSuccessResult(processedResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search collection {CollectionName}, Error: {Error}", collectionName, ex.Message);
                return result.SetFailureResult("SearchAsync:EXCEPTION", $"Failed to search collection {collectionName}, Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes an entire collection. Use with caution.
        /// </summary>
        public async Task<bool> DeleteKnowledgeBaseAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _milvusClient.DropCollectionAsync(collectionName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete collection {CollectionName}", collectionName);
                return false;
            }
        }
    }
}
