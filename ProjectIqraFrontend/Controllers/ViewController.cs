using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class ViewController : Controller
    {
        private readonly UserManager _userManager;

        public ViewController(UserManager userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("/")]
        public async Task<IActionResult> Index()
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

            return View();
        }

        [HttpGet("/login")]
        public async Task<IActionResult> Login()
        {
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return View();
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return View();
            }

            return RedirectToAction("Index");
        }

        [HttpGet("/password-reset")]
        public IActionResult PasswordReset()
        {
            return View();
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("sessionId");
            Response.Cookies.Delete("authKey");
            Response.Cookies.Delete("userEmail");

            return RedirectToAction("Login");
        }
    }
}
