using IqraCore.Entities.Helpers;
using IqraCore.Models.Usage;
using IqraCore.Models.User.Usage;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.API.v1.User
{
    [ApiController]
    [Route("api/v1/user/usage")]
    public class APIv1UserUsageController : Controller
    {
        private readonly UserAPIValidationHelper _userAPIValidationHelper;
        private readonly UserUsageManager _userUsageManager;

        public APIv1UserUsageController(UserAPIValidationHelper userAPIValidationHelper, UserUsageManager userUsageManager)
        {
            _userAPIValidationHelper = userAPIValidationHelper;
            _userUsageManager = userUsageManager;
        }

        [HttpPost("count")]
        public async Task<FunctionReturnResult<GetUserUsageCountResponseModel?>> GetUsageCount([FromBody] GetUserUsageCountRequestModel modelData)
        {
            var result = new FunctionReturnResult<GetUserUsageCountResponseModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateUserAPIAsync(Request);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetUsageCount:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var userData = apiKeyValidaiton.Data!.userData!;

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult(
                        "GetUsageCount:INVALID_MODEL_DATA",
                        $"Invalid model data:\n{string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}"
                    );
                }

                // Forward
                var forwardResult = await _userUsageManager.GetUsageCount(userData.Email, modelData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUsageCount:{forwardResult.Code}",
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUsageCount:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
        }

        [HttpPost("history")]
        public async Task<FunctionReturnResult<PaginatedResult<MinuteUsageRecordModel>?>> GetUsageHistory([FromBody] GetUserUsageHistoryRequestModel modelData)
        {
            var result = new FunctionReturnResult<PaginatedResult<MinuteUsageRecordModel>?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateUserAPIAsync(Request);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetUsageHistory:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var userData = apiKeyValidaiton.Data!.userData!;

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult(
                        "GetUsageHistory:INVALID_MODEL_DATA",
                        $"Invalid model data:\n{string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}"
                    );
                }

                // Forward
                var forwardResult = await _userUsageManager.GetUsageHistoryAsync(userData.Email, modelData.Limit, modelData.NextCursor, modelData.PreviousCursor, modelData.BusinessIds);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUsageHistory:{forwardResult.Code}",
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUsageHistory:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
        }
    }
}
