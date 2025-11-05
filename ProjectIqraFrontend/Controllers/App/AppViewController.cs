using IqraCore.Entities.Frontend;
using IqraCore.Entities.WhiteLabel;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Repositories.WhiteLabel;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App
{
    public class AppViewController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserManager _userManager;
        private readonly ViewLinkConfiguration _viewLinkConfiguration;
        private readonly WhiteLabelContext _whiteLabelContext;
        private readonly WhiteLabelCustomerSessionRepository _wlSessionRepo;
        private readonly UserRepository _userRepository;

        public AppViewController(
            UserSessionValidationHelper userSessionValidationHelper,
            UserManager userManager,
            ViewLinkConfiguration viewLinkConfiguration,
            WhiteLabelContext whiteLabelContext,
            WhiteLabelCustomerSessionRepository wlSessionRepo,
            UserRepository userRepository
        ) {
            _userSessionValidationHelper = userSessionValidationHelper;
            _userManager = userManager;
            _viewLinkConfiguration = viewLinkConfiguration;
            _whiteLabelContext = whiteLabelContext;
            _wlSessionRepo = wlSessionRepo;
            _userRepository = userRepository;
        }


        [HttpGet("/login")]
        public async Task<IActionResult> Login()
        {
            if (_whiteLabelContext.IsWhiteLabelRequest)
            {
                if (!(await _userSessionValidationHelper.ValidateWhiteLabelCustomerSessionAsync(Request, _whiteLabelContext)).Success)
                {
                    return View("WhiteLabel/Authentication");
                }
            }
            else if (!(await _userSessionValidationHelper.ValidateUserSessionAsync(Request)).Success)
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
        [HttpGet("/billing")]
        [HttpGet("/whitelabel")]
        public async Task<IActionResult> App()
        {
            string originalPath = Request.Path + Request.QueryString;

            if (_whiteLabelContext.IsWhiteLabelRequest)
            {
                try
                {
                    var whiteLabelCustomerValidationResult = await _userSessionValidationHelper.ValidateWhiteLabelCustomerSessionAsync(Request, _whiteLabelContext);
                    if (!whiteLabelCustomerValidationResult.Success)
                    {
                        RedirectToAction("Login", new { redirectTo = originalPath });
                    }
                    var sessionData = whiteLabelCustomerValidationResult.Data!;

                    var masterUser = await _userRepository.GetUserWhiteLabelData(sessionData.MasterUserEmail);
                    var customer = masterUser?.WhiteLabel.Customers.FirstOrDefault(c => c.Email == sessionData.CustomerEmail);

                    if (customer == null)
                    {
                        return RedirectToAction("Logout");
                    }

                    if (customer.AssignedBusinesses.Count == 0)
                    {
                        return RedirectToAction("Logout");
                    }

                    var firstBusinessId = customer.AssignedBusinesses.FirstOrDefault();
                    if (firstBusinessId > 0)
                    {
                        return RedirectToAction("Business", new { businessId = firstBusinessId });
                    }
                }
                catch (Exception ex)
                {
                     return RedirectToAction("Logout");
                }
            }
            else
            {
                if (!(await _userSessionValidationHelper.ValidateUserSessionAsync(Request)).Success)
                {
                    return RedirectToAction("Login", new { redirectTo = originalPath });
                }
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
            string originalPath = Request.Path + Request.QueryString;

            if (_whiteLabelContext.IsWhiteLabelRequest)
            {
                if (!(await _userSessionValidationHelper.ValidateWhiteLabelCustomerSessionAsync(Request, _whiteLabelContext)).Success)
                {
                    RedirectToAction("Login", new { redirectTo = originalPath });
                }
            }
            else
            {
                if (!(await _userSessionValidationHelper.ValidateUserSessionAsync(Request)).Success)
                {
                    return RedirectToAction("Login", new { redirectTo = originalPath });
                }
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
