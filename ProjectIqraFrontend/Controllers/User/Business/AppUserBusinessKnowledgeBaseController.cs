using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class AppUserBusinessKnowledgeBaseController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly BusinessManager _businessManager;

        public AppUserBusinessKnowledgeBaseController(UserSessionValidationHelper userSessionValidationHelper, BusinessManager businessManager)
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _businessManager = businessManager;
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
    }
}
