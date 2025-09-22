using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.Conversations;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [Route("api/v1/business/{businessId}/call")]
    public class APIv1CallController : Controller
    {
        public readonly UserAPIValidationHelper _userAPIValidationHelper;
        private readonly BillingValidationManager _billingValidationManager;
        private readonly BusinessManager _businessManager;

        public APIv1CallController(UserAPIValidationHelper userAPIValidationHelper, BillingValidationManager billingValidationManager, BusinessManager businessManager)
        {
            _userAPIValidationHelper = userAPIValidationHelper;
            _billingValidationManager = billingValidationManager;
            _businessManager = businessManager;
        }

        [HttpPost("initiate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<FunctionReturnResult<List<string?>?>> InitiateCall(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<string?>?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateAPIUserAndBusinessSessionAsync(Request, businessId);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"InitiateCall:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var businessData = apiKeyValidaiton.Data!.businessData!;

                // Check Make Call Permissions
                if (businessData.Permission.MakeCall.DisabledCallingAt != null)
                {
                    return result.SetFailureResult(
                        "InitiateCall:BUSINESS_CALLING_DISABLED",
                        "Outbound calling is disabled for this business" + (string.IsNullOrWhiteSpace(businessData.Permission.MakeCall.DisabledCallingReason) ? "" : ": " + businessData.Permission.MakeCall.DisabledCallingReason)
                    );
                }

                // Check Balance/Package
                var checkBalanceOrMinutes = await _billingValidationManager.CheckCreditOrPackageMinutesOnly(businessId, "outbound call");
                if (!checkBalanceOrMinutes.Success)
                {
                    return result.SetFailureResult(
                        "InitiateCall:" + checkBalanceOrMinutes.Code,
                        checkBalanceOrMinutes.Message
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetMakeCallManager().QueueCallInitiationRequestAsync(businessData, formData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "InitiateCall:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "InitiateCall:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }

        [HttpGet("outboundqueues")]
        public async Task<FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>> GetOutboundCallQueues(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateAPIUserAndBusinessSessionAsync(Request, businessId);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetOutboundCallQueues:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var businessData = apiKeyValidaiton.Data!.businessData!;

                // Check Business Conversations Permissions
                if (businessData.Permission.Conversations.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "GetOutboundCallQueues:BUSINESS_CONVERSATIONS_DISABLED",
                        "Business conversations are disabled" + (string.IsNullOrWhiteSpace(businessData.Permission.Conversations.DisabledFullReason) ? "" : ": " + businessData.Permission.Conversations.DisabledFullReason)
                    );
                }
                if (businessData.Permission.Conversations.Outbound.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "GetOutboundCallQueues:BUSINESS_CONVERSATIONS_OUTBOUND_DISABLED",
                        "Business outbound conversations are disabled" + (string.IsNullOrWhiteSpace(businessData.Permission.Conversations.Outbound.DisabledFullReason) ? "" : ": " + businessData.Permission.Conversations.Outbound.DisabledFullReason)
                    );
                }

                // Get Filters if defined
                int limit = 10;
                if (formData.TryGetValue("limit", out StringValues limitValue) || !string.IsNullOrWhiteSpace(limitValue.FirstOrDefault()))
                {
                    if (int.TryParse(limitValue.First(), out int limitInt) == false)
                    {
                        return result.SetFailureResult(
                            "GetOutboundCallQueues:LIMIT_INVALID",
                            "Invalid 'limit' data in request. Could not parse."
                        );
                    }
                    limit = limitInt;
                }

                string? nextCursor = null;
                if (formData.TryGetValue("nextCursor", out StringValues nextCursorValue))
                {
                    nextCursor = nextCursorValue.First();
                }

                string? previousCursor = null;
                if (formData.TryGetValue("previousCursor", out StringValues previousCursorValue))
                {
                    previousCursor = previousCursorValue.First();
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetOutboundConversationsMetaDataListAsync(businessId, limit, nextCursor, previousCursor);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "GetOutboundCallQueues:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }

        [HttpGet("conversationstate")]
        public async Task<FunctionReturnResult<ConversationStateViewModel?>> GetConversationState(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<ConversationStateViewModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateAPIUserAndBusinessSessionAsync(Request, businessId);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetConversationState:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var businessData = apiKeyValidaiton.Data!.businessData!;

                // Check Business Conversations Permissions
                if (businessData.Permission.Conversations.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "GetConversationState:BUSINESS_CONVERSATIONS_DISABLED",
                        "Business conversations are disabled" + (string.IsNullOrWhiteSpace(businessData.Permission.Conversations.DisabledFullReason) ? "" : ": " + businessData.Permission.Conversations.DisabledFullReason)
                    );
                }

                // Get Session Id
                string? sessionId = null;
                if (!formData.TryGetValue("sessionId", out StringValues sessionIdValue))
                {
                    return result.SetFailureResult(
                        "GetConversationState:SESSION_ID_MISSING",
                        "Missing 'session id' data in request."
                    );
                }
                sessionId = sessionIdValue.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return result.SetFailureResult(
                        "GetConversationState:SESSION_ID_INVALID",
                        "Invalid 'session id' data in request."
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetConversationStateViewModelByIdAsync(businessId, sessionId);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "GetConversationState:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetConversationState:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }

    }
}
