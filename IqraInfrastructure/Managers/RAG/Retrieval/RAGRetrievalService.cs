using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraCore.Models.RAG.Retrieval;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.KnowledgeBase;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.RAG;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.RAG.Retrieval
{
    public record RAGRetrievalOptions
    {
        public required string Query { get; init; }
        public required int TopK { get; init; }
        public double? ScoreThreshold { get; init; }
    }

    public class RAGRetrievalService : IAsyncDisposable
    {
        private readonly ILogger<RAGRetrievalService> _logger;

        private readonly BusinessManager _businessManager;
        private readonly KnowledgeBaseVectorRepository _vectorRepository;
        private readonly RAGKeywordStore _keywordStore;
        private readonly EmbeddingProviderManager _embeddingManager;
        private readonly BusinessKnowledgeBaseDocumentRepository _documentRepository;
        private readonly KnowledgeBaseCollectionsLoadManager _knowledgeBaseCollectionsLoadManager;
        private readonly string _collectionLoadSessionId;
        private readonly TimeSpan _collectionReleaseExpiry;

        private long _businessId;
        private BusinessAppKnowledgeBase _knowledgeBaseData;

        private IEmbeddingService? _embeddingService;
        

        public RAGRetrievalService(
            ILogger<RAGRetrievalService> logger,

            BusinessManager businessManager,
            KnowledgeBaseVectorRepository vectorRepository,
            RAGKeywordStore keywordStore,
            EmbeddingProviderManager embeddingManager,
            BusinessKnowledgeBaseDocumentRepository documentRepository,
            KnowledgeBaseCollectionsLoadManager knowledgeBaseCollectionsLoadManager,
            string vectorCollectionLoadSessionId,
            TimeSpan vectorCollectionReleaseExpiry
        )
        {
            _logger = logger;

            _businessManager = businessManager;
            _vectorRepository = vectorRepository;
            _keywordStore = keywordStore;
            _embeddingManager = embeddingManager;
            _documentRepository = documentRepository;
            _knowledgeBaseCollectionsLoadManager = knowledgeBaseCollectionsLoadManager;

            _collectionLoadSessionId = vectorCollectionLoadSessionId;
            _collectionReleaseExpiry = vectorCollectionReleaseExpiry;
        }

        public async Task<FunctionReturnResult> Initalize(long businessId, BusinessAppKnowledgeBase knowledgeBaseData)
        {
            var result = new FunctionReturnResult();

            try
            {
                _businessId = businessId;
                _knowledgeBaseData = knowledgeBaseData;

                if (_knowledgeBaseData.Configuration.Retrieval.Type == KnowledgeBaseRetrievalType.VectorSearch ||
                    _knowledgeBaseData.Configuration.Retrieval.Type == KnowledgeBaseRetrievalType.HybirdSearch)
                {
                    var embeddingIntegrationResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_businessId, _knowledgeBaseData.Configuration.Embedding.Id);
                    if (!embeddingIntegrationResult.Success)
                    {
                        return result.SetFailureResult(
                            $"Initalize:{embeddingIntegrationResult.Code}",
                            embeddingIntegrationResult.Message
                        );
                    }

                    var embeddingProviderResult = await _embeddingManager.BuildProviderServiceByIntegration(
                        embeddingIntegrationResult.Data!,
                        _knowledgeBaseData.Configuration.Embedding
                    );
                    if (!embeddingProviderResult.Success || embeddingProviderResult.Data == null)
                    {
                        return result.SetFailureResult(
                            $"Initalize:{embeddingProviderResult.Code}",
                            embeddingProviderResult.Message
                        );
                    }
                    _embeddingService = embeddingProviderResult.Data;

                    var collectionLoadResult = await _knowledgeBaseCollectionsLoadManager.RegisterUseAsync(
                        $"b{_businessId}_kb{_knowledgeBaseData.Id}",
                        _collectionLoadSessionId,
                        _collectionReleaseExpiry
                    );
                    if (!collectionLoadResult)
                    {
                        return result.SetFailureResult(
                            "Initalize:COLLECTION_LOAD_FAILED",
                            "Collection load failed."
                        );
                    }
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Initalize:EXCEPTION",
                    ex.Message
                );
            }
        }

        public async Task<List<RAGRetrievalDocumentModal>> RetrieveAsync(RAGRetrievalOptions options)
        {
            var retrievalType = _knowledgeBaseData.Configuration.Retrieval.Type;

            switch (retrievalType)
            {
                case KnowledgeBaseRetrievalType.VectorSearch:
                    return await SearchByVectorAsync(options);

                case KnowledgeBaseRetrievalType.FullTextSearch:
                    return await SearchByKeywordsAsync(options);

                case KnowledgeBaseRetrievalType.HybirdSearch:
                    return await SearchHybridAsync(options);

                default:
                    _logger.LogWarning("Unsupported retrieval type: {RetrievalType}", retrievalType);
                    return new List<RAGRetrievalDocumentModal>();
            }
        }

        private async Task<List<RAGRetrievalDocumentModal>> SearchHybridAsync(RAGRetrievalOptions options)
        {
            var vectorSearchTask = SearchByVectorAsync(options);
            var keywordSearchTask = SearchByKeywordsAsync(options);

            await Task.WhenAll(vectorSearchTask, keywordSearchTask);

            var vectorResults = await vectorSearchTask;
            var keywordResults = await keywordSearchTask;

            var combinedDocs = new Dictionary<string, RAGRetrievalDocumentModal>();

            // Prioritize adding vector results first as they usually have more meaningful scores
            foreach (var doc in vectorResults)
            {
                if (doc.Metadata.TryGetValue("ChunkId", out var chunkIdObj) && chunkIdObj is string chunkId)
                {
                    if (!combinedDocs.ContainsKey(chunkId))
                    {
                        combinedDocs.Add(chunkId, doc);
                    }
                }
            }

            foreach (var doc in keywordResults)
            {
                if (doc.Metadata.TryGetValue("ChunkId", out var chunkIdObj) && chunkIdObj is string chunkId)
                {
                    if (!combinedDocs.ContainsKey(chunkId))
                    {
                        combinedDocs.Add(chunkId, doc);
                    }
                    else
                    {
                        // Optional: If keyword result is also in vector results, we can add a flag or boost score
                        combinedDocs[chunkId].Metadata["KeywordMatch"] = true;
                    }
                }
            }

            return combinedDocs.Values.ToList();
        }

        private async Task<List<RAGRetrievalDocumentModal>> SearchByVectorAsync(RAGRetrievalOptions options)
        {
            var queryVectorResult = await _embeddingService!.GenerateEmbeddingForTextAsync(options.Query);
            if (!queryVectorResult.Success || queryVectorResult.Data == null)
            {
                _logger.LogWarning("Vector embedding failed: {Message}", queryVectorResult.Message);
                return new List<RAGRetrievalDocumentModal>();
            }

            var collectionName = $"b{_businessId}_kb{_knowledgeBaseData.Id}";
            var searchResult = await _vectorRepository.SearchAsync(
                collectionName,
                new ReadOnlyMemory<float>(queryVectorResult.Data),
                options.TopK,
                _knowledgeBaseData.Configuration.Chunking.Type == KnowledgeBaseChunkingType.ParentChild
            );
            if (!searchResult.Success || searchResult.Data == null)
            {
                _logger.LogWarning("Vector search failed for collection {CollectionName}: {Message}", collectionName, searchResult.Message);
                return new List<RAGRetrievalDocumentModal>();
            }

            return searchResult.Data.Select(res => new RAGRetrievalDocumentModal
            {
                PageContent = res.Text,
                Metadata = new Dictionary<string, object>
                {
                    { "Score", res.Score },
                    { "DocumentId", res.DocumentId },
                    { "ChunkId", res.ChunkId },
                    { "ParentChunkId", res.ParentChunkid ?? string.Empty }
                }
            }).ToList();
        }

        private async Task<List<RAGRetrievalDocumentModal>> SearchByKeywordsAsync(RAGRetrievalOptions options)
        {
            var chunkIds = await _keywordStore.SearchAsync(_knowledgeBaseData.Id, options.Query, options.TopK);
            if (chunkIds == null || !chunkIds.Any())
            {
                return new List<RAGRetrievalDocumentModal>();
            }

            // Hydrate the chunk IDs with content from the document store
            var chunks = await _documentRepository.GetChunksByIdsAsync(chunkIds);

            return chunks.Select(chunk =>
            {
                var doc = new RAGRetrievalDocumentModal
                {
                    PageContent = chunk.Text,
                    Metadata = new Dictionary<string, object>
                    {
                        { "ChunkId", chunk.Id },
                        { "Score", 1.0 }, // Keyword searches don't have a normalized score, assign a default
                        { "KeywordMatch", true }
                    }
                };

                if (chunk is BusinessAppKnowledgeBaseDocumentChildChunk childChunk)
                {
                    doc.Metadata["ParentChunkId"] = childChunk.ParentId;
                }

                return doc;
            }).ToList();
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_embeddingService != null)
            {
                try
                {
                    _embeddingService.Dispose();
                }
                catch { /** Ignore **/ }

                var deregisterResult = await _knowledgeBaseCollectionsLoadManager.DeregisterUseAsync(
                    $"b{_businessId}_kb{_knowledgeBaseData.Id}",
                    _collectionLoadSessionId!
                );
                if (!deregisterResult)
                {
                    _logger.LogWarning(
                        "Failed to deregister collection load session: {CollectionLoadSessionId}",
                        _collectionLoadSessionId
                    );
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
