using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Helpers;
using IqraCore.Models.RAG.Retrieval;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.RAG.PostProcessing;
using IqraInfrastructure.Managers.RAG.Retrieval;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.RAG;
using Microsoft.Extensions.Logging;
using System.Text;

namespace IqraInfrastructure.Managers.KnowledgeBase.Retrieval
{
    public class KnowledgeBaseRetrievalManager : IAsyncDisposable
    {
        private readonly BusinessManager _businessManager;
        private readonly RAGRetrievalService _retrievalService;
        private readonly RAGDataPostProcessor _dataPostProcessor;
        private readonly BusinessKnowledgeBaseDocumentRepository _documentRepository;

        private readonly long _businessId;
        private readonly string _knowledgeBaseId;

        private BusinessAppKnowledgeBase _knowledgeBaseData;

        private bool _isInitialized = false;

        public KnowledgeBaseRetrievalManager(
            ILoggerFactory loggerFactory,
            BusinessManager businessManager,
            KnowledgeBaseVectorRepository knowledgeBaseVectorRepository,
            RAGKeywordStore ragKeywordStore,
            BusinessKnowledgeBaseDocumentRepository documentRepository,
            EmbeddingProviderManager embeddingProviderManager,
            RerankProviderManager rerankProviderManager,
            KnowledgeBaseCollectionsLoadManager knowledgeBaseCollectionsLoadManager,
            long businessId,
            string knowledgeBaseId,
            string vectorCollectionLoadSessionId,
            TimeSpan vectorCollectionReleaseExpiry
        )
        {
            _businessId = businessId;
            _knowledgeBaseId = knowledgeBaseId;

            _businessManager = businessManager;
            _documentRepository = documentRepository;

            _retrievalService = new RAGRetrievalService(
                loggerFactory.CreateLogger<RAGRetrievalService>(),
                businessManager,
                knowledgeBaseVectorRepository,
                ragKeywordStore,
                embeddingProviderManager,
                documentRepository,
                knowledgeBaseCollectionsLoadManager,
                vectorCollectionLoadSessionId,
                vectorCollectionReleaseExpiry
            );
            _dataPostProcessor = new RAGDataPostProcessor(
                businessManager,
                rerankProviderManager
            );
        }

        public async Task<FunctionReturnResult> Initalize()
        {
            var result = new FunctionReturnResult();

            try
            {
                if (_isInitialized)
                {
                    return result.SetSuccessResult();
                }

                var knowledgeBaseResult = await _businessManager.GetKnowledgeBaseManager().GetKnowledgeBaseById(_businessId, _knowledgeBaseId);
                if (!knowledgeBaseResult.Success)
                {
                    return result.SetFailureResult(
                        $"Initalize:{knowledgeBaseResult.Code}",
                        knowledgeBaseResult.Message
                    );
                }
                _knowledgeBaseData = knowledgeBaseResult.Data!;

                var retrievalInitResult = await _retrievalService.Initalize(_businessId, _knowledgeBaseData);
                if (!retrievalInitResult.Success)
                {
                    return result.SetFailureResult(
                        $"Initalize:{retrievalInitResult.Code}",
                        retrievalInitResult.Message
                    );
                }

                var postProcessingInitResult = await _dataPostProcessor.Initalize(_businessId, _knowledgeBaseData);
                if (!postProcessingInitResult.Success)
                {
                    return result.SetFailureResult(
                        $"Initalize:{postProcessingInitResult.Code}",
                        postProcessingInitResult.Message
                    );
                }

                _isInitialized = true;
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

        public async Task<FunctionReturnResult<RAGRetrievalResultModel?>> RetrieveContextAsync(long businessId, string knowledgeBaseId, string query)
        {
            var result = new FunctionReturnResult<RAGRetrievalResultModel?>();

            try
            {
                if (!_isInitialized)
                {
                    return result.SetFailureResult(
                        "RetrieveContextAsync:NOT_INITIALIZED",
                        "Current class is not initalized."
                    );
                }

                var retrievalConfig = _knowledgeBaseData.Configuration.Retrieval;
                int topK = GetTopK(retrievalConfig);
                double? scoreThreshold = GetScoreThreshold(retrievalConfig);

                // 1. Retrieve raw documents
                var retrievalOptions = new RAGRetrievalOptions
                {
                    Query = query,
                    TopK = topK,
                    ScoreThreshold = scoreThreshold
                };
                var rawDocs = await _retrievalService.RetrieveAsync(retrievalOptions);

                // 2. Post-process the documents
                var postProcessingOptions = new RAGPostProcessingOptions
                {
                    TopN = topK,
                    ScoreThreshold = scoreThreshold
                };
                var finalDocs = await _dataPostProcessor.ProcessAsync(query, rawDocs, postProcessingOptions);

                // 3. Format the final result
                var retreivalResult = await FormatRetrievalResultAsync(finalDocs);

                return result.SetSuccessResult(retreivalResult);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "RetrieveContextAsync:EXCEPTION",
                    ex.Message
                );
            }
        }

        private async Task<RAGRetrievalResultModel> FormatRetrievalResultAsync(List<RAGRetrievalDocumentModal> finalDocs)
        {
            if (finalDocs == null || !finalDocs.Any())
            {
                return new RAGRetrievalResultModel();
            }

            var contextBuilder = new StringBuilder();
            var sources = new List<RAGRetrievalSourceModel>();

            // Efficiently fetch parent document names
            var documentIds = finalDocs
                .Select(d => d.Metadata.TryGetValue("DocumentId", out var idObj) && idObj is long id ? id : -1)
                .Where(id => id != -1)
                .Distinct()
                .ToList();

            var parentDocs = await _documentRepository.GetDocumentsByIdsAsync(documentIds);
            var parentDocsMap = parentDocs?.ToDictionary(d => d.Id) ?? new Dictionary<long, BusinessAppKnowledgeBaseDocument>();

            foreach (var doc in finalDocs)
            {
                contextBuilder.AppendLine(doc.PageContent).AppendLine();

                doc.Metadata.TryGetValue("DocumentId", out var docIdObj);
                doc.Metadata.TryGetValue("ChunkId", out var chunkIdObj);
                doc.Metadata.TryGetValue("Score", out var scoreObj);

                long docId = docIdObj is long id ? id : 0;

                sources.Add(new RAGRetrievalSourceModel
                {
                    DocumentId = docId,
                    DocumentName = parentDocsMap.TryGetValue(docId, out var parentDoc) ? parentDoc.Name : "Unknown",
                    ChunkId = chunkIdObj?.ToString() ?? string.Empty,
                    Content = doc.PageContent,
                    Score = scoreObj is float score ? score : 0.0f,
                });
            }

            return new RAGRetrievalResultModel
            {
                ContextString = contextBuilder.ToString().Trim(),
                Sources = sources
            };
        }

        // Helper methods to extract common config values
        private int GetTopK(BusinessAppKnowledgeBaseConfigurationRetrieval config) => config switch
        {
            BusinessAppKnowledgeBaseConfigurationVectorRetrieval c => c.TopK,
            BusinessAppKnowledgeBaseConfigurationFullTextRetrieval c => c.TopK,
            BusinessAppKnowledgeBaseConfigurationHybridRetrieval c => c.TopK,
            _ => 3
        };

        private double? GetScoreThreshold(BusinessAppKnowledgeBaseConfigurationRetrieval config) => config switch
        {
            BusinessAppKnowledgeBaseConfigurationVectorRetrieval c when c.UseScoreThreshold => c.ScoreThreshold,
            BusinessAppKnowledgeBaseConfigurationHybridRetrieval c when c.UseScoreThreshold => c.ScoreThreshold,
            _ => null
        };

        public async ValueTask DisposeAsync()
        {
            await _retrievalService.DisposeAsync();
            await _dataPostProcessor.DisposeAsync();

            GC.SuppressFinalize(this);
        }
    }
}
