using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase
    {
        public MainController()
        {

        }

        [HttpGet(Name = "GetTestString")]
        public string Get()
        {
            return "Test";
        }
    }
}
