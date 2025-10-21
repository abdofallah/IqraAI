using IqraCore.Entities.Frontend;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App
{
    public class AppViewController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserManager _userManager;
        private readonly ViewLinkConfiguration _viewLinkConfiguration;

        public AppViewController(UserSessionValidationHelper userSessionValidationHelper, UserManager userManager, ViewLinkConfiguration viewLinkConfiguration)
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _userManager = userManager;
            _viewLinkConfiguration = viewLinkConfiguration;
        }

        [HttpGet("/login")]
        public async Task<IActionResult> Login()
        {
            if (!(await _userSessionValidationHelper.ValidateUserSessionAsync(Request)).Success)
            {
                return View("Authentication");
            }

            return RedirectToAction("App");
        }

        [HttpGet("/register")]
        public async Task<IActionResult> Register()
        {
            if (!(await _userSessionValidationHelper.ValidateUserSessionAsync(Request)).Success)
            {
                return View("Authentication");
            }

            return RedirectToAction("App");
        }

        [HttpGet("/forget")]
        public async Task<IActionResult> Forget()
        {
            if (!(await _userSessionValidationHelper.ValidateUserSessionAsync(Request)).Success)
            {
                return View("Authentication");
            }

            return RedirectToAction("App");
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("sessionId");
            Response.Cookies.Delete("authKey");
            Response.Cookies.Delete("userEmail");

            return RedirectToAction("Login");
        }

        [HttpGet("/")]
        [HttpGet("/businesses")]
        [HttpGet("/usage")]
        [HttpGet("/api-keys")]
        [HttpGet("/billing")]
        [HttpGet("/whitelabel")]
        public async Task<IActionResult> App()
        {
            if (!(await _userSessionValidationHelper.ValidateUserSessionAsync(Request)).Success)
            {
                string originalPath = Request.Path + Request.QueryString;
                return RedirectToAction("Login", new { redirectTo = originalPath });
            }

            TempData.TryAdd("BusinessLogoURL", _viewLinkConfiguration.BusinessLogoURL);

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
            if (!(await _userSessionValidationHelper.ValidateUserSessionAsync(Request)).Success)
            {
                string originalPath = Request.Path + Request.QueryString;
                return RedirectToAction("Login", new { redirectTo = originalPath });
            }

            TempData.TryAdd("BusinessLogoURL", _viewLinkConfiguration.BusinessLogoURL);
            TempData.TryAdd("BusinessToolAudioURL", _viewLinkConfiguration.BusinessToolAudioURL);
            TempData.TryAdd("BusinessAgentBackgroundAudioURL", _viewLinkConfiguration.BusinessAgentBackgroundAudioURL);
            TempData.TryAdd("IntegrationLogoURL", _viewLinkConfiguration.IntegrationLogoURL);
            return View("Business/Business");
        }

        [HttpGet("/admin")]
        public async Task<IActionResult> Admin()
        {
            var validationResult = await _userSessionValidationHelper.ValidateUserSessionAsync(Request);
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

            TempData.TryAdd("BusinessLogoURL", _viewLinkConfiguration.BusinessLogoURL);
            TempData.TryAdd("IntegrationLogoURL", _viewLinkConfiguration.IntegrationLogoURL);
            return View("Admin/Admin");
        }
    }
}
