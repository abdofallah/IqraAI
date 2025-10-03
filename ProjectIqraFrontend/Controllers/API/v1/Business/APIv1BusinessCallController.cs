using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}/call")]
    public class APIv1BusinessCallController : Controller
    {
        private readonly UserAPIValidationHelper _userAPIValidationHelper;
        private readonly UserUsageValidationManager _billingValidationManager;
        private readonly BusinessManager _businessManager;

        public APIv1BusinessCallController(UserAPIValidationHelper userAPIValidationHelper, UserUsageValidationManager billingValidationManager, BusinessManager businessManager)
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
                var checkBalanceOrMinutes = await _billingValidationManager.ValidateCallPermissionAsync(businessId, false);
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
    }
}
