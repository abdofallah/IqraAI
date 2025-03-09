using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackendProxy.Controllers
{
    public class TwilioWebhookController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
