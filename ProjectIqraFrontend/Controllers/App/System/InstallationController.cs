using IqraCore.Entities.Helpers;
using IqraCore.Models.App;
using IqraInfrastructure.Managers.App;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.App.System
{
    public class InstallationController : Controller
    {
        private readonly IqraAppManager _appManager;

        public InstallationController(IqraAppManager appManager)
        {
            _appManager = appManager;
        }

        [HttpPost("/api/install/setup")]
        public async Task<FunctionReturnResult> PerformSetup([FromBody] InstallRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return new FunctionReturnResult().SetFailureResult("INVALID_INPUT", "Please check input fields.");
            }

            return await _appManager.PerformFreshInstallAsync(request);
        }
    }
}
