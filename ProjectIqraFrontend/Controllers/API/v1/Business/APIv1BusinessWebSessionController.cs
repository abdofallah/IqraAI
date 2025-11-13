using IqraCore.Entities.Helpers;
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
    
        // TODO
        // Get Web Sessions
        // Get Web Session by Id
    }
}
