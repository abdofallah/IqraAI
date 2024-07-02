using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class AppIndexController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
