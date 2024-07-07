using IqraCore.Entities.User;
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
        public IActionResult Index()
        {
            return View("Index");
        }

        [HttpGet("/login")]
        public async Task<IActionResult> Login()
        {
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return View("App/Authentication");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return View("App/Authentication");
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
                return View("App/Authentication");
            }

            if ((await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return RedirectToAction("Login");
            }

            return View("App/Authentication");
        }

        [HttpGet("/forget")]
        public async Task<IActionResult> Forget()
        {
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return View("App/Authentication");
            }

            if ((await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return RedirectToAction("Login");
            }

            return View("App/Authentication");
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("sessionId");
            Response.Cookies.Delete("authKey");
            Response.Cookies.Delete("userEmail");

            return RedirectToAction("Login");
        }

        [HttpGet("/app")]
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

            return View("App/Index");
        }

        [HttpGet("/app/business")]
        public async Task<IActionResult> Business()
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

            return View("App/Business");
        }


        [HttpGet("/app/business/{businessId}")]
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

            return View("App/Business");
        }

        [HttpGet("/app/admin")]
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

            return View("App/Admin");
        }
    }
}
