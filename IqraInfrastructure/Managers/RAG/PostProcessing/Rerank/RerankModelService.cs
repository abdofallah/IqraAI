using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.RAG;
using IqraCore.Models.RAG.Retrieval;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Rerank;

namespace IqraInfrastructure.Managers.RAG.PostProcessing.Rerank
{
    public class RerankModelService : IReranker
    {
        private readonly BusinessManager _businessManager;
        private readonly RerankProviderManager _rerankProviderManager;

        private IRerankService _rerankService;

        public RerankModelService(BusinessManager businessManager, RerankProviderManager rerankProviderManager)
        {
            _businessManager = businessManager;
            _rerankProviderManager = rerankProviderManager;
        }

        public async Task<FunctionReturnResult> Initalize(BusinessAppKnowledgeBaseConfigurationRetrieval retrivalConfig, long? businessId = null)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (!businessId.HasValue)
                {
                    return result.SetFailureResult(
                        "Initalize:BUSINESS_ID_MISSING",
                        "Business Id is missing."
                    );
                }

                var rerankIntegrationData = GetRerankIntegrationDataFromRetrivalConfig(retrivalConfig);
                if (rerankIntegrationData == null)
                {
                    return result.SetFailureResult(
                        "Initalize:RERANK_INTEGRATION_DATA_MISSING",
                        "Rerank integration data is missing."
                    );
                }

                var rerankIntegrationResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(businessId.Value, rerankIntegrationData.Id);
                if (!rerankIntegrationResult.Success)
                {
                    return result.SetFailureResult(
                        "Initalize:RERANK_INTEGRATION_NOT_FOUND",
                        "Rerank integration not found."
                    );
                }

                var rerankServiceResult = await _rerankProviderManager.BuildProviderServiceByIntegration
                    (
                        rerankIntegrationResult.Data,
                        rerankIntegrationData,
                        new Dictionary<string, string>()
                    );
                if (!rerankServiceResult.Success)
                {
                    return result.SetFailureResult(
                        $"Initalize:{rerankServiceResult.Code}",
                        rerankServiceResult.Message
                    );
                }
                _rerankService = rerankServiceResult.Data!;

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

        public async Task<List<RAGRetrievalDocumentModal>> RerankAsync(string query, List<RAGRetrievalDocumentModal> documents)
        {
            var textsToRerank = documents.Select(d => d.PageContent).ToList();
            var rerankResult = await _rerankService.RerankAsync(query, textsToRerank, documents.Count);

            if (!rerankResult.Success || rerankResult.RerankedDocuments == null)
            {
                // Log error but proceed without reranking
                return documents;
            }

            var rerankedDocs = new List<RAGRetrievalDocumentModal>();
            foreach (var rerankedDoc in rerankResult.RerankedDocuments)
            {
                var originalDoc = documents[rerankedDoc.OriginalIndex];
                originalDoc.Metadata["Score"] = rerankedDoc.RelevanceScore;
                rerankedDocs.Add(originalDoc);
            }

            return rerankedDocs;
        }

        private BusinessAppAgentIntegrationData? GetRerankIntegrationDataFromRetrivalConfig(BusinessAppKnowledgeBaseConfigurationRetrieval retrivalConfig)
        {
            return retrivalConfig switch
            {
                BusinessAppKnowledgeBaseConfigurationVectorRetrieval vec => vec.Rerank.Integration,
                BusinessAppKnowledgeBaseConfigurationFullTextRetrieval ft => ft.Rerank.Integration,
                BusinessAppKnowledgeBaseConfigurationHybridRetrieval hyb => hyb.RerankIntegration,
                _ => null
            };
        }
    }
}
