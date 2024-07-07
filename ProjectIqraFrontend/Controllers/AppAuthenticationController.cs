using IqraCore.Entities.User;
using IqraCore.Models.AppAuthentication;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class AppAuthenticationController : Controller
    {
        private readonly UserManager _userManager;

        public AppAuthenticationController(UserManager userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("/auth/register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!TryValidateModel(model))
            {
                return BadRequest(new { success = false, message = "Invalid email or password" });
            }

            UserData? user = await _userManager.GetUserByEmail(model.Email);
            if (user != null)
            {
                return BadRequest(new { success = false, message = "User already exists" });
            }

            user = await _userManager.RegisterUser(model);

            UserSession? session = await _userManager.CreateUserSession(user.Email);
            if (session == null)
            {
                return BadRequest(new { success = false, message = "Error while creating session" });
            }

            return Ok(new { success = true, sessionId = session.Id, authKey = session.Token });
        }

        [HttpPost("/auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!TryValidateModel(model))
            {
                return BadRequest(new { success = false, message = "Invalid email or password" });
            }

            UserData? user = await _userManager.GetUserByEmail(model.Email);
            if (user == null || !_userManager.ValidatePassword(user, model.Password))
            {
                return BadRequest(new { success = false, message = "Invalid email or password" });
            }

            UserPermission userPermission = user.Permission;
            if (!userPermission.CanLogin)
            {
                return BadRequest(new { success = false, message = "User is not allowed to login" });
            }

            UserSession? session = await _userManager.CreateUserSession(user.Email);
            if (session == null)
            {
                return BadRequest(new { success = false, message = "Error while creating session" });
            }

            await _userManager.UpdateLastLoginAndIncreaseCount(user);

            return Ok(new { success = true, sessionId = session.Id, authKey = session.Token });
        }

        [HttpPost("/auth/request-reset-password")]
        public async Task<IActionResult> RequestResetPassword([FromBody] ResetPasswordRequestModel model)
        {
            if (!TryValidateModel(model))
            {
                return BadRequest(new { success = false, message = "Invalid email" });
            }

            UserData? user = await _userManager.GetUserByEmail(model.Email);
            if (user == null)
            {
                return BadRequest(new { success = false, message = "User not found" });
            }

            await _userManager.SendPasswordResetEmail(user.Email);

            return Ok(new { success = true, message = "Reset password email sent" });
        }

        [HttpPost("/auth/reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!TryValidateModel(model))
            {
                return BadRequest(new { success = false, message = "Invalid email or token or password" });
            }

            UserData? user = await _userManager.GetUserByEmail(model.Email);
            if (user == null || !_userManager.ValidateResetPasswordToken(user, model.Token))
            {
                return BadRequest(new { success = false, message = "Invalid reset password token or token expired" });
            }

            if (!(await _userManager.ResetPassword(user.Email, model.NewPassword)))
            {
                return BadRequest(new { success = false, message = "Error while resetting password" });
            }

            return Ok(new { success = true, message = "Password reset successful" });
        }
    }
}
