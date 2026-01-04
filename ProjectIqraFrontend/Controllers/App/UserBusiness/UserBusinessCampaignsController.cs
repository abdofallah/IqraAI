using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessCampaignsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessCampaignsController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        )
        {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
        }

        /*
         * 
         * Telephony Campaigns
         * 
        **/

        [HttpPost("/app/user/business/{businessId}/campaign/telephony/save")]
        public async Task<FunctionReturnResult<BusinessAppTelephonyCampaign?>> SaveBusinessTelephonyCampaign(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppTelephonyCampaign?>();

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
                        "SaveBusinessTelephonyCampaign:INVALID_POST_TYPE",
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
                            ModulePath = "TelephonyCampaigns",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "TelephonyCampaigns",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessTelephonyCampaign:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                BusinessAppTelephonyCampaign? existingTelephonyCampaignData = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingTelephonyCampaignId", out StringValues existingTelephonyCampaignIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessTelephonyCampaign:MISSING_EXISTING_TELEPHONY_CAMPAIGN_ID",
                            "Existing Telephony Campaign ID is required for edit mode."
                        );
                    }
                    string? existingTelephonyCampaignId = existingTelephonyCampaignIdValue.ToString();
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
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessTelephonyCampaign:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
        
        [HttpPost("/app/user/business/{businessId}/campaign/telephony/{telephonyCampaignId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessTelephonyCampaign(long businessId, string telephonyCampaignId)
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
                            ModulePath = "TelephonyCampaigns",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "TelephonyCampaigns",
                            Type = BusinessModulePermissionType.Deleting,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessTelephonyCampaign:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var campaignData = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(businessId, telephonyCampaignId);
                if (!campaignData.Success || campaignData.Data == null)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessTelephonyCampaign:${campaignData.Code}",
                        campaignData.Message
                    );
                }

                var deleteResult = await _businessManager.GetCampaignManager().DeleteTelephonyCampaign(businessId, campaignData.Data);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessTelephonyCampaign:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteBusinessTelephonyCampaign:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        /*
         * 
         * Web Campaigns
         * 
        **/ 

        [HttpPost("/app/user/business/{businessId}/campaign/web/save")]
        public async Task<FunctionReturnResult<BusinessAppWebCampaign?>> SaveBusinessWebCampaign(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppWebCampaign?>();

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
                        "SaveBusinessWebCampaign:INVALID_POST_TYPE",
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
                            ModulePath = "WebCampaigns",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "WebCampaigns",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessWebCampaign:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                BusinessAppWebCampaign? existingWebCampaignData = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingWebCampaignId", out StringValues existingWebCampaignIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessWebCampaign:MISSING_EXISTING_Web_CAMPAIGN_ID",
                            "Existing Web Campaign ID is required for edit mode."
                        );
                    }
                    string? existingWebCampaignId = existingWebCampaignIdValue.ToString();
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
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessWebCampaign:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/campaign/web/{webCampaignId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessWebCampaign(long businessId, string webCampaignId)
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
                            ModulePath = "WebCampaigns",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "WebCampaigns",
                            Type = BusinessModulePermissionType.Deleting,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessWebCampaign:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var campaignData = await _businessManager.GetCampaignManager().GetWebCampaignById(businessId, webCampaignId);
                if (!campaignData.Success || campaignData.Data == null)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessWebCampaign:{campaignData.Code}",
                        campaignData.Message
                    );
                }

                var deleteResult = await _businessManager.GetCampaignManager().DeleteWebCampaign(businessId, campaignData.Data);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessWebCampaign:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex) {
                return result.SetFailureResult(
                    "DeleteBusinessWebCampaign:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}
