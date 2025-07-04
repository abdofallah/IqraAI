using IqraCore.Entities.Business;
using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.User;
using IqraCore.Models.User.GetMasterUserDataModel;
using IqraCore.Models.User.GetUserPlanDetailsModel;
using IqraCore.Utilities;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using Microsoft.AspNetCore.Mvc;
using PhoneNumbers;

namespace ProjectIqraFrontend.Controllers.User
{
    public class AppUserController : Controller
    {
        private readonly AppRepository _appRepository;
        private readonly UserManager _userManager;
        private readonly UserUsageManager _userUsageManager;
        private readonly PlanManager _planManager;
        private readonly BusinessManager _businessManager;
        private readonly LanguagesManager _languageManager;

        private static readonly PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();

        public AppUserController(AppRepository appRepository, UserManager userManager, UserUsageManager userUsageManager,  PlanManager planManager, BusinessManager businessManager, LanguagesManager languageManager)
        {
            _appRepository = appRepository;
            _userManager = userManager;
            _userUsageManager = userUsageManager;
            _planManager = planManager;
            _businessManager = businessManager;
            _languageManager = languageManager;
        }

        /**
         * 
         * User
         * 
        **/

        [HttpPost("/app/user")]
        public async Task<FunctionReturnResult<GetMasterUserDataModel?>> GetMasterUserDataModel()
        {
            var result = new FunctionReturnResult<GetMasterUserDataModel?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUser:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUser:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUser:3";
                result.Message = "User not found";
                return result;
            }

            GetMasterUserDataModel userDataModel = new GetMasterUserDataModel(user);

            return result.SetSuccessResult(userDataModel);
        }

        /**
         * 
         * User Plan Details
         * 
        **/ 

        [HttpPost("/app/user/plan")]
        public async Task<FunctionReturnResult<GetUserPlanDetailsModel?>> GetUserPlanDetailsModel()
        {
            var result = new FunctionReturnResult<GetUserPlanDetailsModel?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserPlanDetails:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserPlanDetails:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserPlanDetails:3";
                result.Message = "User not found";
                return result;
            }

            string userPlanId = "";
            if (user.Billing.Subscription == null)
            {
                var billingConfigData = await _appRepository.GetBillingPlanConfig();
                if (billingConfigData == null || string.IsNullOrWhiteSpace(billingConfigData.NewUserPlanId))
                {
                    result.Code = "GetUserPlanDetails:APP_BILLING_PLAN_NOT_FOUND";
                    result.Message = "App billing configuration not found";
                    return result;
                }

                userPlanId = billingConfigData.NewUserPlanId;
            }
            else
            {
                userPlanId = user.Billing.Subscription.PlanId;
            }

            var planData = await _planManager.GetPlanByIdAsync(userPlanId);
            if (!planData.Success)
            {
                result.Code = "GetUserPlanDetails:" + planData.Code;
                result.Message = planData.Message;
                return result;
            }

            GetUserPlanDetailsModel planDetailsModel = new GetUserPlanDetailsModel(planData.Data);

            return result.SetSuccessResult(planDetailsModel);
        }

        /**
         * 
         * User Usage
         * 
        **/

        [HttpPost("/app/user/usage/summary")]
        public async Task<FunctionReturnResult<GetUsageSummaryModel?>> GetUsageSummary([FromBody] GetUsageSummaryRequestModel request)
        {
            var result = new FunctionReturnResult<GetUsageSummaryModel?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUsageSummary:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUsageSummary:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUsageSummary:3";
                result.Message = "User not found";
                return result;
            }

            if (request == null)
            {
                result.Code = "GetUsageSummary:INVALID_REQUEST_DATA";
                result.Message = "Invalid request data";
                return result;
            }

            var usageSummaryResult = await _userUsageManager.GetUsageSummaryAsync(userEmail, request);
            if (!usageSummaryResult.Success)
            {
                result.Code = "GetUsageSummary:" + usageSummaryResult.Code;
                result.Message = usageSummaryResult.Message;
                return result;
            }

            return result.SetSuccessResult(usageSummaryResult.Data);
        }

        [HttpPost("/app/user/usage/history")]
        public async Task<FunctionReturnResult<PaginatedResult<MinuteUsageRecordModel>?>> GetUsageHistory([FromBody] GetUsageHistoryRequestModel request)
        {
            var result = new FunctionReturnResult<PaginatedResult<MinuteUsageRecordModel>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUsageHistory:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUsageHistory:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUsageHistory:3";
                result.Message = "User not found";
                return result;
            }

            var limit = Math.Clamp(request.Limit, 10, 50);
            var usaheHistoryResult = await _userUsageManager.GetUsageHistoryAsync(userEmail, limit, request.NextCursor, request.PreviousCursor);
            if (!usaheHistoryResult.Success)
            {
                result.Code = "GetUsageHistory:" + usaheHistoryResult.Code;
                result.Message = usaheHistoryResult.Message;
                return result;
            }

            return result.SetSuccessResult(usaheHistoryResult.Data);
        }

        /**
         * 
         * Business
         * 
        **/

        [HttpPost("/app/user/businesses")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> GetUserBusinesses()
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusinesses:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusinesses:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusinesses:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusinesses:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            FunctionReturnResult<List<BusinessData>?> businessesResult = await _businessManager.GetUserBusinessesByIds(user.Businesses, user.Email);
            if (!businessesResult.Success)
            {
                result.Code = "GetUserBusinesses:" + businessesResult.Code;
                result.Message = businessesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessesResult.Data;

            return result;
        }

        [HttpPost("/app/user/business/{businessId}/meta")]
        public async Task<FunctionReturnResult<BusinessData?>> GetUserBusinessMeta(long businessId)
        {
            var result = new FunctionReturnResult<BusinessData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusinessMeta:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusinessMeta:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusinessMeta:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusinessMeta:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetUserBusinessMeta:5";
                result.Message = "User does not own this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = "GetUserBusinessMeta:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetUserBusinessMeta:6";
                result.Message = "Business is disabled.";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += " Reason: " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            result.Success = true;
            result.Data = businessResult.Data;

            return result;
        }

        [HttpPost("/app/user/business/{businessId}/app")]
        public async Task<FunctionReturnResult<BusinessApp?>> GetUserBusinessApp(long businessId)
        {
            var result = new FunctionReturnResult<BusinessApp?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusinessApp:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusinessApp:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusinessApp:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusinessApp:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetUserBusinessApp:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = "GetUserBusinessApp:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetUserBusinessApp:6";
                result.Message = "Business is disabled.";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += " Reason: " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            FunctionReturnResult<BusinessApp?> businessAppResult = await _businessManager.GetUserBusinessAppById(businessId, user.Email);
            if (!businessAppResult.Success)
            {
                result.Code = "GetUserBusinessApp:" + businessAppResult.Code;
                result.Message = businessAppResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessAppResult.Data;

            return result;
        }

        [HttpPost("/app/user/business/{businessId}/whitelabeldomains")]
        public async Task<FunctionReturnResult<List<BusinessWhiteLabelDomain>?>> GetUserBusinessWhiteLabelDomains(long businessId)
        {
            var result = new FunctionReturnResult<List<BusinessWhiteLabelDomain>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:6";
                result.Message = "Business is disabled.";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += " Reason: " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            FunctionReturnResult<List<BusinessWhiteLabelDomain>?> businessWhiteLabelDomainResult = await _businessManager.GetSettingsManager().GetUserBusinessWhiteLabelDomainByIds(businessResult.Data.WhiteLabelDomainIds, businessId, user.Email);
            if (!businessWhiteLabelDomainResult.Success)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:" + businessWhiteLabelDomainResult.Code;
                result.Message = businessWhiteLabelDomainResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessWhiteLabelDomainResult.Data;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}")]
        public async Task<FunctionReturnResult<GetUserBusinessFullReturnModel?>> GetUserBusiness(long businessId)
        {
            var result = new FunctionReturnResult<GetUserBusinessFullReturnModel?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusiness:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusiness:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusiness:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusiness:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetUserBusiness:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = "GetUserBusiness:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetUserBusiness:6";
                result.Message = "Business is disabled.";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += " Reason: " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            FunctionReturnResult<BusinessApp?> businessAppResult = await _businessManager.GetUserBusinessAppById(businessId, user.Email);
            if (!businessAppResult.Success)
            {
                result.Code = "GetUserBusiness:" + businessAppResult.Code;
                result.Message = businessAppResult.Message;
                return result;
            }

            FunctionReturnResult<List<BusinessWhiteLabelDomain>?> businessWhiteLabelDomainResult = await _businessManager.GetSettingsManager().GetUserBusinessWhiteLabelDomainByIds(businessResult.Data.WhiteLabelDomainIds, businessId, user.Email);
            if (!businessWhiteLabelDomainResult.Success)
            {
                result.Code = "GetUserBusiness:" + businessWhiteLabelDomainResult.Code;
                result.Message = businessWhiteLabelDomainResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = new GetUserBusinessFullReturnModel()
            {
                BusinessData = businessResult.Data,
                BusinessApp = businessAppResult.Data,
                BusinessWhiteLabelDomain = businessWhiteLabelDomainResult.Data
            };

            return result;
        }

        [HttpPost("/app/user/business/add")]
        public async Task<FunctionReturnResult<BusinessData?>> AddUserBusiness([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "AddUserBusiness:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "AddUserBusiness:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "AddUserBusiness:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.AddBusinessDisabledAt != null)
            {
                result.Code = "AddUserBusiness:4";
                result.Message = "User does not have permission to add businesses";

                if (user.Permission.Business.DisableBusinessesAt != null && !string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                if (!string.IsNullOrEmpty(user.Permission.Business.AddBusinessDisableReason))
                {
                    result.Message += ": " + user.Permission.Business.AddBusinessDisableReason;
                }

                return result;
            }

            string? businessName = formData["BusinessName"];
            string? businessDefaultLanguage = formData["BusinessDefaultLanguage"];
            IFormFile? businessLogo = formData.Files.GetFile("BusinessLogo");

            if (string.IsNullOrWhiteSpace(businessName) || businessName.Length > 64)
            {
                result.Code = "AddUserBusiness:5";
                result.Message = "Invalid business name. Minimum length is 1 and maximum length is 64.";
                return result;
            }

            // Valdiate Langauge
            if (string.IsNullOrWhiteSpace(businessDefaultLanguage))
            {
                result.Code = "AddUserBusiness:6";
                result.Message = "Missing business default language.";
                return result;
            }
            var langaugeData = await _languageManager.GetLanguageByCode(businessDefaultLanguage);
            if (!langaugeData.Success)
            {
                result.Code = "AddUserBusiness:" + langaugeData.Code;
                result.Message = langaugeData.Message;
                return result;
            }
            if (langaugeData.Data.DisabledAt != null)
            {
                result.Code = "AddUserBusiness:7";
                result.Message = "Business default language is disabled.";
                return result;
            }

            // Valdiate Business Logo if exists
            if (businessLogo != null)
            {
                int imageResult = ImageHelper.ValidateBusinessLogoFile(businessLogo);
                if (imageResult == 0)
                {
                    result.Code = "AddUserBusiness:8";
                    result.Message = "Business logo too large. Allowed file size is 5MB.";
                    return result;
                }

                if (imageResult == 1)
                {
                    result.Code = "AddUserBusiness:9";
                    result.Message = "Invalid business logo file. Allowed file types are: png, jpg, jpeg, webp, gif.";
                    return result;
                }

                if (imageResult != 200)
                {
                    result.Code = "AddUserBusiness:10";
                    result.Message = "Failed to validate business logo.";
                    return result;
                }
            }

            var newBusinessResult = await _businessManager.AddBusiness(
                new BusinessData()
                {
                    Name = businessName,
                    MasterUserEmail = userEmail,
                    DefaultLanguage = businessDefaultLanguage,
                    Languages = new List<string> { businessDefaultLanguage },
                    Tutorials = new Dictionary<string, object>()
                    {
                        { "NewBusinessTutorial", true}
                    }
                },
                businessLogo
            );
            if (!newBusinessResult.Success)
            {
                result.Code = "AddUserBusiness:" + newBusinessResult.Code;
                result.Message = newBusinessResult.Message;
                return result;
            }

            await _userManager.AddBusinessIdToUser(userEmail, newBusinessResult.Data.Id);

            result.Success = true;
            result.Data = newBusinessResult.Data;
            return result;
        }
    }
}
