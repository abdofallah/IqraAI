using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Helpers;
using IqraCore.Models.RAG.Retrieval;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.KnowledgeBase.Retrieval;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class AppUserBusinessKnowledgeBaseController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly BusinessManager _businessManager;
        private readonly KnowledgeBaseRetrievalManagerFactory _knowledgeBaseRetrievalManagerFactory;

        private readonly IMongoClient _mongoClient;

        public AppUserBusinessKnowledgeBaseController(
            UserSessionValidationHelper userSessionValidationHelper,
            BusinessManager businessManager,
            KnowledgeBaseRetrievalManagerFactory knowledgeBaseRetrievalManagerFactory,
            IMongoClient mongoClient
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _businessManager = businessManager;
            _knowledgeBaseRetrievalManagerFactory = knowledgeBaseRetrievalManagerFactory;
            _mongoClient = mongoClient;
        }

        [HttpPost("/app/user/business/{businessId}/knowledgebase/save")]
        public async Task<FunctionReturnResult<BusinessAppKnowledgeBase?>> SaveKnowledgeBase(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppKnowledgeBase?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkBusinessesDisabled: true,
                checkBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                result.Code = $"SaveKnowledgeBase:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data.userData;
            var businessData = userSessionAndBusinessValidationResult.Data.businessData;

            // Knowledge Base Permission
            if (businessData.Permission.KnowledgeBases.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "SaveKnowledgeBase:KNOWLEDGE_BASES_DISABLED",
                    $"Knowledge Bases are disabled for this business: {businessData.Permission.KnowledgeBases.DisabledFullReason}"
                );
            }

            // Check New or Edit
            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                return result.SetFailureResult(
                    "SaveKnowledgeBase:INVALID_POST_TYPE",
                    "Invalid post type specified. Can only be 'new' or 'edit'."
                );
            }

            string? existingKbId = null;
            bool exisitingKbResult = formData.TryGetValue("existingKnowledgeBaseId", out StringValues existingKbIdValue);
            if (postType == "edit")
            {
                if (!exisitingKbResult || string.IsNullOrWhiteSpace(existingKbIdValue.ToString()))
                {
                    return result.SetFailureResult(
                        "SaveKnowledgeBase:MISSING_EXISTING_KKNOWLEDGEBASE_ID",
                        "Existing Knowledge Base ID is required for edit mode."
                    );
                }
                else
                {
                    existingKbId = existingKbIdValue.ToString();
                }
            }

            BusinessAppKnowledgeBase? existingKbData = null;
            if (postType == "new")
            {
                if (businessData.Permission.KnowledgeBases.DisabledAddingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveKnowledgeBase:ADDING_KNOWLEDGE_BASES_DISABLED",
                        $"Permission to add knowledge bases is disabled for this business: {businessData.Permission.KnowledgeBases.DisabledAddingReason}"
                    );
                }
            }
            else if (postType == "edit")
            {
                if (businessData.Permission.KnowledgeBases.DisabledEditingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveKnowledgeBase:EDITING_KNOWLEDGE_BASES_DISABLED",
                        $"Permission to edit knowledge bases is disabled for this business: {businessData.Permission.KnowledgeBases.DisabledEditingReason}"
                    );
                }

                if (string.IsNullOrWhiteSpace(existingKbId))
                {
                    return result.SetFailureResult(
                        "SaveKnowledgeBase:INVALID_EXISTING_KKNOWLEDGEBASE_ID",
                        "Existing Knowledge Base ID is required for edit mode but is invalid."
                    );
                }

                var getKnowledgeBaseResult = await _businessManager.GetKnowledgeBaseManager().GetKnowledgeBaseById(businessId, existingKbId);
                if (!getKnowledgeBaseResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveKnowledgeBase:GET_KNOWLEDGE_BASE_FAILED",
                        $"Failed to get existing knowledge base: {getKnowledgeBaseResult.Message}"
                    );
                }
                existingKbData = getKnowledgeBaseResult.Data;
            }

            // Delegate to Manager
            var addOrUpdateResult = await _businessManager.GetKnowledgeBaseManager().AddOrUpdateKnowledgeBaseAsync(businessId, formData, postType, existingKbData);
            if (!addOrUpdateResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveKnowledgeBase:{addOrUpdateResult.Code}",
                    addOrUpdateResult.Message
                );
            }

            return result.SetSuccessResult(addOrUpdateResult.Data);
        }

        [HttpPost("/app/user/business/{businessId}/knowledgebase/{knowledgeBaseId}/documents/upload")]
        public async Task<FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>> UploadKnowledgeBaseDocument(long businessId, string knowledgeBaseId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkBusinessesDisabled: true,
                checkBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                result.Code = $"UploadKnowledgeBaseDocument:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data.userData;
            var businessData = userSessionAndBusinessValidationResult.Data.businessData;

            // Knowledge Base Permission
            if (businessData.Permission.KnowledgeBases.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "UploadKnowledgeBaseDocument:KNOWLEDGE_BASES_DISABLED",
                    $"Knowledge Bases are disabled for this business: {businessData.Permission.KnowledgeBases.DisabledFullReason}"
                );
            }

            if (businessData.Permission.KnowledgeBases.DisabledEditingAt != null)
            {
                return result.SetFailureResult(
                    "UploadKnowledgeBaseDocument:EDITING_KNOWLEDGE_BASES_DISABLED",
                    $"Permission to edit knowledge bases is disabled for this business: {businessData.Permission.KnowledgeBases.DisabledEditingReason}"
                );
            }

            if (businessData.Permission.KnowledgeBases.Documents.DisabledFullReason != null)
            {
                return result.SetFailureResult(
                    "UploadKnowledgeBaseDocument:KNOWLEDGE_BASES_DOCUMENTS_DISABLED",
                    $"Documents are disabled for knowledge base for this business: {businessData.Permission.KnowledgeBases.Documents.DisabledFullReason}"
                );
            }

            if (businessData.Permission.KnowledgeBases.Documents.DisabledAddingAt != null)
            {
                return result.SetFailureResult(
                    "UploadKnowledgeBaseDocument:ADDING_KNOWLEDGE_BASES_DOCUMENTS_DISABLED",
                    $"Permission to add documents is disabled for knowledge base for this business: {businessData.Permission.KnowledgeBases.Documents.DisabledAddingReason}"
                );
            }

            // Logic
            var file = formData.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                return result.SetFailureResult(
                    "UploadKnowledgeBaseDocument:NO_FILE",
                    "No file was provided for upload."
                );
            }
            if (file.Length > 15 * 1024 * 1024) // 15 MB
            {
                return result.SetFailureResult(
                    "UploadKnowledgeBaseDocument:FILE_TOO_LARGE",
                    "File is too large. Maximum file size is 15 MB."
                );
            }

            // Delegate to Manager
            var addAndProcessResult = await _businessManager.GetKnowledgeBaseManager().ProcessAndAddDocumentAsync(businessId, knowledgeBaseId, formData, file);
            if (!addAndProcessResult.Success)
            {
                return result.SetFailureResult(
                    $"UploadKnowledgeBaseDocument:{addAndProcessResult.Code}",
                    addAndProcessResult.Message
                );
            }

            return result.SetSuccessResult(addAndProcessResult.Data);
        }

        [HttpGet("/app/user/business/{businessId}/knowledgebase/{kbId}/documents")]
        public async Task<FunctionReturnResult<List<BusinessAppKnowledgeBaseDocument>?>> GetKnowledgebaseDocuments(long businessId, string kbId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<BusinessAppKnowledgeBaseDocument>?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkBusinessesDisabled: true,
                checkBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                result.Code = $"GetKnowledgebaseDocuments:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data.userData;
            var businessData = userSessionAndBusinessValidationResult.Data.businessData;

            // Knowledge Base Permission
            if (businessData.Permission.KnowledgeBases.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "GetKnowledgebaseDocuments:KNOWLEDGE_BASES_DISABLED",
                    $"Knowledge Bases are disabled for this business: {businessData.Permission.KnowledgeBases.DisabledFullReason}"
                );
            }

            if (businessData.Permission.KnowledgeBases.Documents.DisabledFullReason != null)
            {
                return result.SetFailureResult(
                    "GetKnowledgebaseDocuments:KNOWLEDGE_BASES_DOCUMENTS_DISABLED",
                    $"Documents are disabled for knowledge base for this business: {businessData.Permission.KnowledgeBases.Documents.DisabledFullReason}"
                );
            }
  
            // Delegate to Manager
            var addOrUpdateResult = await _businessManager.GetKnowledgeBaseManager().GetKnowledgeBaseDocuments(businessId, kbId);
            if (!addOrUpdateResult.Success)
            {
                return result.SetFailureResult(
                    $"GetKnowledgebaseDocuments:{addOrUpdateResult.Code}",
                    addOrUpdateResult.Message
                );
            }

            return result.SetSuccessResult(addOrUpdateResult.Data);
        }

        [HttpPost("/app/user/business/{businessId}/knowledgebase/{knowledgeBaseId}/retrieve")]
        public async Task<FunctionReturnResult<RAGRetrievalResultModel?>> RetrieveFromKnowledgeBase(long businessId, string knowledgeBaseId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<RAGRetrievalResultModel?>();

            KnowledgeBaseRetrievalManager? knowledgeBaseRetrievalManager = null;
            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                    Request,
                    businessId,
                    checkUserDisabled: true,
                    checkBusinessesDisabled: true
                );

                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"RetrieveFromKnowledgeBase:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message);
                }
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Knowledge Base Permission
                if (businessData.Permission.KnowledgeBases.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "RetrieveFromKnowledgeBase:KNOWLEDGE_BASES_DISABLED",
                        $"Knowledge Bases are disabled for this business: {(string.IsNullOrEmpty(businessData.Permission.KnowledgeBases.DisabledFullReason) ? "." : businessData.Permission.KnowledgeBases.DisabledFullReason)}");
                }
                if (businessData.Permission.KnowledgeBases.DisabledRetrievingAt != null)
                {
                    return result.SetFailureResult(
                        "RetrieveFromKnowledgeBase:RETRIEVING_KNOWLEDGE_BASES_DISABLED",
                        $"Permission to retrieve knowledge bases results is disabled for this business{(string.IsNullOrEmpty(businessData.Permission.KnowledgeBases.DisabledRetrievingReason) ? "." : ": " + businessData.Permission.KnowledgeBases.DisabledRetrievingReason)}"
                    );
                }

                string? retrievalQuery = null;
                if (!formData.TryGetValue("query", out var queryValues))
                {
                    return result.SetFailureResult(
                        "RetrieveFromKnowledgeBase:QUERY_NOT_FOUND",
                        "Query not found in request."
                    );
                }
                retrievalQuery = queryValues.FirstOrDefault();
                if (string.IsNullOrEmpty(retrievalQuery))
                {
                    return result.SetFailureResult(
                        "RetrieveFromKnowledgeBase:QUERY_INVALID",
                        "Query is empty or null."
                    );
                }

                // Create Retrieval Manager
                var knowledgeBaseRetrievalManagerCreateResult = await _knowledgeBaseRetrievalManagerFactory.CreateManagerAsync(
                    businessId,
                    knowledgeBaseId,
                    $"IQRAPPTESTRETRIEVAL{Guid.NewGuid().ToString()}",
                    TimeSpan.FromMinutes(5)
                );
                if (!knowledgeBaseRetrievalManagerCreateResult.Success)
                {
                    return result.SetFailureResult(
                        $"RetrieveFromKnowledgeBase:{knowledgeBaseRetrievalManagerCreateResult.Code}",
                        knowledgeBaseRetrievalManagerCreateResult.Message
                    );
                }
                knowledgeBaseRetrievalManager = knowledgeBaseRetrievalManagerCreateResult.Data!;

                // Delegate to Manager
                // TODO GET CACHE SETTINGS FROM FRONTEND
                var retrievalResult = await knowledgeBaseRetrievalManager.RetrieveContextAsync(businessId, knowledgeBaseId, retrievalQuery.ToLower(), true, businessData.DefaultLanguage, "TEST_RETRIVAL_UI");
                if (retrievalResult == null)
                {
                    return result.SetFailureResult(
                        "RetrieveFromKnowledgeBase:RETRIEVAL_FAILED",
                        "Failed to retrieve context from the knowledge base. The knowledge base might not exist.");
                }

                // Return Result
                return result.SetSuccessResult(retrievalResult.Data);
            }
            catch (Exception ex) {
                return result.SetFailureResult(
                    "RetrieveFromKnowledgeBase:EXCEPTION",
                    ex.Message
                );
            }
            finally
            {
                if (knowledgeBaseRetrievalManager != null)
                {
                    await knowledgeBaseRetrievalManager.DisposeAsync();
                }
            }
        }

        [HttpPost("/app/user/business/{businessId}/knowledgebase/{knowledgeBaseId}/documents/{documentId}/chunks/save")]
        public async Task<FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>> SaveDocumentChunks(long businessId, string knowledgeBaseId, long documentId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserAndBusinessSessionAsync(
                    Request,
                    businessId,
                    checkUserDisabled: true,
                    checkBusinessesDisabled: true,
                    checkBusinessesEditingEnabled: true
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    result.Code = $"SaveDocumentChunks:{userSessionAndBusinessValidationResult.Code}";
                    result.Message = userSessionAndBusinessValidationResult.Message;
                    return result;
                }
                var userData = userSessionAndBusinessValidationResult.Data.userData;
                var businessData = userSessionAndBusinessValidationResult.Data.businessData;

                // Knowledge Base Permission
                if (businessData.Permission.KnowledgeBases.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "SaveDocumentChunks:KNOWLEDGE_BASES_DISABLED",
                        $"Knowledge Bases are disabled for this business: {businessData.Permission.KnowledgeBases.DisabledFullReason}"
                    );
                }
                if (businessData.Permission.KnowledgeBases.Documents.DisabledFullReason != null)
                {
                    return result.SetFailureResult(
                        "SaveDocumentChunks:KNOWLEDGE_BASES_DOCUMENTS_DISABLED",
                        $"Documents are disabled for knowledge base for this business: {businessData.Permission.KnowledgeBases.Documents.DisabledFullReason}"
                    );
                }
                if (businessData.Permission.KnowledgeBases.Documents.DisabledEditingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveDocumentChunks:KNOWLEDGE_BASES_DOCUMENTS_DISABLED_EDITING",
                        $"Documents editing is disabled for knowledge base for this business: {businessData.Permission.KnowledgeBases.Documents.DisabledEditingReason}"
                    );
                }

                // Delegate to Manager
                try
                {
                    using (var mongoSession = _mongoClient.StartSession())
                    {
                        mongoSession.StartTransaction();

                        var forwardResult = await _businessManager.GetKnowledgeBaseManager()
                            .UpdateKnowledgeBaseDocumentChunksAsync(businessId, knowledgeBaseId, documentId, formData, mongoSession);
                        if (!forwardResult.Success)
                        {
                            await mongoSession.AbortTransactionAsync();
                            return result.SetFailureResult(
                                $"SaveDocumentChunks:{forwardResult.Code}",
                                forwardResult.Message
                            );
                        }

                        return result.SetSuccessResult(forwardResult.Data);                  
                    }
                }
                catch (Exception ex)
                {
                    return result.SetFailureResult(
                        "SaveDocumentChunks:MONGO_SESSION_EXCEPTION",
                        "Exception occured during mongo session."
                    );
                }
            }
            catch (Exception ex) {
                return result.SetFailureResult(
                    "SaveDocumentChunks:EXCEPTION",
                    ex.Message
                );
            }
        }
    }
}
