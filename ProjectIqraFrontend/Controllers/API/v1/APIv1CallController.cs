using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.Conversations;
using IqraCore.Models.Business.MakeCalls;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace ProjectIqraFrontend.Controllers.API.v1
{
    [Route("api/v1/call")]
    public class APIv1CallController : Controller
    {
        private readonly UserApiKeyManager _userApiKeyManager;
        private readonly BillingValidationManager _billingValidationManager;
        private readonly BusinessManager _businessManager;

        public APIv1CallController(UserApiKeyManager userApiKeyManager, BillingValidationManager billingValidationManager, BusinessManager businessManager)
        {
            _userApiKeyManager = userApiKeyManager;
            _billingValidationManager = billingValidationManager;
            _businessManager = businessManager;
        }

        [HttpPost("initiate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<FunctionReturnResult<List<string?>?>> InitiateCall([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<string?>?>();

            var authorizationToken = Request.Headers["Authorization"].ToString();
            var apiKey = authorizationToken.Replace("Token ", "");

            var apiKeyValidaiton = await _userApiKeyManager.ValidateUserApiKeyAsync(apiKey);
            if (!apiKeyValidaiton.IsValid || apiKeyValidaiton.User == null || apiKeyValidaiton.ApiKey == null)
            {
                return result.SetFailureResult("InitiateCall:INVALID_API_KEY", "Validation failed for the api key.");
            }

            var user = apiKeyValidaiton.User;
            var apiKeyData = apiKeyValidaiton.ApiKey;

            // todo include api disabled check

            if (user.Permission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "InitiateCall:USER_BUSINESS_DISABLED",
                    "User business editing disabled" + (string.IsNullOrWhiteSpace(user.Permission.Business.DisableBusinessesReason) ? "" : ": " + user.Permission.Business.DisableBusinessesReason)
                );
            }

            if (!formData.TryGetValue("businessId", out StringValues businessIdValue) || string.IsNullOrWhiteSpace(businessIdValue.FirstOrDefault()))
            {
                return result.SetFailureResult(
                    "InitiateCall:BUSINESS_ID_MISSING",
                    "Missing 'business id' data in request."
                );
            }
            if (long.TryParse(businessIdValue.First(), out long businessId) == false)
            {
                return result.SetFailureResult(
                    "InitiateCall:BUSINESS_ID_INVALID",
                    "Invalid 'business id' data in request. Could not parse."
                );
            }

            if (!user.Businesses.Contains(businessId))
            {
                return result.SetFailureResult(
                    "InitiateCall:BUSINESS_NOT_FOUND",
                    "User does not own this business."
                );
            }

            if (apiKeyData.RestrictedToBusinessIds.Count > 0 && !apiKeyData.RestrictedToBusinessIds.Contains(businessId))
            {
                return result.SetFailureResult(
                    "InitiateCall:RESTRICTED_API_KEY",
                    "API Key is restricted to a different business."
                );
            }

            var checkBalanceOrMinutes = await _billingValidationManager.CheckCreditOrPackageMinutesOnly(businessId, "outbound call");
            if (!checkBalanceOrMinutes.Success)
            {
                return result.SetFailureResult(
                    "InitiateCall:" + checkBalanceOrMinutes.Code,
                    checkBalanceOrMinutes.Message
                );
            }

            var businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success || businessResult.Data == null)
            {
                return result.SetFailureResult(
                    "InitiateCall:" + businessResult.Code,
                    businessResult.Message
                );
            }
            var business = businessResult.Data;
            if (business.Permission.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "InitiateCall:BUSINESS_DISABLED",
                    "Business is disabled" + (string.IsNullOrWhiteSpace(business.Permission.DisabledFullReason) ? "" : ": " + business.Permission.DisabledFullReason)
                );
            }
            if (business.Permission.MakeCall.DisabledCallingAt != null)
            {
                return result.SetFailureResult(
                    "InitiateCall:BUSINESS_CALLING_DISABLED",
                    "Outbound calling is disabled for this business" + (string.IsNullOrWhiteSpace(business.Permission.MakeCall.DisabledCallingReason) ? "" : ": " + business.Permission.MakeCall.DisabledCallingReason)
                );
            }
            if (business.AllocatedMonthlyMinuteCap.HasValue)
            {
                if (business.CurrentMonthlyMinuteUsage >= business.AllocatedMonthlyMinuteCap.Value)
                {
                    return result.SetFailureResult(
                        "InitiateCall:BUSINESS_MONTHLY_MINUTE_CAP_EXCEEDED",
                        "Monthly minute cap exceeded for business"
                    );
                }
            }

            try
            {
                var forwardResult = await _businessManager.GetMakeCallManager().QueueCallInitiationRequestAsync(businessResult.Data, formData);

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
        public async Task<FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>> GetOutboundCallQueues([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>();

            var authorizationToken = Request.Headers["Authorization"].ToString();
            var apiKey = authorizationToken.Replace("Token ", "");

            var apiKeyValidaiton = await _userApiKeyManager.ValidateUserApiKeyAsync(apiKey);
            if (!apiKeyValidaiton.IsValid || apiKeyValidaiton.User == null || apiKeyValidaiton.ApiKey == null)
            {
                return result.SetFailureResult("GetOutboundCallQueues:INVALID_API_KEY", "Validation failed for the api key.");
            }

            var user = apiKeyValidaiton.User;
            var apiKeyData = apiKeyValidaiton.ApiKey;

            // todo include api disabled check

            if (user.Permission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:USER_BUSINESS_DISABLED",
                    "User business editing disabled" + (string.IsNullOrWhiteSpace(user.Permission.Business.DisableBusinessesReason) ? "" : ": " + user.Permission.Business.DisableBusinessesReason)
                );
            }

            if (!formData.TryGetValue("businessId", out StringValues businessIdValue) || string.IsNullOrWhiteSpace(businessIdValue.FirstOrDefault()))
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:BUSINESS_ID_MISSING",
                    "Missing 'business id' data in request."
                );
            }
            if (long.TryParse(businessIdValue.First(), out long businessId) == false)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:BUSINESS_ID_INVALID",
                    "Invalid 'business id' data in request. Could not parse."
                );
            }

            if (!user.Businesses.Contains(businessId))
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:BUSINESS_NOT_FOUND",
                    "User does not own this business."
                );
            }

            if (apiKeyData.RestrictedToBusinessIds.Count > 0 && !apiKeyData.RestrictedToBusinessIds.Contains(businessId))
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:RESTRICTED_API_KEY",
                    "API Key is restricted to a different business."
                );
            }

            var businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success || businessResult.Data == null)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:" + businessResult.Code,
                    businessResult.Message
                );
            }
            var business = businessResult.Data;
            if (business.Permission.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:BUSINESS_DISABLED",
                    "Business is disabled" + (string.IsNullOrWhiteSpace(business.Permission.DisabledFullReason) ? "" : ": " + business.Permission.DisabledFullReason)
                );
            }
            if (business.Permission.Conversations.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:BUSINESS_CONVERSATIONS_DISABLED",
                    "Business conversations are disabled" + (string.IsNullOrWhiteSpace(business.Permission.Conversations.DisabledFullReason) ? "" : ": " + business.Permission.Conversations.DisabledFullReason)
                );
            }
            if (business.Permission.Conversations.Outbound.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:BUSINESS_CONVERSATIONS_OUTBOUND_DISABLED",
                    "Business outbound conversations are disabled" + (string.IsNullOrWhiteSpace(business.Permission.Conversations.Outbound.DisabledFullReason) ? "" : ": " + business.Permission.Conversations.Outbound.DisabledFullReason)
                );
            }

            int limit = 20;
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

        [HttpGet("conversationstate")]
        public async Task<FunctionReturnResult<ConversationStateViewModel?>> GetConversationState([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<ConversationStateViewModel?>();

            var authorizationToken = Request.Headers["Authorization"].ToString();
            var apiKey = authorizationToken.Replace("Token ", "");

            var apiKeyValidaiton = await _userApiKeyManager.ValidateUserApiKeyAsync(apiKey);
            if (!apiKeyValidaiton.IsValid || apiKeyValidaiton.User == null || apiKeyValidaiton.ApiKey == null)
            {
                return result.SetFailureResult("GetConversationState:INVALID_API_KEY", "Validation failed for the api key.");
            }

            var user = apiKeyValidaiton.User;
            var apiKeyData = apiKeyValidaiton.ApiKey;

            // todo include api disabled check

            if (user.Permission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "GetConversationState:USER_BUSINESS_DISABLED",
                    "User business editing disabled" + (string.IsNullOrWhiteSpace(user.Permission.Business.DisableBusinessesReason) ? "" : ": " + user.Permission.Business.DisableBusinessesReason)
                );
            }

            if (!formData.TryGetValue("businessId", out StringValues businessIdValue) || string.IsNullOrWhiteSpace(businessIdValue.FirstOrDefault()))
            {
                return result.SetFailureResult(
                    "GetConversationState:BUSINESS_ID_MISSING",
                    "Missing 'business id' data in request."
                );
            }
            if (long.TryParse(businessIdValue.First(), out long businessId) == false)
            {
                return result.SetFailureResult(
                    "GetConversationState:BUSINESS_ID_INVALID",
                    "Invalid 'business id' data in request. Could not parse."
                );
            }

            if (!user.Businesses.Contains(businessId))
            {
                return result.SetFailureResult(
                    "GetConversationState:BUSINESS_NOT_FOUND",
                    "User does not own this business."
                );
            }

            if (apiKeyData.RestrictedToBusinessIds.Count > 0 && !apiKeyData.RestrictedToBusinessIds.Contains(businessId))
            {
                return result.SetFailureResult(
                    "GetConversationState:RESTRICTED_API_KEY",
                    "API Key is restricted to a different business."
                );
            }

            var businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success || businessResult.Data == null)
            {
                return result.SetFailureResult(
                    "GetConversationState:" + businessResult.Code,
                    businessResult.Message
                );
            }
            var business = businessResult.Data;
            if (business.Permission.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "GetConversationState:BUSINESS_DISABLED",
                    "Business is disabled" + (string.IsNullOrWhiteSpace(business.Permission.DisabledFullReason) ? "" : ": " + business.Permission.DisabledFullReason)
                );
            }
            if (business.Permission.Conversations.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "GetConversationState:BUSINESS_CONVERSATIONS_DISABLED",
                    "Business conversations are disabled" + (string.IsNullOrWhiteSpace(business.Permission.Conversations.DisabledFullReason) ? "" : ": " + business.Permission.Conversations.DisabledFullReason)
                );
            }

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

    }
}
