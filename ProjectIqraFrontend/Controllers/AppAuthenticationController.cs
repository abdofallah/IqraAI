using IqraCore.Entities.App.Configuration;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.Authentication;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class AppAuthenticationController : Controller
    {
        private readonly UserManager _userManager;
        private readonly AppRepository _appRepository;

        public AppAuthenticationController(UserManager userManager, AppRepository appRepository)
        {
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
                if (appPermissionConfig != null && appPermissionConfig.RegisterationDisabledAt != null)
                {
                    string message = ("Registration is currently disabled" + (string.IsNullOrEmpty(appPermissionConfig.PublicRegisterationDisabledReason) ? "" : ": " + appPermissionConfig.PublicRegisterationDisabledReason));

                    return result.SetFailureResult(
                        "Register:1",
                        message
                    );
                }

                if (!TryValidateModel(model))
                {
                    return result.SetFailureResult(
                        "Register:2",
                        "Register data validation failed"
                    );
                }

                UserData? user = await _userManager.GetUserByEmail(model.Email);
                if (user != null)
                {
                    return result.SetFailureResult(
                        "Register:3",
                        "User already exists"
                    );
                }

                user = await _userManager.RegisterUser(model);
                var emailResult = await _userManager.GenerateAndSendUserRegisterVerifyEmail(user.Email);
                if (!emailResult.Success)
                {
                    return result.SetFailureResult(
                        "Register:" + emailResult.Code,
                        "Email registeration successful but failed to send verify email: " + emailResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Register:-1",
                    "Internal server error occured while trying to register."
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
                        "Verify:1",
                        "Invalid email or token"
                    );
                }

                UserData? user = await _userManager.GetUserByEmail(email);
                if (user == null)
                {
                    return result.SetFailureResult(
                        "Verify:2",
                        "User not found"
                    );
                }

                if (string.IsNullOrWhiteSpace(user.VerifyEmailToken))
                {
                    return result.SetFailureResult(
                        "Verify:3",
                        "User is already verified"
                    );
                }

                if (user.VerifyEmailToken != token)
                {
                    return result.SetFailureResult(
                        "Verify:4",
                        "Invalid verify token"
                    );
                }

                await _userManager.VerifyUserEmail(user.Email);
                var emailResult = await _userManager.SendUserRegisterWelcomeEmail(user.Email, user.FirstName, user.LastName);
                if (!emailResult.Success)
                {
                    // The user does not need to know this but we are logging it to find out why this happened
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Verify:-1",
                    "Internal server error occured while trying to verify."
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
                if (appPermissionConfig != null && appPermissionConfig.LoginDisabledAt != null)
                {
                    string message = ("Login is currently disabled" + (string.IsNullOrEmpty(appPermissionConfig.PublicLoginDisabledReason) ? "." : ": " + appPermissionConfig.PublicLoginDisabledReason));
                    return result.SetFailureResult(
                        "Login:1",
                        message
                    );
                }

                if (!TryValidateModel(model))
                {
                    return result.SetFailureResult(
                        "Login:2",
                        "Login data validation failed"
                    );
                }

                UserData? user = await _userManager.GetUserByEmail(model.Email);
                if (user == null || !_userManager.ValidatePassword(user, model.Password))
                {
                    return result.SetFailureResult(
                        "Login:3",
                        "Invalid email or password"
                    );
                }

                if (!string.IsNullOrWhiteSpace(user.VerifyEmailToken)) {
                    return result.SetFailureResult(
                        "Login:4",
                        "User is not verified"
                    );
                }

                UserPermission userPermission = user.Permission;
                if (userPermission.LoginDisabledAt != null)
                {
                    var message = ("User is not allowed to login" + (string.IsNullOrEmpty(userPermission.LoginDisabledReason) ? "" : ": " + userPermission.LoginDisabledReason));
                    return result.SetFailureResult(
                        "Login:5",
                        message
                    );
                }

                UserSession? session = await _userManager.CreateUserSession(user.Email);
                if (session == null)
                {
                    return result.SetFailureResult(
                        "Login:6",
                        "Failed to create user session"
                    );
                }

                await _userManager.UpdateLastLoginAndIncreaseCount(user);
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
                    "Login:-1",
                    "Internal server error occured while trying to login."
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
                        "RequestResetPassword:1",
                        "Invalid email"
                    );
                }

                UserData? user = await _userManager.GetUserByEmail(model.Email);
                if (user == null)
                {
                    return result.SetFailureResult(
                        "RequestResetPassword:2",
                        "User not found"
                    );
                }

                var emailResult = await _userManager.GenerateAndSendPasswordResetEmail(user.Email, HttpContext.Connection.RemoteIpAddress?.ToString());
                if (!emailResult.Success)
                {
                    return result.SetFailureResult(
                        "RequestResetPassword:" + emailResult.Code,
                        emailResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "RequestResetPassword:-1",
                    "Internal server error occured while trying to request reset password."
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
                        "ResetPassword:1",
                        "Invalid email or token or password"
                    );
                }

                UserData? user = await _userManager.GetUserByEmail(model.Email);
                if (user == null)
                {
                    return result.SetFailureResult(
                        "ResetPassword:2",
                        "User not found"
                    );
                }

                var validateResult = await _userManager.ValidateResetPasswordToken(user, model.Token);
                if (!validateResult.Success)
                {
                    return result.SetFailureResult(
                        "ResetPassword:" + validateResult.Code,
                        validateResult.Message
                    );
                }

                if (!(await _userManager.ResetPassword(user.Email, model.NewPassword)))
                {
                    return result.SetFailureResult(
                        "ResetPassword:3",
                        "Error while resetting password"
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ResetPassword:-1",
                    "Internal server error occured while trying to reset password."
                );
            }
        }
    }
}
