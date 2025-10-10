using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.Usage;
using IqraCore.Models.User.GetMasterUserDataModel;
using IqraCore.Models.User.Usage;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserManager _userManager;
        private readonly UserUsageManager _userUsageManager;

        public UserController(
            UserSessionValidationHelper userSessionValidationHelper,
            UserManager userManager,
            UserUsageManager userUsageManager
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _userManager = userManager;
            _userUsageManager = userUsageManager;
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
    }
}
