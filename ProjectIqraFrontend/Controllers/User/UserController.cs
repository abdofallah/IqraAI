using IqraCore.Entities.Business;
using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.Usage;
using IqraCore.Models.User;
using IqraCore.Models.User.Billing;
using IqraCore.Models.User.GetMasterUserDataModel;
using IqraCore.Models.User.Usage;
using IqraCore.Utilities;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PhoneNumbers;

namespace ProjectIqraFrontend.Controllers.User
{
    public class UserController : Controller
    {
        private readonly AppRepository _appRepository;
        private readonly UserManager _userManager;
        private readonly UserUsageManager _userUsageManager;
        private readonly PlanManager _planManager;
        private readonly BusinessManager _businessManager;
        private readonly LanguagesManager _languageManager;
        private readonly IMongoClient _mongoClient;

        private static readonly PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();

        public UserController(
            AppRepository appRepository,
            UserManager userManager,
            UserUsageManager userUsageManager,
            PlanManager planManager,
            BusinessManager businessManager,
            LanguagesManager languageManager,
            IMongoClient mongoClient
        )
        {
            _appRepository = appRepository;
            _userManager = userManager;
            _userUsageManager = userUsageManager;
            _planManager = planManager;
            _businessManager = businessManager;
            _languageManager = languageManager;
            _mongoClient = mongoClient;
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
        public async Task<FunctionReturnResult<GetUserBillingPlanDetailsModel?>> GetUserPlanDetailsModel()
        {
            var result = new FunctionReturnResult<GetUserBillingPlanDetailsModel?>();

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

            var planData = await _planManager.GetPlanByIdAsync(user.Billing.Subscription.PlanId);
            if (!planData.Success)
            {
                result.Code = "GetUserPlanDetails:" + planData.Code;
                result.Message = planData.Message;
                return result;
            }

            GetUserBillingPlanDetailsModel planDetailsModel = new GetUserBillingPlanDetailsModel(planData.Data!);

            return result.SetSuccessResult(planDetailsModel);
        }

        /**
         * 
         * User Usage
         * 
        **/

        [HttpPost("/app/user/usage/summary")]
        public async Task<FunctionReturnResult<GetUserUsageSummaryModel?>> GetUsageSummary([FromBody] GetUserUsageSummaryRequestModel request)
        {
            var result = new FunctionReturnResult<GetUserUsageSummaryModel?>();

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
        public async Task<FunctionReturnResult<PaginatedResult<UserUsageRecordModel>?>> GetUsageHistory([FromBody] GetUserUsageHistoryRequestModel request)
        {
            var result = new FunctionReturnResult<PaginatedResult<UserUsageRecordModel>?>();

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
            var usaheHistoryResult = await _userUsageManager.GetUsageHistoryAsync(userEmail, limit, request.NextCursor, request.PreviousCursor, request.BusinessIds);
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

        [HttpPost("/app/user/businesses/{businessId}/meta")]
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

        [HttpPost("/app/user/businesses/{businessId}/app")]
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

        [HttpPost("/app/user/businesses/{businessId}/whitelabeldomains")]
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

        [HttpPost("/app/user/businesses/{businessId}")]
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

            using (var mongoSession = _mongoClient.StartSession())
            {
                try
                {
                    mongoSession.StartTransaction();

                    var newBusinessResult = await _businessManager.AddBusiness(
                        userEmail,
                        formData,
                        mongoSession
                    );
                    if (!newBusinessResult.Success)
                    {
                        result.Code = "AddUserBusiness:" + newBusinessResult.Code;
                        result.Message = newBusinessResult.Message;
                        return result;
                    }

                    await _userManager.AddBusinessIdToUser(userEmail, newBusinessResult.Data.Id, mongoSession);

                    await mongoSession.CommitTransactionAsync();

                    return result.SetSuccessResult(newBusinessResult.Data);
                }
                catch (Exception ex)
                {
                    // TODO add logging
                    return result.SetFailureResult("AddUserBusiness:MONGO_SESSION_EXCEPTION", "Exception occured during mongo session.");
                }
            }
        }

        [HttpPost("/app/user/business/delete")]
        public async Task<FunctionReturnResult> DeleteUserBusiness([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "DeleteUserBusiness:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "DeleteUserBusiness:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "DeleteUserBusiness:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.DeleteBusinessDisableAt != null)
            {
                result.Code = "DeleteUserBusiness:4";
                result.Message = "User does not have permission to delete businesses";

                if (user.Permission.Business.DisableBusinessesAt != null && !string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                if (!string.IsNullOrEmpty(user.Permission.Business.DeleteBusinessDisableReason))
                {
                    result.Message += ": " + user.Permission.Business.DeleteBusinessDisableReason;
                }

                return result;
            }

            string? businessId = formData["BusinessId"];
            if (string.IsNullOrWhiteSpace(businessId))
            {
                result.Code = "DeleteUserBusiness:5";
                result.Message = "Invalid business id";
                return result;
            }

            if (!long.TryParse(businessId, out long businessIdLong))
            {
                result.Code = "DeleteUserBusiness:6";
                result.Message = "Invalid business id";
                return result;
            }

            if (!user.Businesses.Contains(businessIdLong))
            {
                result.Code = "DeleteUserBusiness:7";
                result.Message = "User does not have business with the given id.";
                return result;
            }

            using (var mongoSession = _mongoClient.StartSession())
            {
                try
                {
                    mongoSession.StartTransaction();

                    var deleteBusinessResult = await _businessManager.DeleteBusiness(businessIdLong, mongoSession);
                    if (!deleteBusinessResult.Success)
                    {
                        mongoSession.AbortTransaction();

                        return result.SetFailureResult(
                            "DeleteUserBusiness:" + deleteBusinessResult.Code,
                            deleteBusinessResult.Message
                        );
                    }

                    var removeUserBusinessResult = await _userManager.RemoveBusinessFromUser(userEmail, businessIdLong, mongoSession);
                    if (!removeUserBusinessResult.Success)
                    {
                        mongoSession.AbortTransaction();

                        return result.SetFailureResult(
                            "DeleteUserBusiness:" + removeUserBusinessResult.Code,
                            removeUserBusinessResult.Message
                        );
                    }

                    await mongoSession.CommitTransactionAsync();

                    return result.SetSuccessResult();
                }
                catch (Exception ex)
                {
                    // TODO add logging
                    return result.SetFailureResult("DeleteUserBusiness:MONGO_SESSION_EXCEPTION", "Exception occured during mongo session.");
                }
            }            
        }
    }
}
