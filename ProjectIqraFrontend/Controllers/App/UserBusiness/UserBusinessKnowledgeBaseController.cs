using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.RAG.Retrieval;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.KnowledgeBase.Retrieval;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Driver;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessKnowledgeBaseController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;
        private readonly KnowledgeBaseRetrievalManagerFactory _knowledgeBaseRetrievalManagerFactory;

        public UserBusinessKnowledgeBaseController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager,
            KnowledgeBaseRetrievalManagerFactory knowledgeBaseRetrievalManagerFactory
        )
        {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
            _knowledgeBaseRetrievalManagerFactory = knowledgeBaseRetrievalManagerFactory;
        }

        [HttpPost("/app/user/business/{businessId}/knowledgebase/save")]
        public async Task<FunctionReturnResult<BusinessAppKnowledgeBase?>> SaveKnowledgeBase(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppKnowledgeBase?>();

            try
            {
                // Check New or Edit
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveKnowledgeBase:INVALID_POST_TYPE",
                        "Invalid post type specified. Can only be 'new' or 'edit'."
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveKnowledgeBase:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                BusinessAppKnowledgeBase? existingKbData = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingKnowledgeBaseId", out StringValues existingKbIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveKnowledgeBase:MISSING_EXISTING_KKNOWLEDGEBASE_ID",
                            "Existing Knowledge Base ID is required for edit mode."
                        );
                    }
                    string? existingKbId = existingKbIdValue.ToString();
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
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveKnowledgeBase:EXCEPTION",
                    $"Exception saving knowledge base: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/knowledgebase/{knowledgeBaseId}/delete")]
        public async Task<FunctionReturnResult> DeleteKnowledgeBase(long businessId, string knowledgeBaseId)
        {
            var result = new FunctionReturnResult();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = BusinessModulePermissionType.Deleting,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteKnowledgeBase:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var knowledgeBaseData = await _businessManager.GetKnowledgeBaseManager().GetKnowledgeBaseById(businessId, knowledgeBaseId);
                if (!knowledgeBaseData.Success || knowledgeBaseData.Data == null)
                {
                    return result.SetFailureResult(
                        $"DeleteKnowledgeBase:{knowledgeBaseData.Code}",
                        knowledgeBaseData.Message
                    );
                }

                var deleteResult = await _businessManager.GetKnowledgeBaseManager().DeleteKnowledgeBase(businessId, knowledgeBaseData.Data);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteKnowledgeBase:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteKnowledgeBase:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/knowledgebase/{knowledgeBaseId}/documents/upload")]
        public async Task<FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>> UploadKnowledgeBaseDocument(long businessId, string knowledgeBaseId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = BusinessModulePermissionType.Editing,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.Documents",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.Documents",
                            Type = BusinessModulePermissionType.Adding,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"UploadKnowledgeBaseDocument:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
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
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "UploadKnowledgeBaseDocument:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("/app/user/business/{businessId}/knowledgebase/{kbId}/documents")]
        public async Task<FunctionReturnResult<List<BusinessAppKnowledgeBaseDocument>?>> GetKnowledgebaseDocuments(long businessId, string kbId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<BusinessAppKnowledgeBaseDocument>?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                        Type = BusinessModulePermissionType.Retrieving,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "KnowledgeBases.Documents",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "KnowledgeBases.Documents",
                        Type = BusinessModulePermissionType.Retrieving,
                    },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetKnowledgebaseDocuments:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
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
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetKnowledgebaseDocuments:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/knowledgebase/{knowledgeBaseId}/retrieve")]
        public async Task<FunctionReturnResult<RAGRetrievalResultModel?>> RetrieveFromKnowledgeBase(long businessId, string knowledgeBaseId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<RAGRetrievalResultModel?>();

            KnowledgeBaseRetrievalManager? knowledgeBaseRetrievalManager = null;
            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = BusinessModulePermissionType.Retrieving,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.Documents",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.Documents",
                            Type = BusinessModulePermissionType.Retrieving,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"RetrieveFromKnowledgeBase:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

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
                    $"IQRAPPTESTRETRIEVAL{ObjectId.GenerateNewId().ToString()}",
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
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.KnowledgeBasePermissions",
                            Type = BusinessModulePermissionType.Editing,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.Documents",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "KnowledgeBases.Documents",
                            Type = BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveDocumentChunks:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                // Delegate to Manager
                var forwardResult = await _businessManager.GetKnowledgeBaseManager().UpdateKnowledgeBaseDocumentChunksAsync(businessId, knowledgeBaseId, documentId, formData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveDocumentChunks:{forwardResult.Code}",
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
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
