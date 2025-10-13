using IqraCore.Entities.App.Configuration;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.Authentication;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.App
{
    public class AppAuthenticationController : Controller
    {
        private readonly ILogger<AppAuthenticationController> _logger;
        private readonly UserManager _userManager;
        private readonly AppRepository _appRepository;

        public AppAuthenticationController(ILogger<AppAuthenticationController> logger, UserManager userManager, AppRepository appRepository)
        {
            _logger = logger;
            _userManager = userManager;
            _appRepository = appRepository;
        }

        [HttpPost("/auth/register")]
        public async Task<FunctionReturnResult> Register([FromBody] RegisterModel model)
        {
            var result = new FunctionReturnResult();

            try
            {
                AppPermissionConfig? appPermissionConfig = await _appRepository.GetAppPermissionConfig();
                if (appPermissionConfig == null)
                {
                    _logger.LogCritical("App permission configuration not found when trying to register user.");
                    return result.SetFailureResult(
                        "Register:PERMISSION_CONFIG_NOT_FOUND",
                        "App permission configuration not found. Notify us right away!"
                    );
                }
                if (appPermissionConfig.MaintenanceEnabledAt != null)
                {
                    string message = "Maintenance is currently enabled" + (string.IsNullOrEmpty(appPermissionConfig.PublicMaintenanceEnabledReason) ? "" : ": " + appPermissionConfig.PublicMaintenanceEnabledReason);

                    return result.SetFailureResult(
                        "Register:MAINTENANCE_ENABLED",
                        message
                    );
                }
                if (appPermissionConfig != null && appPermissionConfig.RegisterationDisabledAt != null)
                {
                    string message = "Registration is currently disabled" + (string.IsNullOrEmpty(appPermissionConfig.PublicRegisterationDisabledReason) ? "" : ": " + appPermissionConfig.PublicRegisterationDisabledReason);

                    return result.SetFailureResult(
                        "Register:REGISTERATION_DISABLED",
                        message
                    );
                }

                if (!TryValidateModel(model))
                {
                    return result.SetFailureResult(
                        "Register:MODEL_VALIDATION_FAILED",
                        "Register data validation failed:\n" + string.Join("\n", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))
                    );
                }

                bool userAlreadyExists = await _userManager.CheckUserExistsByEmail(model.Email);
                if (userAlreadyExists)
                {
                    return result.SetFailureResult(
                        "Register:USER_ALREADY_EXISTS",
                        "User already exists with this email."
                    );
                }

                var userAddResult = await _userManager.RegisterUser(model);
                if (!userAddResult.Success)
                {
                    return result.SetFailureResult(
                        "Register:" + userAddResult.Code,
                        userAddResult.Message
                    );
                }

                var emailResult = await _userManager.GenerateAndSendUserRegisterVerifyEmail(userAddResult.Data!.Email);
                if (!emailResult.Success)
                {
                    _logger.LogCritical("Email registeration successful but failed to send verify email: [{Code}] {Message}", emailResult.Code, emailResult.Message);
                    return result.SetFailureResult(
                        "Register:SUCCESS_BUT_FAILED_TO_SEND_VERIFY_EMAIL",
                        "Email registeration successful but failed to send verify email. Please contact support."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Register:EXCEPTION",
                    $"Internal server error occured while trying to register: {ex.Message}"
                );
            }
        }

        [HttpPost("/auth/verify")]
        public async Task<FunctionReturnResult> Verify([FromQuery] string email, [FromQuery] string token)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                {
                    return result.SetFailureResult(
                        "Verify:INVALID_EMAIL_OR_TOKEN",
                        "Invalid email or token"
                    );
                }

                UserData? user = await _userManager.GetFullUserByEmail(email);
                if (user == null)
                {
                    return result.SetFailureResult(
                        "Verify:USER_NOT_FOUND",
                        "User not found"
                    );
                }

                if (string.IsNullOrWhiteSpace(user.VerifyEmailToken))
                {
                    return result.SetFailureResult(
                        "Verify:ALREADY_VERIFIED",
                        "User is already verified"
                    );
                }

                if (user.VerifyEmailToken != token)
                {
                    return result.SetFailureResult(
                        "Verify:INVALID_TOKEN",
                        "Invalid verify token"
                    );
                }

                await _userManager.VerifyUserEmail(user.Email);
                var emailResult = await _userManager.SendUserRegisterWelcomeEmail(user.Email, user.FirstName, user.LastName);
                if (!emailResult.Success)
                {
                    _logger.LogCritical("Email registeration successful but failed to send registeration welcome email: [{Code}] {Message}", emailResult.Code, emailResult.Message);
                    // The user does not need to know this but we are logging it to find out why this happened
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Verify:EXCEPTION",
                    $"Internal server error occured while trying to verify: {ex.Message}"
                );
            }
        }

        [HttpPost("/auth/login")]
        public async Task<FunctionReturnResult<LoginResponseModel?>> Login([FromBody] LoginModel model)
        {
            var result = new FunctionReturnResult<LoginResponseModel?>();

            try
            {
                AppPermissionConfig? appPermissionConfig = await _appRepository.GetAppPermissionConfig();
                if (appPermissionConfig == null)
                {
                    return result.SetFailureResult(
                        "Login:PERMISSION_CONFIG_NOT_FOUND",
                        "App permission configuration not found. Notify us right away!"
                    );
                }
                if (appPermissionConfig.MaintenanceEnabledAt != null)
                {
                    string message = "Maintenance is currently enabled" + (string.IsNullOrEmpty(appPermissionConfig.PublicMaintenanceEnabledReason) ? "" : ": " + appPermissionConfig.PublicMaintenanceEnabledReason);

                    return result.SetFailureResult(
                        "Login:MAINTENANCE_ENABLED",
                        message
                    );
                }
                if (appPermissionConfig != null && appPermissionConfig.LoginDisabledAt != null)
                {
                    string message = "Login is currently disabled" + (string.IsNullOrEmpty(appPermissionConfig.PublicLoginDisabledReason) ? "." : ": " + appPermissionConfig.PublicLoginDisabledReason);
                    return result.SetFailureResult(
                        "Login:LOGIN_DISABLED",
                        message
                    );
                }

                if (!TryValidateModel(model))
                {
                    return result.SetFailureResult(
                        "Login:MODEL_VALIDATION_FAILED",
                        "Login data validation failed:\n" + string.Join("\n", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    );
                }

                UserData? user = await _userManager.GetUserDataForLoginValidation(model.Email);
                if (user == null || !_userManager.ValidatePassword(user.Email, user.PasswordSHA, model.Password))
                {
                    return result.SetFailureResult(
                        "Login:INVALID_EMAIL_OR_PASSWORD",
                        "Invalid email or password"
                    );
                }
                UserPermission userPermission = user.Permission;
                if (userPermission.DisableUserAt != null)
                {
                    var message = "User is disabled" + (string.IsNullOrEmpty(userPermission.UserDisabledReason) ? "" : ": " + userPermission.UserDisabledReason);
                    return result.SetFailureResult(
                        "Login:USER_DISABLED",
                        message
                    );
                }
                if (!string.IsNullOrWhiteSpace(user.VerifyEmailToken)) {
                    return result.SetFailureResult(
                        "Login:USER_NOT_VERIFIED",
                        "User is not verified"
                    );
                }

                UserSession? session = await _userManager.CreateUserSession(user.Email);
                if (session == null)
                {
                    return result.SetFailureResult(
                        "Login:CREATE_USER_SESSION_FAILED",
                        "Failed to create user session"
                    );
                }

                var userLoginEntry = new UserLoginEntry()
                {
                    Date = DateTime.UtcNow,
                    SessionId = session.Id,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = (Request.Headers.TryGetValue("User-Agent", out var userAgent) ? userAgent.ToString() : "")
                };

                await _userManager.UpdateLastLoginAndIncreaseCount(user.Email, userLoginEntry);
                return result.SetSuccessResult(new LoginResponseModel()
                    {
                        SessionId = session.Id,
                        AuthKey = session.Token
                    }
                );
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Login:EXCEPTION",
                    $"Internal server error occured while trying to login: {ex.Message}"
                );
            }
        }

        [HttpPost("/auth/request-reset-password")]
        public async Task<FunctionReturnResult> RequestResetPassword([FromBody] ResetPasswordRequestModel model)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (!TryValidateModel(model))
                {
                    return result.SetFailureResult(
                        "RequestResetPassword:VALIDATION_FAILED",
                        "Reset password data validation failed:\n" + string.Join("\n", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    );
                }

                UserData? user = await _userManager.GetUserDataForRequestResetPasswordValiation(model.Email);
                if (user == null)
                {
                    return result.SetFailureResult(
                        "RequestResetPassword:USER_NOT_FOUND",
                        "User not found"
                    );
                }
                UserPermission userPermission = user.Permission;
                if (userPermission.DisableUserAt != null)
                {
                    var message = "User is disabled" + (string.IsNullOrEmpty(userPermission.UserDisabledReason) ? "" : ": " + userPermission.UserDisabledReason);
                    return result.SetFailureResult(
                        "RequestResetPassword:USER_DISABLED",
                        message
                    );
                }
                if (!string.IsNullOrWhiteSpace(user.VerifyEmailToken))
                {
                    return result.SetFailureResult(
                        "RequestResetPassword:USER_NOT_VERIFIED",
                        "User is not verified"
                    );
                }

                var emailResult = await _userManager.GenerateAndSendPasswordResetEmail(model.Email, HttpContext.Connection.RemoteIpAddress?.ToString());
                if (!emailResult.Success)
                {
                    _logger.LogCritical("Failed to send password reset email: [{Code}] {Message}", emailResult.Code, emailResult.Message);
                    return result.SetFailureResult(
                        "RequestResetPassword:FAILED_TO_SEND_EMAIL",
                        "Failed to send password reset email. Please contact support."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "RequestResetPassword:EXCEPTION",
                    $"Internal server error occured while trying to request reset password: {ex.Message}"
                );
            }
        }

        [HttpPost("/auth/reset-password")]
        public async Task<FunctionReturnResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (!TryValidateModel(model))
                {
                    return result.SetFailureResult(
                        "ResetPassword:INVALID_REQUEST",
                        "Invalid reset password request:\n" + string.Join("\n", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    );
                }

                UserData? user = await _userManager.GetUserDataForResetPasswordValidation(model.Email);
                if (user == null)
                {
                    return result.SetFailureResult(
                        "ResetPassword:USER_NOT_FOUND",
                        "User not found"
                    );
                }
                UserPermission userPermission = user.Permission;
                if (userPermission.DisableUserAt != null)
                {
                    var message = "User is disabled" + (string.IsNullOrEmpty(userPermission.UserDisabledReason) ? "" : ": " + userPermission.UserDisabledReason);
                    return result.SetFailureResult(
                        "ResetPassword:USER_DISABLED",
                        message
                    );
                }
                if (!string.IsNullOrWhiteSpace(user.VerifyEmailToken))
                {
                    return result.SetFailureResult(
                        "ResetPassword:USER_NOT_VERIFIED",
                        "User is not verified"
                    );
                }

                var validateResult = await _userManager.ValidateResetPasswordToken(user.Email, user.ResetPasswordTokens, model.Token);
                if (!validateResult.Success)
                {
                    return result.SetFailureResult(
                        "ResetPassword:" + validateResult.Code,
                        validateResult.Message
                    );
                }

                if (!await _userManager.ResetPassword(user.Email, model.NewPassword))
                {
                    return result.SetFailureResult(
                        "ResetPassword:FAILED_TO_RESET_PASSWORD",
                        "Error while resetting password"
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ResetPassword:EXCEPTION",
                    $"Internal server error occured while trying to reset password: {ex.Message}"
                );
            }
        }
    }
}
