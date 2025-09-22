using IqraCore.Entities.Helpers;
using IqraCore.Models.WebSession;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}/websession")]
    public class APIv1BusinessWebSessionController : Controller
    {
        private readonly UserAPIValidationHelper _userAPIValidationHelper;
        private readonly BillingValidationManager _billingValidationManager;
        private readonly BusinessManager _businessManager;

        public APIv1BusinessWebSessionController(UserAPIValidationHelper userAPIValidationHelper, BillingValidationManager billingValidationManager, BusinessManager businessManager)
        {
            _userAPIValidationHelper = userAPIValidationHelper;
            _billingValidationManager = billingValidationManager;
            _businessManager = businessManager;
        }

        [HttpPost("initiate")]
        public async Task<FunctionReturnResult<InitiateWebSessionResultModel?>> InitiateWebSession(long businessId, [FromForm] IFormCollection formData)
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
                var checkBalanceOrMinutes = await _billingValidationManager.CheckCreditOrPackageMinutesOnly(businessId, "websession");
                if (!checkBalanceOrMinutes.Success)
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:" + checkBalanceOrMinutes.Code,
                        checkBalanceOrMinutes.Message
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetWebSessionmanager().InitiateWebSession(businessData, formData);
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
    
        // TODO
        // Get Web Sessions
        // Get Web Session by Id
    }
}
