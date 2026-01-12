using IqraCore.Entities.App.Configuration;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.App;
using IqraInfrastructure.Managers.App;
using IqraInfrastructure.Repositories.App;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    [Route("app/admin/app")]
    public class AppAdminAppController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly AppRepository _appRepository;
        private readonly IqraAppManager _appManager;

        public AppAdminAppController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            AppRepository appRepository,
            IqraAppManager appManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _appRepository = appRepository;
            _appManager = appManager;
        }

        [HttpGet("config")]
        public async Task<FunctionReturnResult<IqraAppConfig?>> GetAppConfig()
        {
            var result = new FunctionReturnResult<IqraAppConfig?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetAppConfig:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                // get cached config
                var config = _appManager.CurrentConfig;
                return result.SetSuccessResult(config);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetAppConfig:EXCEPTION",
                    $"Failed to get app config. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("update-status")]
        public async Task<FunctionReturnResult<UpdateCheckResult?>> CheckForUpdates()
        {
            var result = new FunctionReturnResult<UpdateCheckResult?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"CheckForUpdates:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                // get cached update check result
                return result.SetSuccessResult(_appManager.CurrentUpdateCheckResult);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "CheckForUpdates:EXCEPTION",
                    $"Failed to check for updates. Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("permissions")]
        public async Task<FunctionReturnResult<AppPermissionConfig?>> GetPermissions()
        {
            var result = new FunctionReturnResult<AppPermissionConfig?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetPermissions:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var config = await _appRepository.GetAppPermissionConfig();
                return result.SetSuccessResult(config ?? new AppPermissionConfig());
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetPermissions:EXCEPTION",
                    $"Failed to get permissions. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("permissions")]
        public async Task<FunctionReturnResult<AppPermissionConfig?>> UpdatePermissions([FromBody] AppPermissionConfig permissions)
        {
            var result = new FunctionReturnResult<AppPermissionConfig?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"UpdatePermissions:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                if (permissions.MaintenanceEnabledAt != null)
                {
                    if (string.IsNullOrEmpty(permissions.PublicMaintenanceEnabledReason) || string.IsNullOrEmpty(permissions.PrivateMaintenanceEnabledReason))
                    {
                        return result.SetFailureResult(
                            "UpdatePermissions:INVALID_MAINTENANCE_REASON",
                            "Maintenance public or private reason is missing."
                        );
                    }
                }

                if (permissions.RegisterationDisabledAt != null)
                {
                    if (string.IsNullOrEmpty(permissions.PublicRegisterationDisabledReason) || string.IsNullOrEmpty(permissions.PrivateRegisterationDisabledReason))
                    {
                        return result.SetFailureResult(
                            "UpdatePermissions:INVALID_REGISTERATION_REASON",
                            "Registeration public or private reason is missing."
                        );
                    }
                }

                if (permissions.LoginDisabledAt != null)
                {
                    if (string.IsNullOrEmpty(permissions.PublicLoginDisabledReason) || string.IsNullOrEmpty(permissions.PrivateLoginDisabledReason))
                    {
                        return result.SetFailureResult(
                            "UpdatePermissions:INVALID_LOGIN_REASON",
                            "Login public or private reason is missing."
                        );
                    }
                }

                var success = await _appRepository.AddUpdateAppPermissionConfig(permissions);
                if (!success)
                {
                    return result.SetFailureResult(
                        "UpdatePermissions:SAVE_FAILED",
                        "Failed to save permissions."
                    );
                }

                return result.SetSuccessResult(permissions);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "UpdatePermissions:EXCEPTION",
                    $"Failed to update permissions. Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("email-templates")]
        public async Task<FunctionReturnResult<EmailTemplates?>> GetEmailTemplates()
        {
            var result = new FunctionReturnResult<EmailTemplates?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetEmailTemplates:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var templates = await _appRepository.GetEmailTemplates();
                return result.SetSuccessResult(templates ?? new EmailTemplates());
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetEmailTemplates:EXCEPTION",
                    $"Failed to get email templates. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("email-templates")]
        public async Task<FunctionReturnResult<EmailTemplates?>> UpdateEmailTemplates([FromBody] EmailTemplates templates)
        {
            var result = new FunctionReturnResult<EmailTemplates?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"UpdateEmailTemplates:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                if (string.IsNullOrEmpty(templates.VerifyEmailTemplate.Subject) || string.IsNullOrEmpty(templates.VerifyEmailTemplate.Body))
                {
                    return result.SetFailureResult(
                        "UpdateEmailTemplates:INVALID_VERIFY_TEMPLATE",
                        "Verify email template is missing subject or body."
                    );
                }

                if (string.IsNullOrEmpty(templates.WelcomeUserTemplate.Subject) || string.IsNullOrEmpty(templates.WelcomeUserTemplate.Body))
                {
                    return result.SetFailureResult(
                        "UpdateEmailTemplates:INVALID_WELCOME_TEMPLATE",
                        "Welcome user template is missing subject or body."
                    );
                }

                if (string.IsNullOrEmpty(templates.ResetPasswordTemplate.Subject) || string.IsNullOrEmpty(templates.ResetPasswordTemplate.Body))
                {
                    return result.SetFailureResult(
                        "UpdateEmailTemplates:INVALID_RESET_PASSWORD_TEMPLATE",
                        "Reset password template is missing subject or body."
                    );
                }

                var success = await _appRepository.AddUpdateEmailTemplates(templates);
                if (!success)
                {
                    return result.SetFailureResult(
                        "UpdateEmailTemplates:SAVE_FAILED",
                        "Failed to save templates."
                    );
                }

                return result.SetSuccessResult(templates);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "UpdateEmailTemplates:EXCEPTION",
                    $"Failed to update email templates. Exception: {ex.Message}"
                );
            }
        }
    }
}
