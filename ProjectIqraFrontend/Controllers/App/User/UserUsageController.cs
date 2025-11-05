using IqraCore.Entities.Helpers;
using IqraCore.Models.Usage;
using IqraCore.Models.User.Usage;
using IqraCore.Models.User.Usage.Summary;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserUsageController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserUsageManager _userUsageManager;

        public UserUsageController(
            UserSessionValidationHelper userSessionValidationHelper,
            UserUsageManager userUsageManager
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _userUsageManager = userUsageManager;
        }

        [HttpPost("/app/user/usage/summary")]
        public async Task<FunctionReturnResult<UserUsageSummaryResponseModel?>> GetUsageSummary([FromBody] UserUsageSummaryRequestModel request)
        {
            var result = new FunctionReturnResult<UserUsageSummaryResponseModel?>();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled: true);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUsageSummary:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

                if (request == null)
                {
                    return result.SetFailureResult(
                        "GetUsageSummary:INVALID_REQUEST_DATA",
                        "Invalid request data"
                    );
                }

                var usageSummaryResult = await _userUsageManager.GetUsageSummaryAsync(userData.Email, request);
                if (!usageSummaryResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUsageSummary:{usageSummaryResult.Code}",
                        usageSummaryResult.Message
                    );
                }

                return result.SetSuccessResult(usageSummaryResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUsageSummary:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/usage/history")]
        public async Task<FunctionReturnResult<PaginatedResult<UserUsageRecordModel>?>> GetUsageHistory([FromBody] GetUserUsageHistoryRequestModel request)
        {
            var result = new FunctionReturnResult<PaginatedResult<UserUsageRecordModel>?>();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled: true);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUsageHistory:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

                var limit = Math.Clamp(request.Limit, 10, 50);
                var usageHistoryResult = await _userUsageManager.GetUsageHistoryAsync(userData.Email, limit, request.NextCursor, request.PreviousCursor, request.BusinessIds);
                if (!usageHistoryResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUsageHistory:{usageHistoryResult.Code}",
                        usageHistoryResult.Message
                    );
                }

                return result.SetSuccessResult(usageHistoryResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUsageHistory:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }
    }
}
