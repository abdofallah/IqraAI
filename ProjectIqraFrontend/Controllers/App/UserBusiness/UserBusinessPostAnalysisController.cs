using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessPostAnalysisController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly BusinessManager _businessManager;

        public UserBusinessPostAnalysisController(
            UserSessionValidationHelper userSessionValidationHelper,
            BusinessManager businessManager
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/postanalysis/save")]
        public async Task<FunctionReturnResult<BusinessAppPostAnalysis?>> SavePostAnalysisTemplate(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppPostAnalysis?>();

            // Validate Session & Business
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                Request, businessId,
                checkUserDisabled: true,
                checkUserBusinessesDisabled: true,
                checkUserBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"SavePostAnalysisTemplate:{userSessionAndBusinessValidationResult.Code}",
                    userSessionAndBusinessValidationResult.Message
                );
            }
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            // Check Top-Level Permission
            if (businessData.Permission.PostAnalysis.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "SavePostAnalysisTemplate:POST_ANALYSIS_DISABLED",
                    $"Post Analysis features are disabled for this business{(string.IsNullOrEmpty(businessData.Permission.PostAnalysis.DisabledFullReason) ? "." : $": {businessData.Permission.PostAnalysis.DisabledFullReason}.")}"
                );
            }

            // Basic Form Validation
            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
            {
                return result.SetFailureResult(
                    "SavePostAnalysisTemplate:INVALID_POST_TYPE",
                    "Invalid post type specified. Can only be 'new' or 'edit'."
                );
            }

            string? existingTemplateId = null;
            BusinessAppPostAnalysis? existingTemplateData = null;

            if (postType == "new")
            {
                if (businessData.Permission.PostAnalysis.DisabledAddingAt != null)
                {
                    return result.SetFailureResult(
                        "SavePostAnalysisTemplate:ADDING_DISABLED",
                        "Permission to add new templates is disabled."
                    );
                }
            }
            else // postType is "edit"
            {
                if (businessData.Permission.PostAnalysis.DisabledEditingAt != null)
                {
                    return result.SetFailureResult(
                        "SavePostAnalysisTemplate:EDITING_DISABLED",
                        "Permission to edit templates is disabled."
                    );
                }

                formData.TryGetValue("existingTemplateId", out StringValues existingIdValue);
                existingTemplateId = existingIdValue.ToString();

                if (string.IsNullOrWhiteSpace(existingTemplateId))
                {
                    return result.SetFailureResult(
                        "SavePostAnalysisTemplate:MISSING_TEMPLATE_ID",
                        "Existing Template ID is required for edit mode."
                    );
                }

                var getTemplateResult = await _businessManager.GetPostAnalysisManager().GetTemplateById(businessId, existingTemplateId);
                if (!getTemplateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SavePostAnalysisTemplate:{getTemplateResult.Code}",
                        getTemplateResult.Message
                    );
                }
                existingTemplateData = getTemplateResult.Data;
            }

            // Delegate to Manager for Business Logic
            var addOrUpdateResult = await _businessManager.GetPostAnalysisManager().AddOrUpdateTemplateAsync(businessId, formData, postType, existingTemplateData);
            if (!addOrUpdateResult.Success)
            {
                return result.SetFailureResult(
                    $"SavePostAnalysisTemplate:{addOrUpdateResult.Code}",
                    addOrUpdateResult.Message
                );
            }

            return result.SetSuccessResult(addOrUpdateResult.Data);
        }

        [HttpPost("/app/user/business/{businessId}/postanalysis/{templateId}/delete")]
        public async Task<FunctionReturnResult> DeletePostAnalysisTemplate(long businessId, string templateId)
        {
            var result = new FunctionReturnResult();

            try
            {
                // Validate Session & Business
                var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                    Request, businessId,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeletePostAnalysisTemplate:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Check Top-Level Permission
                if (businessData.Permission.PostAnalysis.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "DeletePostAnalysisTemplate:POST_ANALYSIS_DISABLED",
                        $"Post Analysis features are disabled for this business{(string.IsNullOrEmpty(businessData.Permission.PostAnalysis.DisabledFullReason) ? "." : $": {businessData.Permission.PostAnalysis.DisabledFullReason}.")}"
                    );
                }

                // Check deleting permission
                if (businessData.Permission.PostAnalysis.DisabledDeletingAt != null)
                {
                    return result.SetFailureResult(
                        "DeletePostAnalysisTemplate:DELETING_DISABLED",
                        $"Permission to delete templates is disabled{(string.IsNullOrEmpty(businessData.Permission.PostAnalysis.DisabledDeletingReason) ? "." : $": {businessData.Permission.PostAnalysis.DisabledDeletingReason}.")}"
                    );
                }

                var postAnalysisData = await _businessManager.GetPostAnalysisManager().GetTemplateById(businessId, templateId);
                if (!postAnalysisData.Success || postAnalysisData.Data == null)
                {
                    return result.SetFailureResult(
                        "DeletePostAnalysisTemplate:TEMPLATE_NOT_FOUND",
                        "Template not found."
                    );
                }

                var deleteResult = await _businessManager.GetPostAnalysisManager().DeleteTemplate(businessId, postAnalysisData.Data);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeletePostAnalysisTemplate:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeletePostAnalysisTemplate:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}
