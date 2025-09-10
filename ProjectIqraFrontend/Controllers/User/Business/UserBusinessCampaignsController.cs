using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Campaign;
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

        [HttpPost("/app/user/business/{businessId}/campaign/telephony/save")]
        public async Task<FunctionReturnResult<BusinessAppTelephonyCampaign?>> SaveBusinessTelephonyCampaign(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppTelephonyCampaign?>();

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
                result.Code = $"SaveBusinessTelephonyCampaign:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data.userData;
            var businessData = userSessionAndBusinessValidationResult.Data.businessData;

            // Campaigns Permission
            if (businessData.Permission.Campaigns.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "SaveBusinessTelephonyCampaign:CAMPAIGNS_DISABLED",
                    $"Campaigns are disabled for this business{(string.IsNullOrEmpty(businessData.Permission.Campaigns.DisabledFullReason) ? "." : $": {businessData.Permission.Campaigns.DisabledFullReason}.")}"
                );
            }

            // Check New or Edit
            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                return result.SetFailureResult(
                    "SaveBusinessTelephonyCampaign:INVALID_POST_TYPE",
                    "Invalid post type specified. Can only be 'new' or 'edit'."
                );
            }

            string? existingTelephonyCampaignId = null;
            bool exisitingCampaignIdResult = formData.TryGetValue("existingTelephonyCampaignId", out StringValues existingTelephonyCampaignIdValue);
            if (postType == "edit")
            {
                if (!exisitingCampaignIdResult || string.IsNullOrWhiteSpace(existingTelephonyCampaignIdValue.ToString()))
                {
                    return result.SetFailureResult(
                        "SaveBusinessTelephonyCampaign:MISSING_EXISTING_TELEPHONY_CAMPAIGN_ID",
                        "Existing Telephony Campaign ID is required for edit mode."
                    );
                }
                else
                {
                    existingTelephonyCampaignId = existingTelephonyCampaignIdValue.ToString();
                }
            }

            BusinessAppTelephonyCampaign? existingTelephonyCampaignData = null;
            if (postType == "new")
            {
                if (businessData.Permission.Campaigns.DisabledAddingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessTelephonyCampaign:ADDING_CAMPAIGNS_DISABLED",
                        $"Permission to add campaign is disabled for this business{(string.IsNullOrEmpty(businessData.Permission.Campaigns.DisabledAddingReason) ? "." : $": {businessData.Permission.Campaigns.DisabledAddingReason}.")}"
                    );
                }
            }
            else if (postType == "edit")
            {
                if (businessData.Permission.Campaigns.DisabledEditingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessTelephonyCampaign:EDITING_CAMPAIGNS_DISABLED",
                        $"Permission to edit campaign is disabled for this business{(string.IsNullOrEmpty(businessData.Permission.Campaigns.DisabledEditingReason) ? "." : $": {businessData.Permission.Campaigns.DisabledEditingReason}.")}"
                    );
                }

                if (string.IsNullOrWhiteSpace(existingTelephonyCampaignId))
                {
                    return result.SetFailureResult(
                        "SaveBusinessTelephonyCampaign:INVALID_EXISTING_TELEPHONY_CAMPAIGN_ID",
                        "Existing Telephony Campaign ID is required for edit mode but is invalid."
                    );
                }

                var getTelephonyCampaignResult = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(businessId, existingTelephonyCampaignId);
                if (!getTelephonyCampaignResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessTelephonyCampaign:{getTelephonyCampaignResult.Code}",
                        getTelephonyCampaignResult.Message
                    );
                }
                existingTelephonyCampaignData = getTelephonyCampaignResult.Data;
            }

            // Delegate to Manager
            var addOrUpdateResult = await _businessManager.GetCampaignManager().AddOrUpdateTelephonyCampaignAsync(businessId, formData, postType, existingTelephonyCampaignData);
            if (!addOrUpdateResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessTelephonyCampaign:{addOrUpdateResult.Code}",
                    addOrUpdateResult.Message
                );
            }

            return result.SetSuccessResult(addOrUpdateResult.Data);
        }

        [HttpPost("/app/user/business/{businessId}/campaign/web/save")]
        public async Task<FunctionReturnResult<BusinessAppWebCampaign?>> SaveBusinessWebCampaign(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppWebCampaign?>();

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
                result.Code = $"SaveBusinessWebCampaign:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data.userData;
            var businessData = userSessionAndBusinessValidationResult.Data.businessData;

            // Campaigns Permission
            if (businessData.Permission.Campaigns.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "SaveBusinessWebCampaign:CAMPAIGNS_DISABLED",
                    $"Campaigns are disabled for this business{(string.IsNullOrEmpty(businessData.Permission.Campaigns.DisabledFullReason) ? "." : $": {businessData.Permission.Campaigns.DisabledFullReason}.")}"
                );
            }

            // Check New or Edit
            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                return result.SetFailureResult(
                    "SaveBusinessWebCampaign:INVALID_POST_TYPE",
                    "Invalid post type specified. Can only be 'new' or 'edit'."
                );
            }

            string? existingWebCampaignId = null;
            bool exisitingCampaignIdResult = formData.TryGetValue("existingWebCampaignId", out StringValues existingWebCampaignIdValue);
            if (postType == "edit")
            {
                if (!exisitingCampaignIdResult || string.IsNullOrWhiteSpace(existingWebCampaignIdValue.ToString()))
                {
                    return result.SetFailureResult(
                        "SaveBusinessWebCampaign:MISSING_EXISTING_Web_CAMPAIGN_ID",
                        "Existing Web Campaign ID is required for edit mode."
                    );
                }
                else
                {
                    existingWebCampaignId = existingWebCampaignIdValue.ToString();
                }
            }

            BusinessAppWebCampaign? existingWebCampaignData = null;
            if (postType == "new")
            {
                if (businessData.Permission.Campaigns.DisabledAddingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessWebCampaign:ADDING_CAMPAIGNS_DISABLED",
                        $"Permission to add campaign is disabled for this business{(string.IsNullOrEmpty(businessData.Permission.Campaigns.DisabledAddingReason) ? "." : $": {businessData.Permission.Campaigns.DisabledAddingReason}.")}"
                    );
                }
            }
            else if (postType == "edit")
            {
                if (businessData.Permission.Campaigns.DisabledEditingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessWebCampaign:EDITING_CAMPAIGNS_DISABLED",
                        $"Permission to edit campaign is disabled for this business{(string.IsNullOrEmpty(businessData.Permission.Campaigns.DisabledEditingReason) ? "." : $": {businessData.Permission.Campaigns.DisabledEditingReason}.")}"
                    );
                }

                if (string.IsNullOrWhiteSpace(existingWebCampaignId))
                {
                    return result.SetFailureResult(
                        "SaveBusinessWebCampaign:INVALID_EXISTING_Web_CAMPAIGN_ID",
                        "Existing Web Campaign ID is required for edit mode but is invalid."
                    );
                }

                var getWebCampaignResult = await _businessManager.GetCampaignManager().GetWebCampaignById(businessId, existingWebCampaignId);
                if (!getWebCampaignResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessWebCampaign:{getWebCampaignResult.Code}",
                        getWebCampaignResult.Message
                    );
                }
                existingWebCampaignData = getWebCampaignResult.Data;
            }

            // Delegate to Manager
            var addOrUpdateResult = await _businessManager.GetCampaignManager().AddOrUpdateWebCampaignAsync(businessId, formData, postType, existingWebCampaignData);
            if (!addOrUpdateResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessWebCampaign:{addOrUpdateResult.Code}",
                    addOrUpdateResult.Message
                );
            }

            return result.SetSuccessResult(addOrUpdateResult.Data);
        }
    }
}
