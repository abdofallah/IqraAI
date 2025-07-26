using IqraCore.Entities.Helpers;
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
        public async Task<FunctionReturnResult> InitiateCall([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult();

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

            if (!formData.TryGetValue("config", out StringValues configJsonValues) || string.IsNullOrWhiteSpace(configJsonValues.FirstOrDefault()))
            {
                return result.SetFailureResult(
                    "InitiateCall:CALL_CONFIG_MISSING",
                    "Missing 'config' data in request."
                );
            }
            string configJson = configJsonValues.First() ?? "";

            MakeCallRequestDto? callConfig;
            try
            {
                callConfig = JsonSerializer.Deserialize<MakeCallRequestDto>(configJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (callConfig == null)
                {
                    return result.SetFailureResult(
                        "InitiateCall:CALL_CONFIG_INVALID",
                        "Unable to deserialize 'config' JSON."
                    );
                }
            }
            catch (JsonException ex)
            {
                return result.SetFailureResult(
                    "InitiateCall:CALL_CONFIG_INVALID",
                    $"Invalid 'config' JSON format: {ex.Message}"
                );
            }
            IFormFile? bulkCsvFile = formData.Files.GetFile("bulk_file");

            try
            {
                var forwardResult = await _businessManager.GetMakeCallManager().QueueCallInitiationRequestAsync(businessResult.Data, callConfig, bulkCsvFile);

                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "InitiateCall:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "InitiateCall:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }
    }
}
