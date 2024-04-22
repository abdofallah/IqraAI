using IqraInfrastructure.Services.App;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ModemController : ControllerBase
    {
        private readonly ApiManager _apiManager;
        private readonly ModemsManager _modemsManager;

        public ModemController(ApiManager apiManager, ModemsManager modemsManager)
        {
            _apiManager = apiManager;
            _modemsManager = modemsManager;
        }

        [HttpPost("ReloadModemsList")]
        public async Task<IActionResult> ReloadModemsList()
        {
            string? ApiKey = Request.Headers["ApiKey"];
            if ((await _apiManager.ValidateApiKey(ApiKey)) == false)
            {
                return BadRequest("Invalid Api Key");
            }

            await _modemsManager.LoadDevices();

            return Ok();
        }

        [HttpPost("GetConnectedModemsList")]
        public async Task<IActionResult> GetConnectedModemsList()
        {
            string? ApiKey = Request.Headers["ApiKey"];
            if ((await _apiManager.ValidateApiKey(ApiKey)) == false)
            {
                return BadRequest("Invalid Api Key");
            }

            return new JsonResult(new { data = _modemsManager.GetModemInstances() });
        }

        [HttpPost("SetModemPhoneNumber")]
        public async Task<IActionResult> SetModemPhoneNumber(string modemBaseContainerId, string phoneNumber)
        {
            string? ApiKey = Request.Headers["ApiKey"];
            if ((await _apiManager.ValidateApiKey(ApiKey)) == false)
            {
                return BadRequest("Invalid Api Key");
            }

            if (await _modemsManager.SetPhoneNumber(modemBaseContainerId, phoneNumber) == false)
            {
                return BadRequest("Failed to set phone number");
            }

            return Ok();
        }
    }
}
