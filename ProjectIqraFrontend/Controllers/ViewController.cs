using IqraCore.Entities.Frontend;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class ViewController : Controller
    {
        private readonly UserManager _userManager;
        private readonly ViewLinkConfiguration _viewLinkConfiguration;

        public ViewController(UserManager userManager, ViewLinkConfiguration viewLinkConfiguration)
        {
            _userManager = userManager;
            _viewLinkConfiguration = viewLinkConfiguration;
        }

        [HttpGet("/login")]
        public async Task<IActionResult> Login()
        {
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return View("Authentication");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return View("Authentication");
            }

            return RedirectToAction("App");
        }

        [HttpGet("/register")]
        public async Task<IActionResult> Register()
        {
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return View("Authentication");
            }

            if ((await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return RedirectToAction("Login");
            }

            return View("Authentication");
        }

        [HttpGet("/forget")]
        public async Task<IActionResult> Forget()
        {
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return View("Authentication");
            }

            if ((await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return RedirectToAction("Login");
            }

            return View("Authentication");
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
        public async Task<IActionResult> App()
        {
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return RedirectToAction("Login");
            }

            TempData.TryAdd("BusinessLogoURL", _viewLinkConfiguration.BusinessLogoURL);

            return View("Home/Home");
        }

        [HttpGet("/business")]
        public async Task<IActionResult> Business()
        {
            return RedirectToAction("App");
        }


        [HttpGet("/business/{businessId}")]
        public async Task<IActionResult> Business(long? businessId)
        {
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return RedirectToAction("Login");
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
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return RedirectToAction("Login");
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (!user.Permission.IsAdmin)
            {
                return RedirectToAction("App");
            }

            TempData.TryAdd("BusinessLogoURL", _viewLinkConfiguration.BusinessLogoURL);
            TempData.TryAdd("IntegrationLogoURL", _viewLinkConfiguration.IntegrationLogoURL);

            return View("Admin");
        }
    }
}
