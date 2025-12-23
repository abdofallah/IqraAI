using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessSettingsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessSettingsController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/settings/save")]
        public async Task<FunctionReturnResult> SaveBusinessSettings(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult();

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
                checkBusinessCanBeEdited: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessSettings:{userSessionAndBusinessValidationResult.Code}",
                    userSessionAndBusinessValidationResult.Message
                );
            }

            var updateResult = await _businessManager.GetSettingsManager().UpdateUserBusinessSettings(businessId, formData);
            if (!updateResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessSettings:{updateResult.Code}",
                    updateResult.Message
                );
            }

            return result.SetSuccessResult();
        }
    }
}
