using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.MakeCalls;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;
using System.Text.Json;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessMakeCallController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly UserUsageValidationManager _billingValidationManager;

        public UserBusinessMakeCallController(UserSessionValidationHelper userSessionValidationHelper, UserManager userManager, BusinessManager businessManager, UserUsageValidationManager billingValidationManager)
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _userManager = userManager;
            _businessManager = businessManager;
            _billingValidationManager = billingValidationManager;
        }

        [HttpPost("/app/user/business/{businessId}/calls/initiate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<FunctionReturnResult<List<string>?>> InitiateCalls(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<string>?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                    Request,
                    businessId,
                    checkUserDisabled: true,
                    checkBusinessesDisabled: true,
                    checkBusinessesEditingEnabled: true
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    result.Code = $"SaveBusinessCampaign:{userSessionAndBusinessValidationResult.Code}";
                    result.Message = userSessionAndBusinessValidationResult.Message;
                    return result;
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                if (businessData.Permission.MakeCall.DisabledCallingAt != null)
                {
                    return result.SetFailureResult(
                        "InitiateCalls:8",
                        "Outbound calling is disabled for this business" + (string.IsNullOrWhiteSpace(businessData.Permission.MakeCall.DisabledCallingReason) ? "" : ": " + businessData.Permission.MakeCall.DisabledCallingReason)
                    );
                }

                var checkBalanceOrMinutes = await _billingValidationManager.ValidateCallPermissionAsync(businessId);
                if (!checkBalanceOrMinutes.Success)
                {
                    return result.SetFailureResult(
                        "InitiateCalls:" + checkBalanceOrMinutes.Code,
                        checkBalanceOrMinutes.Message
                    );
                }

                var forwardResult = await _businessManager.GetMakeCallManager().QueueCallInitiationRequestAsync(businessData, formData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "InitiateCalls:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "InitiateCalls:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }
    }
}
