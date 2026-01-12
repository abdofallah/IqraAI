using IqraCore.Attributes;
using IqraCore.Entities.App.Lifecycle;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.App;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.App
{
    [OpenSourceOnly]
    public class AppViewController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly UserManager _userManager;
        private readonly IqraAppManager _appManager;

        public AppViewController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            UserManager userManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _userManager = userManager;
        }

        [HttpGet("/install")]
        public async Task<IActionResult> Install()
        {
            var status = _appManager.CurrentStatus;
            if (status == AppLifecycleStatus.Running)
            {
                return Redirect("/");
            }

            return View("Installation");
        }


        [HttpGet("/login")]
        public async Task<IActionResult> Login()
        {
            var validateResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAsync(Request);
            if (!validateResult.Success)
            {
                return View("Authentication");
            }

            return RedirectToAction("App");
        }

        [HttpGet("/register")]
        public async Task<IActionResult> Register()
        {
            if (!(await _userSessionValidationAndPermissionHelper.ValidateUserSessionAsync(Request)).Success)
            {
                return View("Authentication");
            }

            return RedirectToAction("App");
        }

        [HttpGet("/forget")]
        public async Task<IActionResult> Forget()
        {
            if (!(await _userSessionValidationAndPermissionHelper.ValidateUserSessionAsync(Request)).Success)
            {
                return View("Authentication");
            }

            return RedirectToAction("App");
        }

        [HttpGet("/reset")]
        public IActionResult Reset()
        {
            return View("Authentication");
        }

        [HttpGet("/verify")]
        public IActionResult Verify()
        {
            return View("Authentication");
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("wl_session");
            Response.Cookies.Delete("sessionId");
            Response.Cookies.Delete("authKey");
            Response.Cookies.Delete("userEmail");

            return RedirectToAction("Login");
        }

        [HttpGet("/")]
        [HttpGet("/businesses")]
        [HttpGet("/usage")]
        [HttpGet("/api-keys")]
        public async Task<IActionResult> App()
        {
            string originalPath = Request.Path + Request.QueryString;

            var validateResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAsync(Request);
            if (!validateResult.Success)
            {
                return RedirectToAction("Login", new { redirectTo = originalPath });
            }

            return View("Home/Home");
        }

        [HttpGet("/business")]
        public async Task<IActionResult> AppBusiness()
        {
            return RedirectToAction("App");
        }

        [HttpGet("/business/{businessId}")]
        [HttpGet("/business/{businessId}/{*tabPath}")]
        public async Task<IActionResult> Business(long? businessId)
        {
            string originalPath = Request.Path + Request.QueryString;

            var validateResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAsync(Request);
            if (!validateResult.Success)
            {
                return RedirectToAction("Login", new { redirectTo = originalPath });
            }

            return View("Business/Business");
        }

        [HttpGet("/admin")]
        [HttpGet("/admin/{*tabPath}")]
        public async Task<IActionResult> Admin()
        {
            var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAsync(Request);
            if (!validationResult.Success)
            {
                string originalPath = Request.Path + Request.QueryString;
                return RedirectToAction("Login", new { redirectTo = originalPath });
            }
            var userEmail = validationResult.Data!;

            var isUserAdmin = await _userManager.CheckUserIsAdmin(userEmail);
            if (!isUserAdmin)
            {
                return RedirectToAction("App");
            }

            return View("Admin/Admin");
        }
    }
}
