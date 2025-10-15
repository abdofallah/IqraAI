using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserWhiteLabelController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserWhiteLabelManager _userWhiteLabelManager;

        public UserWhiteLabelController(
            UserSessionValidationHelper userSessionValidationHelper,
            UserWhiteLabelManager userWhiteLabelManager
        ) {
            _userSessionValidationHelper = userSessionValidationHelper;
            _userWhiteLabelManager = userWhiteLabelManager;
        }

        [HttpGet("/app/user/whitelabel/activate")]
        public async Task<FunctionReturnResult> ActivateUserWhiteLabel()
        {
            var result = new FunctionReturnResult();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled: true);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"ActivateUserWhiteLabel:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!;

                if (userData.Permission.WhiteLabel.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "ActivateUserWhiteLabel:USER_WHITELABEL_DISABLED",
                        $"White label is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.WhiteLabel.DisabledReason) ? "" : ": " + userData.Permission.WhiteLabel.DisabledReason)}"
                    );
                }

                var forwardResult = _userWhiteLabelManager.ActivateUserWhiteLabel(userData.Email);

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ActivateUserWhiteLabel:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }
    }
}
