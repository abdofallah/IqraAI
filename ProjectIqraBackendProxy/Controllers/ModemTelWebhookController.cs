using IqraCore.Entities.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackendProxy.Controllers
{
    public class ModemTelWebhookController : Controller
    {
        [HttpPost]
        public async Task<FunctionReturnResult>  RecieveWebhook()
    }
}
