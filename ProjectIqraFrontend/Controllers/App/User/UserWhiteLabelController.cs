using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Entities.User.WhiteLabel;
using IqraCore.Entities.User.WhiteLabel.Business.Enum;
using IqraCore.Entities.User.WhiteLabel.Domain;
using IqraCore.Entities.User.WhiteLabel.Plan;
using IqraCore.Models.User.GetMasterUserDataModel.WhiteLabel.Domain;
using IqraCore.Models.User.GetMasterUserDataModel.WhiteLabel.Plan;
using IqraCore.Requests.User.WhiteLabel;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserWhiteLabelController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserWhiteLabelManager _userWhiteLabelManager;

        public UserWhiteLabelController(
            UserSessionValidationHelper userSessionValidationHelper,
            UserWhiteLabelManager userWhiteLabelManager
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _userWhiteLabelManager = userWhiteLabelManager;
        }

        [HttpPost("/app/user/whitelabel/activate")]
        public async Task<FunctionReturnResult> ActivateUserWhiteLabel()
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled: true);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"ActivateUserWhiteLabel:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "ActivateUserWhiteLabel:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                var forwardResult = await _userWhiteLabelManager.ActivateUserWhiteLabel(userData.Email);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        $"ActivateUserWhiteLabel:{forwardResult.Code}",
                        forwardResult.Message
                    );
                }
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ActivateUserWhiteLabel:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/whitelabel/settings/save")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<FunctionReturnResult<UserWhiteLabelBrandingData?>> SavePlatformSettings([FromForm] SaveUserWhiteLabelPlatformSettingsRequest request)
        {
            var result = new FunctionReturnResult<UserWhiteLabelBrandingData?>();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SavePlatformSettings:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "SavePlatformSettings:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "SavePlatformSettings:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                var saveResult = await _userWhiteLabelManager.SavePlatformSettings(userData, request);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        $"SavePlatformSettings:{saveResult.Code}",
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SavePlatformSettings:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/whitelabel/domains/save")]
        public async Task<FunctionReturnResult<UserWhiteLabelDomainDataModel?>> SaveDomain([FromForm] SaveUserWhiteLabelDomainRequest request)
        {
            var result = new FunctionReturnResult<UserWhiteLabelDomainDataModel?>();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveDomain:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "SaveDomain:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "SaveDomain:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                if (string.IsNullOrWhiteSpace(request.PostType) ||
                    (request.PostType != "edit" && request.PostType != "new")
                )
                {
                    return result.SetFailureResult(
                        "SaveDomain:INVALID_POST_TYPE",
                        "Invalid post type."
                    );
                }

                UserWhiteLabelDomainData? exisitingDomainData = userData.WhiteLabel.Domains.Find(d => d.CustomDomain == request.Config.CustomDomain);
                if (request.PostType == "edit")
                {
                    if (string.IsNullOrEmpty(request.Config.CustomDomain))
                    {
                        return result.SetFailureResult(
                            "SaveDomain:INVALID_CUSTOM_DOMAIN",
                            "Invalid custom domain."
                        );
                    }

                    if (exisitingDomainData == null)
                    {
                        return result.SetFailureResult(
                            "SaveDomain:CUSTOM_DOMAIN_NOT_FOUND",
                            "Custom domain not found."
                        );
                    }
                }
                else if (request.PostType == "new")
                {
                    if (exisitingDomainData != null)
                    {
                        return result.SetFailureResult(
                            "SaveDomain:CUSTOM_DOMAIN_EXISTS",
                            "Custom domain already exists."
                        );
                    }
                }

                var saveDomainResult = await _userWhiteLabelManager.SaveDomain(userData.Email, request.PostType, request.Config, request.OverrideLogo, request.OverrideIcon, exisitingDomainData);
                if (!saveDomainResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveDomain:{saveDomainResult.Code}",
                        saveDomainResult.Message
                    );
                }

                var domainDataModel = new UserWhiteLabelDomainDataModel(saveDomainResult.Data!);

                return result.SetSuccessResult(domainDataModel);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("SaveDomain:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("/app/user/whitelabel/domains/delete")]
        public async Task<FunctionReturnResult> DeleteDomain([FromBody] DeleteUserWhiteLabelDomainRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult($"DeleteDomain:{validationResult.Code}", validationResult.Message);
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "DeleteDomain:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "DeleteDomain:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                // TODO: Call _userWhiteLabelManager.DeleteDomain(...)
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("DeleteDomain:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("/app/user/whitelabel/plans/save")]
        public async Task<FunctionReturnResult<UserWhiteLabelPlanDataModel?>> SavePlan([FromBody] SaveWhiteLabelPlanRequest request)
        {
            var result = new FunctionReturnResult<UserWhiteLabelPlanDataModel?>();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SavePlan:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "SavePlan:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "SavePlan:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                if (string.IsNullOrWhiteSpace(request.PostType) || 
                    (request.PostType != "edit" && request.PostType != "new")
                ) {
                    return result.SetFailureResult(
                        "SavePlan:INVALID_POST_TYPE",
                        "Invalid post type."
                    );
                }

                UserWhiteLabelPlanData? exisitingPlanData = null;
                if (request.PostType == "edit")
                {
                    if (string.IsNullOrEmpty(request.ExistingPlanId))
                    {
                        return result.SetFailureResult(
                            "SavePlan:INVALID_PLAN_ID",
                            "Invalid plan ID."
                        );
                    }

                    exisitingPlanData = userData.WhiteLabel.Plans.Find(p => p.Id == request.ExistingPlanId);
                    if (exisitingPlanData == null)
                    {
                        return result.SetFailureResult(
                            "SavePlan:PLAN_NOT_FOUND",
                            "Plan not found."
                        );
                    }
                }

                var savePlanResult = await _userWhiteLabelManager.SavePlan(userData.Email, request.PostType, request.Config, exisitingPlanData);
                if (!savePlanResult.Success)
                {
                    return result.SetFailureResult(
                        $"SavePlan:{savePlanResult.Code}",
                        savePlanResult.Message
                    );
                }

                var newPlanModel = new UserWhiteLabelPlanDataModel(savePlanResult.Data!);
                return result.SetSuccessResult(newPlanModel);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SavePlan:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/whitelabel/plans/archive")]
        public async Task<FunctionReturnResult> ArchivePlan([FromBody] ArchiveUserWhiteLabelPlanRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult($"ArchivePlan:{validationResult.Code}", validationResult.Message);
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "ArchivePlan:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "ArchivePlan:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                if (string.IsNullOrEmpty(request.PlanId))
                {
                    return result.SetFailureResult(
                        "ArchivePlan:INVALID_PLAN_ID",
                        "Invalid plan ID."
                    );
                }

                var archivePlanResult = await _userWhiteLabelManager.ArchivePlan(userData, request.PlanId, request.IsArchived);
                if (!archivePlanResult.Success)
                {
                    return result.SetFailureResult(
                        $"ArchivePlan:{archivePlanResult.Code}",
                        archivePlanResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("ArchivePlan:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("/app/user/whitelabel/businesses/onboard")]
        public async Task<FunctionReturnResult> OnboardBusiness([FromBody] OnboardUserWhiteLabelBusinessRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"OnboardBusiness:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "OnboardBusiness:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "OnboardBusiness:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                if (!userData.Businesses.Contains(request.BusinessId))
                {
                    return result.SetFailureResult(
                        "OnboardBusiness:USER_BUSINESS_NOT_FOUND",
                        "User business not found"
                    );
                }

                var oboardResult = await _userWhiteLabelManager.OnboardBusiness(userData, request.BusinessId);
                if (!oboardResult.Success)
                {
                    return result.SetFailureResult(
                        $"OnboardBusiness:{oboardResult.Code}",
                        oboardResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("OnboardBusiness:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("/app/user/whitelabel/businesses/status")]
        public async Task<FunctionReturnResult> SetBusinessStatus([FromBody] SetStatusUserWhiteLabelBusinessRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SetBusinessStatus:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "SetBusinessStatus:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "SetBusinessStatus:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                if (!userData.Businesses.Contains(request.BusinessId))
                {
                    return result.SetFailureResult(
                        "SetBusinessStatus:USER_BUSINESS_NOT_FOUND",
                        "User business not found"
                    );
                }

                var activateResult = await _userWhiteLabelManager.SetBusinessStatus(userData, request.BusinessId, request.IsActive);
                if (!activateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SetBusinessStatus:{activateResult.Code}",
                        activateResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SetBusinessStatus:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/whitelabel/businesses/plan")]
        public async Task<FunctionReturnResult> SetBusinessPlan([FromBody] SetPlanUserWhiteLabelBusinessRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SetBusinessPlan:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "SetBusinessPlan:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "SetBusinessPlan:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                if (!userData.Businesses.Contains(request.BusinessId))
                {
                    return result.SetFailureResult(
                        "SetBusinessPlan:USER_BUSINESS_NOT_FOUND",
                        "User business not found"
                    );
                }

                var updatePlanResult = await _userWhiteLabelManager.SetBusinessPlan(userData, request.BusinessId, request.PlanId);
                if (!updatePlanResult.Success)
                {
                    return result.SetFailureResult(
                        $"SetBusinessPlan:{updatePlanResult.Code}",
                        updatePlanResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SetBusinessPlan:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/whitelabel/businesses/save")]
        public async Task<FunctionReturnResult> SaveBusiness([FromBody] SaveUserWhiteLabelBusinessRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult($"SaveBusiness:{validationResult.Code}", validationResult.Message);
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusiness:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "SaveBusiness:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                // TODO: Call _userWhiteLabelManager.SaveBusiness(...)
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("SaveBusiness:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("/app/user/whitelabel/business/users/save")]
        public async Task<FunctionReturnResult> SaveBusinessUser([FromBody] SaveUserWhiteLabelBusinessUserRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult($"SaveBusinessUser:{validationResult.Code}", validationResult.Message);
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessUser:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessUser:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                // TODO: Call _userWhiteLabelManager.SaveBusinessUser(...)
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("SaveBusinessUser:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("/app/user/whitelabel/business/users/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessUser([FromBody] DeleteUserWhiteLabelBusinessUserRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult($"DeleteBusinessUser:{validationResult.Code}", validationResult.Message);
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "DeleteBusinessUser:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "DeleteBusinessUser:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                // TODO: Call _userWhiteLabelManager.DeleteBusinessUser(...)
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("DeleteBusinessUser:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("/app/user/whitelabel/business/billing/adjust-balance")]
        public async Task<FunctionReturnResult> AdjustBusinessBalance([FromBody] AdjustUserWhiteLabelBusinessBalanceRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult($"AdjustBusinessBalance:{validationResult.Code}", validationResult.Message);
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "AdjustBusinessBalance:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "AdjustBusinessBalance:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                // TODO: Call _userWhiteLabelManager.AdjustBusinessBalance(...)
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("AdjustBusinessBalance:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("/app/user/whitelabel/business/billing/update-subscription")]
        public async Task<FunctionReturnResult> UpdateBusinessSubscription([FromBody] UpdateUserWhiteLabelBusinessSubscriptionRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult($"UpdateBusinessSubscription:{validationResult.Code}", validationResult.Message);
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "UpdateBusinessSubscription:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "UpdateBusinessSubscription:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                // TODO: Call _userWhiteLabelManager.UpdateBusinessSubscription(...)
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("UpdateBusinessSubscription:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("/app/user/whitelabel/overview")]
        public async Task<FunctionReturnResult> FetchWhiteLabelOverview([FromBody] FetchUserWhiteLabelOverviewRequest request)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult($"FetchWhiteLabelOverview:{validationResult.Code}", validationResult.Message);
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "FetchWhiteLabelOverview:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                if (userData.WhiteLabel.ActivatedAt == null)
                {
                    return result.SetFailureResult(
                        "FetchWhiteLabelOverview:USER_WHITELABEL_INACTIVE",
                        "White label is inactive for the user"
                    );
                }

                // TODO: Call _userWhiteLabelManager.GetOverviewData(...)
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("FetchWhiteLabelOverview:EXCEPTION", $"Internal server error: {ex.Message}");
            }
        }
    }
}