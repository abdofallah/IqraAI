using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraCore.Models.RAG.Retrieval;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.Embedding.Helpers;
using IqraInfrastructure.Managers.KnowledgeBase;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.RAG.PostProcessing;
using IqraInfrastructure.Managers.RAG.Retrieval;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.RAG;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    /// <summary>
    /// Manages all Retrieval-Augmented Generation (RAG) operations for the AI agent during a conversation.
    /// It orchestrates retrieval from multiple knowledge bases, query refinement, post-processing, and context consolidation.
    /// </summary>
    public class ConversationAIAgentRAGManager : IAsyncDisposable
    {
        #region Dependencies & Private Fields
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationAIAgentRAGManager> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly BusinessManager _businessManager;
        private readonly KnowledgeBaseVectorRepository _vectorRepository;
        private readonly RAGKeywordStore _keywordStore;
        private readonly EmbeddingProviderManager _embeddingManager;
        private readonly BusinessKnowledgeBaseDocumentRepository _documentRepository;
        private readonly KnowledgeBaseCollectionsLoadManager _collectionsLoadManager;
        private readonly EmbeddingCacheManager _embeddingCacheManager;
        private readonly RerankProviderManager _rerankProviderManager;
        private readonly LLMProviderManager _llmProviderManager;

        private readonly string _conversationSessionId;
        private readonly TimeSpan _collectionReleaseExpiry = TimeSpan.FromHours(1);

        private bool _isInitialized = false;
        private readonly Dictionary<string, KnowledgeBaseContextSource> _contextSources = new();

        private BusinessAppAgentKnowledgeBase _ragConfig;
        private List<BusinessAppKnowledgeBase> _knowledgeBaseGroupsData;

        private ILLMService? _classifierLlmService;
        private ILLMService? _refinementLlmService;

        // Efficient lookup for manually cached queries. Key: Query Text, Value: Cache Info
        private Dictionary<string, (string GroupId, string EntryId)> _manualCacheLookup = new();
        #endregion

        public ConversationAIAgentRAGManager(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            BusinessManager businessManager,
            KnowledgeBaseVectorRepository vectorRepository,
            RAGKeywordStore keywordStore,
            EmbeddingProviderManager embeddingManager,
            BusinessKnowledgeBaseDocumentRepository documentRepository,
            KnowledgeBaseCollectionsLoadManager collectionsLoadManager,
            EmbeddingCacheManager embeddingCacheManager,
            RerankProviderManager rerankProviderManager,
            LLMProviderManager llmProviderManager,
            string conversationSessionId
        )
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationAIAgentRAGManager>();
            _agentState = agentState;
            _businessManager = businessManager;
            _vectorRepository = vectorRepository;
            _keywordStore = keywordStore;
            _embeddingManager = embeddingManager;
            _documentRepository = documentRepository;
            _collectionsLoadManager = collectionsLoadManager;
            _embeddingCacheManager = embeddingCacheManager;
            _rerankProviderManager = rerankProviderManager;
            _llmProviderManager = llmProviderManager;
            _conversationSessionId = conversationSessionId;
        }

        /// <summary>
        /// Initializes the RAG manager, pre-loading configurations and preparing retrieval services.
        /// </summary>
        public async Task<FunctionReturnResult> InitializeAsync(CancellationToken cancellationToken)
        {
            var result = new FunctionReturnResult();
            _ragConfig = _agentState.BusinessAppAgent.KnowledgeBase;

            if (_ragConfig.LinkedGroups == null || !_ragConfig.LinkedGroups.Any())
            {
                _isInitialized = true;
                return result.SetSuccessResult();
            }

            try
            {
                var businessId = _agentState.BusinessApp.Id;
                _knowledgeBaseGroupsData = _agentState.BusinessApp.KnowledgeBases.FindAll(
                    kb => _ragConfig.LinkedGroups.Contains(kb.Id)
                );

                if (_knowledgeBaseGroupsData == null || !_knowledgeBaseGroupsData.Any())
                {
                    _isInitialized = true;
                    return result.SetSuccessResult();
                }

                BuildManualCacheLookup();

                if (_ragConfig.SearchStrategy.Type == AgentKnowledgeBaseSearchStartegyTypeENUM.LLM)
                {
                    await InitializeClassifierLlmAsync(cancellationToken);
                }

                foreach (var kbData in _knowledgeBaseGroupsData)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var retrievalService = new RAGRetrievalService(
                        _loggerFactory.CreateLogger<RAGRetrievalService>(),
                        _businessManager,
                        _vectorRepository,
                        _keywordStore,
                        _embeddingManager,
                        _documentRepository,
                        _collectionsLoadManager,
                        _embeddingCacheManager,
                        _conversationSessionId,
                        _collectionReleaseExpiry
                    );
                    var postProcessor = new RAGDataPostProcessor(
                        _businessManager,
                        _rerankProviderManager
                    );
                    var contextSource = new KnowledgeBaseContextSource(
                        kbData,
                        retrievalService,
                        postProcessor,
                        _loggerFactory.CreateLogger<KnowledgeBaseContextSource>()
                    );

                    var initResult = await contextSource.Initialize(businessId);
                    if (initResult.Success)
                    {
                        _contextSources.Add(kbData.Id, contextSource);
                    }
                    else
                    {
                        _logger.LogError(
                            "Failed to initialize context source for KB {KBId}: {Message}",
                            kbData.Id,
                            initResult.Message
                        );
                    }
                }

                if (_ragConfig.Refinement.Enabled)
                {
                    await InitializeRefinementLlmAsync(cancellationToken);
                }

                _isInitialized = true;
                return result.SetSuccessResult();
            }
            catch (OperationCanceledException)
            {
                return result.SetFailureResult(
                    "Initialize:Cancelled",
                    "Initialization was cancelled."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during RAG Manager initialization.");
                return result.SetFailureResult("Initialize:Exception", ex.Message);
            }
        }

        /// <summary>
        /// Orchestrates the full RAG pipeline for a given user query.
        /// </summary>
        public async Task<string?> RetrieveContextForQueryAsync(string query, CancellationToken cancellationToken)
        {
            if (!_isInitialized || !_contextSources.Any())
            {
                return null;
            }

            try
            {
                if (!await ShouldPerformSearchAsync(query, cancellationToken))
                {
                    _logger.LogTrace(
                        "Search strategy determined no search is needed for query: '{Query}'",
                        query
                    );
                    return null;
                }

                var queriesToProcess = _ragConfig.Refinement.Enabled
                    ? await RefineQueryAsync(query, cancellationToken)
                    : new List<string> { query };

                var retrievalTasks = new List<Task<List<RAGRetrievalDocumentModal>>>();
                var agentCacheConfig = _agentState.BusinessAppAgent.Cache;
                bool useCache = agentCacheConfig.Embeddings != null && agentCacheConfig.Embeddings.Any();

                foreach (var q in queriesToProcess)
                {
                    foreach (var source in _contextSources.Values)
                    {
                        var retrievalConfig = source.KnowledgeBaseData.Configuration.Retrieval;
                        var retrievalOptions = new RAGRetrievalOptions
                        {
                            Query = q,
                            TopK = GetTopK(retrievalConfig),
                            ScoreThreshold = GetScoreThreshold(retrievalConfig)
                        };

                        if (useCache)
                        {
                            // Efficiently check for a manual cache hit first
                            if (_manualCacheLookup.TryGetValue(q, out var cacheInfo))
                            {
                                var embeddingService = source.RetrievalService.GetEmbeddingService();
                                if (embeddingService != null)
                                {
                                    retrievalOptions.IsCachable = true;
                                    retrievalOptions.CacheGroupId = cacheInfo.GroupId;
                                    retrievalOptions.CacheGroupEntryId = cacheInfo.EntryId;
                                    retrievalOptions.CacheGroupLanguage = _agentState.CurrentLanguageCode;
                                    retrievalOptions.CacheReference = _conversationSessionId;
                                    retrievalOptions.CacheKey = EmbeddingCacheKeyGenerator.Generate(
                                        q, embeddingService.GetProviderType(), embeddingService.GetCacheableConfig()
                                    );
                                }
                            }
                            // If no manual hit, check for auto-caching on miss
                            else if (agentCacheConfig.EmbeddingsCacheSettings.AutoCacheEmbeddingResponses)
                            {
                                var embeddingService = source.RetrievalService.GetEmbeddingService();
                                if (embeddingService != null)
                                {
                                    retrievalOptions.IsCachable = true;
                                    retrievalOptions.CacheGroupId = agentCacheConfig.EmbeddingsCacheSettings.AutoCacheEmbeddingResponseCacheGroupId;
                                    retrievalOptions.CacheGroupLanguage = _agentState.CurrentLanguageCode;
                                    retrievalOptions.CacheReference = _conversationSessionId;
                                    retrievalOptions.CacheKey = EmbeddingCacheKeyGenerator.Generate(
                                        q, embeddingService.GetProviderType(), embeddingService.GetCacheableConfig()
                                    );
                                }
                            }
                        }
                        retrievalTasks.Add(source.RetrievalService.RetrieveAsync(retrievalOptions));
                    }
                }

                var retrievalResults = await Task.WhenAll(retrievalTasks);
                var allDocs = retrievalResults.SelectMany(list => list).ToList();

                var uniqueDocs = allDocs
                    .GroupBy(doc => doc.Metadata["ChunkId"].ToString())
                    .Select(
                        group => group.OrderByDescending(d => (float)d.Metadata["Score"]).First()
                    )
                    .ToList();

                if (!uniqueDocs.Any())
                    return null;

                var postProcessingTasks = new List<Task<List<RAGRetrievalDocumentModal>>>();
                var docsByKb = uniqueDocs
                    .GroupBy(doc => doc.Metadata["KnowledgeBaseId"].ToString())
                    .Where(g => !string.IsNullOrEmpty(g.Key));

                foreach (var group in docsByKb)
                {
                    if (_contextSources.TryGetValue(group.Key, out var source))
                    {
                        var options = new RAGPostProcessingOptions
                        {
                            TopN = GetTopK(source.KnowledgeBaseData.Configuration.Retrieval)
                        };
                        postProcessingTasks.Add(
                            source.PostProcessor.ProcessAsync(query, group.ToList(), options)
                        );
                    }
                }

                var postProcessedResults = await Task.WhenAll(postProcessingTasks);
                var finalDocs = postProcessedResults
                    .SelectMany(list => list)
                    .OrderByDescending(d => (float)d.Metadata["Score"])
                    .ToList();

                return FormatContext(finalDocs);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RAG context retrieval was cancelled.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error during RAG context retrieval for query: '{Query}'",
                    query
                );
                return null;
            }
        }

        #region Private Helper Methods

        private async Task InitializeClassifierLlmAsync(CancellationToken cancellationToken)
        {
            var llmConfig = _ragConfig.SearchStrategy.LLMClassifier?.LLMIntegration;
            _classifierLlmService = await BuildLlmServiceAsync(
                llmConfig,
                "You are a search query classifier. Analyze the user's query and the conversation history. Decide if a search in a knowledge base is likely to yield a relevant answer. Respond with ONLY the word 'SEARCH' or 'SKIP'.",
                cancellationToken
            );
            if (_classifierLlmService == null)
            {
                _ragConfig.SearchStrategy.Type = AgentKnowledgeBaseSearchStartegyTypeENUM.Always; // Fallback
            }
        }

        private async Task InitializeRefinementLlmAsync(CancellationToken cancellationToken)
        {
            var llmConfig = _ragConfig.Refinement.LLMIntegration;
            _refinementLlmService = await BuildLlmServiceAsync(
                llmConfig,
                "You are a query refinement expert. Rewrite a user's query into multiple, effective search queries. Generate a JSON array of strings. Do not include any other text or explanation.",
                cancellationToken
            );
            if (_refinementLlmService == null)
            {
                _ragConfig.Refinement.Enabled = false; // Fallback
            }
        }

        private async Task<ILLMService?> BuildLlmServiceAsync(BusinessAppAgentIntegrationData? llmConfig, string systemPrompt, CancellationToken cancellationToken)
        {
            if (llmConfig == null)
                return null;

            var integrationDataResult = await _businessManager
                .GetIntegrationsManager()
                .getBusinessIntegrationById(_agentState.BusinessApp.Id, llmConfig.Id);
            if (!integrationDataResult.Success || integrationDataResult.Data == null)
            {
                _logger.LogError("Could not find business integration with ID {IntegrationId} for RAG LLM task.", llmConfig.Id);
                return null;
            }

            var llmResult = await _llmProviderManager.BuildProviderServiceByIntegration(
                integrationDataResult.Data,
                llmConfig,
                new Dictionary<string, string>()
            );
            if (!llmResult.Success || llmResult.Data == null)
            {
                _logger.LogError("Failed to build LLM service for RAG task: {Message}", llmResult.Message);
                return null;
            }

            llmResult.Data.SetSystemPrompt(systemPrompt);
            return llmResult.Data;
        }

        private void BuildManualCacheLookup()
        {
            var embeddingCacheGroups = _agentState.BusinessApp.Cache.EmbeddingGroups.Where(
                g => _agentState.BusinessAppAgent.Cache.Embeddings.Contains(g.Id)
            );

            _manualCacheLookup = new Dictionary<string, (string GroupId, string EntryId)>(
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var group in embeddingCacheGroups)
            {
                // We only care about the agent's current language for this conversation
                if (group.Embeddings.TryGetValue(_agentState.CurrentLanguageCode, out var entries))
                {
                    foreach (var entry in entries)
                    {
                        _manualCacheLookup[entry.Query] = (group.Id, entry.Id);
                    }
                }
            }
        }

        private async Task<bool> ShouldPerformSearchAsync(
            string query,
            CancellationToken cancellationToken
        )
        {
            switch (_ragConfig.SearchStrategy.Type)
            {
                case AgentKnowledgeBaseSearchStartegyTypeENUM.Always:
                    return true;

                case AgentKnowledgeBaseSearchStartegyTypeENUM.SpecificKeyword:
                    {
                        var keywords = _ragConfig.SearchStrategy.SpecificKeywords;
                        return keywords != null
                            && keywords.Any(k => query.Contains(k, StringComparison.OrdinalIgnoreCase));
                    }

                case AgentKnowledgeBaseSearchStartegyTypeENUM.KnowledgeBaseKeyword:
                    {
                        if (!_contextSources.Any())
                            return false;
                        foreach (var kbId in _contextSources.Keys)
                        {
                            if ((await _keywordStore.SearchAsync(kbId, query, 1)).Any())
                                return true;
                        }
                        return false;
                    }

                case AgentKnowledgeBaseSearchStartegyTypeENUM.LLM:
                    {
                        // TODO

                        //if (_classifierLlmService == null)
                        //    return false;
                        //var response = await _classifierLlmService.ProcessSingleInputAsync(
                        //    $"User Query: \"{query}\"",
                        //    cancellationToken
                        //);
                        //return response.Success && response.Data?.Trim().ToUpper() == "SEARCH";

                        return false;
                    }

                default:
                    return false;
            }
        }

        private async Task<List<string>> RefineQueryAsync(
            string originalQuery,
            CancellationToken cancellationToken
        )
        {
            if (_refinementLlmService == null)
                return new List<string> { originalQuery };
            try
            {
                // TODO

                //int queryCount = _ragConfig.Refinement.QueryCount.Value;
                //var prompt = $"Original query: \"{originalQuery}\". Generate {queryCount} refined queries as a JSON array of strings:";

                //var response = await _refinementLlmService.ProcessSingleInputAsync(prompt, cancellationToken);
                //if (response.Success && !string.IsNullOrWhiteSpace(response.Data))
                //{
                //    var refinedQueries = JsonSerializer.Deserialize<List<string>>(response.Data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                //    if (refinedQueries != null && refinedQueries.Any())
                //    {
                //        refinedQueries.Insert(0, originalQuery);
                //        return refinedQueries.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                //    }
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during query refinement. Using original query only.");
            }
            return new List<string> { originalQuery };
        }

        private string FormatContext(List<RAGRetrievalDocumentModal> documents)
        {
            if (documents == null || !documents.Any())
                return string.Empty;

            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("--- Relevant Information Found ---");
            for (int i = 0; i < documents.Count; i++)
            {
                contextBuilder.AppendLine($"[Source {i + 1}]: {documents[i].PageContent}");
            }
            contextBuilder.Append("---");
            return contextBuilder.ToString();
        }

        private int GetTopK(BusinessAppKnowledgeBaseConfigurationRetrieval config) =>
            config switch
            {
                BusinessAppKnowledgeBaseConfigurationVectorRetrieval c => c.TopK,
                BusinessAppKnowledgeBaseConfigurationFullTextRetrieval c => c.TopK,
                BusinessAppKnowledgeBaseConfigurationHybridRetrieval c => c.TopK,
                _ => 3
            };

        private double? GetScoreThreshold(BusinessAppKnowledgeBaseConfigurationRetrieval config) =>
            config switch
            {
                BusinessAppKnowledgeBaseConfigurationVectorRetrieval c when c.UseScoreThreshold => c.ScoreThreshold,
                BusinessAppKnowledgeBaseConfigurationHybridRetrieval c when c.UseScoreThreshold => c.ScoreThreshold,
                _ => null
            };

        #endregion

        public async ValueTask DisposeAsync()
        {
            foreach (var source in _contextSources.Values)
            {
                await source.DisposeAsync();
            }
            (_classifierLlmService as IDisposable)?.Dispose();
            (_refinementLlmService as IDisposable)?.Dispose();
        }

        private class KnowledgeBaseContextSource : IAsyncDisposable
        {
            public BusinessAppKnowledgeBase KnowledgeBaseData { get; }
            public RAGRetrievalService RetrievalService { get; }
            public RAGDataPostProcessor PostProcessor { get; }
            private readonly ILogger _logger;

            public KnowledgeBaseContextSource(
                BusinessAppKnowledgeBase kbData,
                RAGRetrievalService retrievalService,
                RAGDataPostProcessor postProcessor,
                ILogger logger
            )
            {
                KnowledgeBaseData = kbData;
                RetrievalService = retrievalService;
                PostProcessor = postProcessor;
                _logger = logger;
            }

            public async Task<FunctionReturnResult> Initialize(long businessId)
            {
                var retrievalInitResult = await RetrievalService.Initalize(businessId, KnowledgeBaseData);
                if (!retrievalInitResult.Success)
                {
                    return retrievalInitResult;
                }
                return await PostProcessor.Initalize(businessId, KnowledgeBaseData);
            }

            public async ValueTask DisposeAsync()
            {
                await RetrievalService.DisposeAsync();
                await PostProcessor.DisposeAsync();
            }
        }
    }
}
