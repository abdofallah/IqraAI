using IqraCore.Entities.Helpers;
using IqraCore.Models.WebSession;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers.API.v1
{
    [Route("api/v1/websession")]
    public class APIv1WebSessionController : Controller
    {
        private readonly UserApiKeyManager _userApiKeyManager;
        private readonly BillingValidationManager _billingValidationManager;
        private readonly BusinessManager _businessManager;

        public APIv1WebSessionController(UserApiKeyManager userApiKeyManager, BillingValidationManager billingValidationManager, BusinessManager businessManager)
        {
            _userApiKeyManager = userApiKeyManager;
            _billingValidationManager = billingValidationManager;
            _businessManager = businessManager;
        }

        [HttpPost("initiate")]
        public async Task<FunctionReturnResult<InitiateWebSessionResultModel?>> InitiateWebSession([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<InitiateWebSessionResultModel?>();

            var authorizationToken = Request.Headers["Authorization"].ToString();
            var apiKey = authorizationToken.Replace("Token ", "");

            var apiKeyValidaiton = await _userApiKeyManager.ValidateUserApiKeyAsync(apiKey);
            if (!apiKeyValidaiton.IsValid || apiKeyValidaiton.User == null || apiKeyValidaiton.ApiKey == null)
            {
                return result.SetFailureResult("InitiateWebSession:INVALID_API_KEY", "Validation failed for the api key.");
            }

            var user = apiKeyValidaiton.User;
            var apiKeyData = apiKeyValidaiton.ApiKey;

            // todo include api disabled check

            if (user.Permission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "InitiateWebSession:USER_BUSINESS_DISABLED",
                    "User businesses are disabled" + (string.IsNullOrWhiteSpace(user.Permission.Business.DisableBusinessesReason) ? "" : ": " + user.Permission.Business.DisableBusinessesReason)
                );
            }

            if (!formData.TryGetValue("businessId", out StringValues businessIdValue) || string.IsNullOrWhiteSpace(businessIdValue.FirstOrDefault()))
            {
                return result.SetFailureResult(
                    "InitiateWebSession:BUSINESS_ID_MISSING",
                    "Missing 'business id' data in request."
                );
            }
            if (long.TryParse(businessIdValue.First(), out long businessId) == false)
            {
                return result.SetFailureResult(
                    "InitiateWebSession:BUSINESS_ID_INVALID",
                    "Invalid 'business id' data in request. Could not parse."
                );
            }

            if (!user.Businesses.Contains(businessId))
            {
                return result.SetFailureResult(
                    "InitiateWebSession:BUSINESS_NOT_FOUND",
                    "User does not own this business."
                );
            }

            if (apiKeyData.RestrictedToBusinessIds.Count > 0 && !apiKeyData.RestrictedToBusinessIds.Contains(businessId))
            {
                return result.SetFailureResult(
                    "InitiateWebSession:RESTRICTED_API_KEY",
                    "API Key is restricted to a different business."
                );
            }

            var businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success || businessResult.Data == null)
            {
                return result.SetFailureResult(
                    "InitiateWebSession:" + businessResult.Code,
                    businessResult.Message
                );
            }
            var business = businessResult.Data;
            if (business.Permission.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "InitiateWebSession:BUSINESS_DISABLED",
                    "Business is disabled" + (string.IsNullOrWhiteSpace(business.Permission.DisabledFullReason) ? "" : ": " + business.Permission.DisabledFullReason)
                );
            }
            if (business.Permission.WebSession.DisabledInitiatingAt != null)
            {
                return result.SetFailureResult(
                    "InitiateWebSession:BUSINESS_WEB_SESSION_INITIATING_DISABLED",
                    "Business web session initiating is disabled" + (string.IsNullOrWhiteSpace(business.Permission.WebSession.DisabledInitiatingReason) ? "" : ": " + business.Permission.WebSession.DisabledInitiatingReason)
                );
            }

            try
            {
                var forwardResult = await _businessManager.GetWebSessionmanager().InitiateWebSession(businessResult.Data, formData);

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

    }
}
