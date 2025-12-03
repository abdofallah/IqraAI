using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.WebSession;
using IqraCore.Models.WebSession;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}/websession")]
    public class APIv1BusinessWebSessionController : Controller
    {
        private readonly UserAPIValidationHelper _userAPIValidationHelper;
        private readonly UserUsageValidationManager _billingValidationManager;
        private readonly BusinessManager _businessManager;

        public APIv1BusinessWebSessionController(UserAPIValidationHelper userAPIValidationHelper, UserUsageValidationManager billingValidationManager, BusinessManager businessManager)
        {
            _userAPIValidationHelper = userAPIValidationHelper;
            _billingValidationManager = billingValidationManager;
            _businessManager = businessManager;
        }

        [HttpPost("initiate")]
        public async Task<FunctionReturnResult<InitiateWebSessionResultModel?>> InitiateWebSession(long businessId, [FromBody] InitiateWebSessionRequestModel modelData)
        {
            var result = new FunctionReturnResult<InitiateWebSessionResultModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateAPIUserAndBusinessSessionAsync(Request, businessId);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"InitiateWebSession:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var businessData = apiKeyValidaiton.Data!.businessData!;

                // Check WebSession Permissions
                if (businessData.Permission.WebSession.DisabledInitiatingAt != null)
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:BUSINESS_WEBSESSION_INITIATING_DISABLED",
                        "WebSession initiating is disabled for this business" + (string.IsNullOrWhiteSpace(businessData.Permission.WebSession.DisabledInitiatingReason) ? "" : ": " + businessData.Permission.WebSession.DisabledInitiatingReason)
                    );
                }

                // Check Balance/Package
                var checkBalanceOrMinutes = await _billingValidationManager.ValidateCallPermissionAsync(businessId);
                if (!checkBalanceOrMinutes.Success)
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:" + checkBalanceOrMinutes.Code,
                        checkBalanceOrMinutes.Message
                    );
                }

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult(
                        "GetConversations:INVALID_MODEL_DATA",
                        $"Invalid model data:\n{string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}"
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetWebSessionmanager().InitiateWebSession(businessData, modelData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "InitiateWebSession:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }

        [HttpPost("count")]
        public async Task<FunctionReturnResult<long?>> GetWebSessionsCount(long businessId, [FromBody] GetBusinessWebSessionsRequestModel modelData)
        {
            var result = new FunctionReturnResult<long?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateAPIUserAndBusinessSessionAsync(Request, businessId);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult($"GetWebSessionsCount:{apiKeyValidaiton.Code}", apiKeyValidaiton.Message);
                }
                var businessData = apiKeyValidaiton.Data!.businessData!;

                // Permissions
                if (businessData.Permission.Conversations.DisabledFullAt != null)
                {
                    return result.SetFailureResult("GetWebSessionsCount:CONVERSATIONS_DISABLED", "Business conversations are disabled.");
                }
                if (businessData.Permission.Conversations.Websocket.DisabledFullAt != null)
                {
                    return result.SetFailureResult("GetWebSessionHistory:WEBSESSION_CONVERSATIONS_DISABLED", "Business Web sessions conversations are disabled.");
                }

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult("GetWebSessionsCount:INVALID_MODEL", "Invalid model data.");
                }

                // Manager Call
                var countResult = await _businessManager.GetConversationsManager().GetWebSessionsCountAsync(businessId, modelData);

                if (!countResult.Success)
                {
                    return result.SetFailureResult($"GetWebSessionsCount:{countResult.Code}", countResult.Message);
                }

                return result.SetSuccessResult(countResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("GetWebSessionsCount:EXCEPTION", $"Internal Error: {ex.Message}");
            }
        }

        [HttpPost("history")]
        public async Task<FunctionReturnResult<PaginatedResult<WebSessionConversationMetadataModel>?>> GetWebSessionHistory(long businessId, [FromBody] GetBusinessWebSessionsRequestModel modelData)
        {
            var result = new FunctionReturnResult<PaginatedResult<WebSessionConversationMetadataModel>?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateAPIUserAndBusinessSessionAsync(Request, businessId);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult($"GetWebSessionHistory:{apiKeyValidaiton.Code}", apiKeyValidaiton.Message);
                }
                var businessData = apiKeyValidaiton.Data!.businessData!;

                // Permissions
                if (businessData.Permission.Conversations.DisabledFullAt != null)
                {
                    return result.SetFailureResult("GetWebSessionHistory:CONVERSATIONS_DISABLED", "Business conversations are disabled.");
                }
                if (businessData.Permission.Conversations.Websocket.DisabledFullAt != null)
                {
                    return result.SetFailureResult("GetWebSessionHistory:WEBSESSION_CONVERSATIONS_DISABLED", "Business Web sessions conversations are disabled.");
                }

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult("GetWebSessionHistory:INVALID_MODEL", "Invalid model data.");
                }

                // Manager Call
                var listResult = await _businessManager.GetConversationsManager().GetWebSessionsMetaDataListAsync(businessId, modelData);

                if (!listResult.Success)
                {
                    return result.SetFailureResult("GetWebSessionHistory:" + listResult.Code, listResult.Message);
                }

                return result.SetSuccessResult(listResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("GetWebSessionHistory:EXCEPTION", $"Internal Error: {ex.Message}");
            }
        }

        [HttpPost("history/{webSessionId}")]
        public async Task<FunctionReturnResult<WebSessionConversationMetadataModel?>> GetWebSessionDetail(long businessId, string webSessionId)
        {
            var result = new FunctionReturnResult<WebSessionConversationMetadataModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateAPIUserAndBusinessSessionAsync(Request, businessId);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult($"GetWebSessionDetail:{apiKeyValidaiton.Code}", apiKeyValidaiton.Message);
                }
                var businessData = apiKeyValidaiton.Data!.businessData!;

                // Permissions
                if (businessData.Permission.Conversations.DisabledFullAt != null)
                {
                    return result.SetFailureResult("GetWebSessionDetail:DISABLED", "Conversations disabled.");
                }
                if (businessData.Permission.Conversations.Websocket.DisabledFullAt != null)
                {
                    return result.SetFailureResult("GetWebSessionDetail:WEBSESSION_CONVERSATIONS_DISABLED", "Business Web sessions conversations are disabled.");
                }

                // Manager Call
                var detailResult = await _businessManager.GetConversationsManager().GetWebSessionMetaDataAsync(businessId, webSessionId);

                if (!detailResult.Success)
                {
                    return result.SetFailureResult("GetWebSessionDetail:" + detailResult.Code, detailResult.Message);
                }

                return result.SetSuccessResult(detailResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("GetWebSessionDetail:EXCEPTION", $"Internal Error: {ex.Message}");
            }
        }
    }
}
