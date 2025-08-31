using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class UserBusinessCampaignsController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly BusinessManager _businessManager;

        public UserBusinessCampaignsController(
            UserSessionValidationHelper userSessionValidationHelper,
            BusinessManager businessManager
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/campaign/save")]
        public async Task<FunctionReturnResult<BusinessAppCampaign?>> SaveBusinessCampaign(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppCampaign?>();

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
                result.Code = $"SaveBusinessCampaign:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data.userData;
            var businessData = userSessionAndBusinessValidationResult.Data.businessData;

            // Campaigns Permission
            if (businessData.Permission.Campaigns.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "SaveBusinessCampaign:CAMPAIGNS_DISABLED",
                    $"Campaigns are disabled for this business{(string.IsNullOrEmpty(businessData.Permission.Campaigns.DisabledFullReason) ? "." : $": {businessData.Permission.Campaigns.DisabledFullReason}.")}"
                );
            }

            // Check New or Edit
            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                return result.SetFailureResult(
                    "SaveBusinessCampaign:INVALID_POST_TYPE",
                    "Invalid post type specified. Can only be 'new' or 'edit'."
                );
            }

            string? existingCampaignId = null;
            bool exisitingCampaignIdResult = formData.TryGetValue("existingCampaignId", out StringValues existingCampaignIdValue);
            if (postType == "edit")
            {
                if (!exisitingCampaignIdResult || string.IsNullOrWhiteSpace(existingCampaignIdValue.ToString()))
                {
                    return result.SetFailureResult(
                        "SaveBusinessCampaign:MISSING_EXISTING_CAMPAIGN_ID",
                        "Existing Campaign ID is required for edit mode."
                    );
                }
                else
                {
                    existingCampaignId = existingCampaignIdValue.ToString();
                }
            }

            BusinessAppCampaign? existingCampaignData = null;
            if (postType == "new")
            {
                if (businessData.Permission.Campaigns.DisabledAddingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessCampaign:ADDING_CAMPAIGNS_DISABLED",
                        $"Permission to add campaign is disabled for this business{(string.IsNullOrEmpty(businessData.Permission.Campaigns.DisabledAddingReason) ? "." : $": {businessData.Permission.Campaigns.DisabledAddingReason}.")}"
                    );
                }
            }
            else if (postType == "edit")
            {
                if (businessData.Permission.Campaigns.DisabledEditingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessCampaign:EDITING_CAMPAIGNS_DISABLED",
                        $"Permission to edit campaign is disabled for this business{(string.IsNullOrEmpty(businessData.Permission.Campaigns.DisabledEditingReason) ? "." : $": {businessData.Permission.Campaigns.DisabledEditingReason}.")}"
                    );
                }

                if (string.IsNullOrWhiteSpace(existingCampaignId))
                {
                    return result.SetFailureResult(
                        "SaveBusinessCampaign:INVALID_EXISTING_CAMPAIGN_ID",
                        "Existing Campaign ID is required for edit mode but is invalid."
                    );
                }

                var getCampaignResult = await _businessManager.GetCampaignManager().GetCampaignById(businessId, existingCampaignId);
                if (!getCampaignResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessCampaign:{getCampaignResult.Code}",
                        getCampaignResult.Message
                    );
                }
                existingCampaignData = getCampaignResult.Data;
            }

            // Delegate to Manager
            var addOrUpdateResult = await _businessManager.GetCampaignManager().AddOrUpdateCampaignAsync(businessId, formData, postType, existingCampaignData);
            if (!addOrUpdateResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessCampaign:{addOrUpdateResult.Code}",
                    addOrUpdateResult.Message
                );
            }

            return result.SetSuccessResult(addOrUpdateResult.Data);
        }
    }
}
