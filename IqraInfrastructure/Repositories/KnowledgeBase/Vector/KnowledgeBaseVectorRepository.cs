using IqraCore.Entities.Helpers;
using IqraCore.Models.KnowledgeBase;
using Microsoft.Extensions.Logging;
using System;

namespace IqraInfrastructure.Repositories.KnowledgeBase.Vector
{
    public class KnowledgeBaseVectorRepository
    {
        private readonly MilvusKnowledgeBaseClient _milvusClient;
        private readonly ILogger<KnowledgeBaseVectorRepository> _logger;

        private readonly string DatabaseName;

        // Field names are defined as constants to ensure consistency
        private const string FieldChunkId = "chunk_id";
        private const string FieldDocumentId = "document_id";
        private const string FieldParentChunkId = "parent_chunk_id";
        private const string FieldTextChunk = "text_chunk";
        private const string FieldEmbedding = "vector";

        public KnowledgeBaseVectorRepository(MilvusKnowledgeBaseClient milvusClient, string databaseName, ILogger<KnowledgeBaseVectorRepository> logger)
        {
            DatabaseName = databaseName;
            _milvusClient = milvusClient;
            _logger = logger;
        }

        public async Task<bool> CreateCollectionAsync(string collectionName, int vectorDimension, bool hybrirdSearchEnabled, CancellationToken cancellationToken = default)
        {
            try
            {
                var createCollectionRequest = new CreateCollectionRequest(
                    dbName: DatabaseName,
                    collectionName: collectionName,
                    primaryFieldName: FieldChunkId,
                    vectorFieldName: FieldEmbedding,
                    schema: new SchemaParameter(
                        autoID: false,
                        enabledDynamicField: false,
                        fields: new List<SchemaFieldParameter>()
                        {
                            new SchemaFieldParameter(
                                fieldName: FieldChunkId,
                                dataType: "VarChar",
                                nullable: false,
                                isPrimary: true,
                                elementTypeParams: new Dictionary<string, object>()
                                {
                                    { "max_length", 255 }
                                }
                            ),
                            new SchemaFieldParameter(
                                fieldName: FieldEmbedding,
                                dataType: "FloatVector",
                                nullable: false,
                                isPrimary: false,
                                elementTypeParams: new Dictionary<string, object> {
                                    { "dim", vectorDimension }
                                }
                            ),
                            new SchemaFieldParameter(
                                fieldName: FieldDocumentId,
                                dataType: "VarChar",
                                nullable: false,
                                isPrimary: false,
                                elementTypeParams: new Dictionary<string, object>()
                                {
                                    { "max_length", 255 }
                                }
                            ),
                            new SchemaFieldParameter(
                                fieldName: FieldParentChunkId,
                                dataType: "VarChar",
                                nullable: true,
                                isPrimary: false,
                                elementTypeParams: new Dictionary<string, object>()
                                {
                                    { "max_length", 255 }
                                }
                            ),
                            new SchemaFieldParameter(
                                fieldName: FieldTextChunk,
                                dataType: "VarChar",
                                nullable: false,
                                isPrimary: false,
                                elementTypeParams: new Dictionary<string, object>()
                                {
                                    { "max_length", 4000 }
                                }
                            )
                        }
                    ),
                    indexParams: new List<IndexParameter>
                    {
                        new IndexParameter(
                            fieldName: FieldEmbedding,
                            metricType: "IP",
                            indexType: "HNSW",
                            indexName: $"idx_{FieldEmbedding}",
                            @params: new Dictionary<string, object> {
                                { "dim", vectorDimension },
                                { "M", 8 },
                                { "efConstruction", 64 }
                            }
                        )
                    }
                );

                bool collectionCreated = await _milvusClient.CreateCollectionAsync(createCollectionRequest, cancellationToken);
                if (!collectionCreated)
                {
                    _logger.LogError("Failed to create collection {CollectionName} via Milvus client.", collectionName);
                    return false;
                }

                bool releaseCollection = await _milvusClient.ReleaseCollectionAsync(DatabaseName, collectionName, cancellationToken);
                if (!releaseCollection)
                {
                    _logger.LogError("Failed to release collection {CollectionName} via Milvus client.", collectionName);
                    // todo is this major? how to release for unused collection, maybe the collection load manager? but why did it fail?
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create collection and index for {CollectionName} with error: {Error}", collectionName, ex.Message);
                return false;
            }
        }

        public async Task<bool> AddChunksAsync(string collectionName, IEnumerable<VectorKnowledgeBaseChunkModel> chunks, CancellationToken cancellationToken = default)
        {
            try
            {
                var dataToInsert = chunks.Select(chunk =>
                {
                    var chunkData = new Dictionary<string, object?>
                    {
                        { FieldDocumentId, chunk.DocumentId },
                        { FieldChunkId, chunk.ChunkId },
                        { FieldTextChunk, chunk.TextChunk },
                        { FieldEmbedding, chunk.Embedding },
                        { FieldParentChunkId, chunk.ParentChunkId }
                    };

                    return chunkData;
                }).ToList();

                if (!dataToInsert.Any())
                {
                    _logger.LogInformation("AddChunksAsync called with no chunks to insert into {CollectionName}.", collectionName);
                    return true;
                }

                var request = new InsertRequest(DatabaseName, collectionName, dataToInsert);
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

        public async Task<FunctionReturnResult<List<KnowledgeBaseSearchResultModel>>> SearchAsync(string collectionName, ReadOnlyMemory<float> queryVector, int topK, bool parentChildSearch, string? filter = null, CancellationToken cancellationToken = default)
        {
            var result = new FunctionReturnResult<List<KnowledgeBaseSearchResultModel>>();

            try
            {
                var outputFields = new List<string> { FieldChunkId, FieldTextChunk, FieldDocumentId };
                if (parentChildSearch)
                {
                    outputFields.Add(FieldParentChunkId);
                }

                var searchRequest = new SearchRequest(
                    dbName: DatabaseName,
                    collectionName: collectionName,
                    vectors: new List<ReadOnlyMemory<float>> { queryVector },
                    annsField: FieldEmbedding,
                    limit: topK,
                    outputFields: outputFields,
                    filter: filter
                );

                var searchResponse = await _milvusClient.SearchAsync(searchRequest, cancellationToken);
                if (searchResponse?.Code != 0 || searchResponse.Data == null)
                {
                    _logger.LogError("Search failed for collection {CollectionName}. Milvus response code: {Code}", collectionName, searchResponse?.Code);
                    return result.SetFailureResult("SEARCH_FAILED", $"Search failed with code: {searchResponse?.Code}");
                }

                var processedResults = new List<KnowledgeBaseSearchResultModel>();
                foreach (var item in searchResponse.Data)
                {
                    var resultItem = new KnowledgeBaseSearchResultModel
                    {
                        Score = float.Parse(item["distance"].ToString()),
                        Text = item[FieldTextChunk].ToString() ?? string.Empty,
                        DocumentId = item[FieldDocumentId].ToString() ?? string.Empty,
                        ChunkId = item[FieldChunkId].ToString() ?? string.Empty
                    };

                    if (parentChildSearch)
                    {
                        resultItem.ParentChunkid = item[FieldParentChunkId].ToString() ?? string.Empty;
                    }

                    processedResults.Add(resultItem);
                }

                return result.SetSuccessResult(processedResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search collection {CollectionName}, Error: {Error}", collectionName, ex.Message);
                return result.SetFailureResult("SearchAsync:EXCEPTION", $"Failed to search collection {collectionName}, Error: {ex.Message}");
            }
        }

        public async Task<bool> DeleteChunksAsync(string collectionName, List<string> chunkIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var formattedIds = string.Join(", ", chunkIds.Select(id => $"\"{id}\""));
                var filterExpression = $"{FieldChunkId} in [{formattedIds}]";

                var request = new DeleteRequest(
                    dbName: DatabaseName,
                    collectionName: collectionName,
                    filter: filterExpression
                );

                bool success = await _milvusClient.DeleteAsync(request, cancellationToken);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while trying to delete chunks from collection {CollectionName}.", collectionName);
                return false;
            }
        }

        public async Task<bool> DeleteKnowledgeBaseAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _milvusClient.DropCollectionAsync(DatabaseName, collectionName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete collection {CollectionName}", collectionName);
                return false;
            }
        }
    }
}
