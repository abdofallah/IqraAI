using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.User;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using static IqraCore.Interfaces.Validation.IUserBusinessPermissionHelper;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}/call")]
    public class APIv1BusinessCallController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly IUserUsageValidationManager _billingValidationManager;
        private readonly BusinessManager _businessManager;

        public APIv1BusinessCallController(
            ISessionValidationAndPermissionHelper sessionValidationAndPermissionHelper,
            IUserUsageValidationManager billingValidationManager,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = sessionValidationAndPermissionHelper;
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
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "MakeCall",
                            Type = BusinessModulePermissionType.Full,
                        }
                    }
                );
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"InitiateCall:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var businessData = apiKeyValidaiton.Data!.businessData!;

                // Check Balance/Package
                var checkBalanceOrMinutes = await _billingValidationManager.ValidateCallPermissionAsync(businessId);
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
