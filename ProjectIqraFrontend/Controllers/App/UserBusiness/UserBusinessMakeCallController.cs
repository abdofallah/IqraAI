using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.User;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using static IqraCore.Interfaces.Validation.IUserBusinessPermissionHelper;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessMakeCallController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;
        private readonly IUserUsageValidationManager _billingValidationManager;

        public UserBusinessMakeCallController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager,
            IUserUsageValidationManager billingValidationManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
            _billingValidationManager = billingValidationManager;
        }

        [HttpPost("/app/user/business/{businessId}/calls/initiate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<FunctionReturnResult<List<string?>?>> InitiateCalls(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<string?>?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
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
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"InitiateCalls:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

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
