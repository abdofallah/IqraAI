using IqraCore.Entities.Helpers;
using IqraCore.Entities.Server.Configuration;
using IqraInfrastructure.Managers.Node;
using Microsoft.AspNetCore.Mvc;

namespace IqraBackgroundProcessor.Controllers
{
    [ApiController]
    [Route("api/node/management")]
    public class NodeManagementController : Controller
    {
        private readonly NodeLifecycleManager _nodeLifecycleManager;
        private readonly BackgroundAppConfig _backgroundAppConfig;

        public NodeManagementController(
          NodeLifecycleManager nodeLifecycleManager,
          BackgroundAppConfig backgroundAppConfig
        )
        {
            _nodeLifecycleManager = nodeLifecycleManager;
            _backgroundAppConfig = backgroundAppConfig;
        }

        [HttpPost("shutdown")]
        public async Task<FunctionReturnResult> Shutdown()
        {
            var result = new FunctionReturnResult();

            try
            {
                bool validationApiKey = ValidateApiKey();
                if (!validationApiKey)
                {
                    return result.SetFailureResult(
                        "Shutdown:INVALID_API_KEY",
                        "Invalid API key"
                    );
                }

                if (_nodeLifecycleManager.IsShutdownRequested)
                {
                    return result.SetFailureResult(
                        "Shutdown:SHUTDOWN_REQUESTED",
                        "Shutdown request already sent"
                    );
                }

                _nodeLifecycleManager.SetShutdownRequested(true);
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Shutdown:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [NonAction]
        private bool ValidateApiKey()
        {
            if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
                return false;

            string apiKey = apiKeyHeader.ToString();
            string expectedApiKey = _backgroundAppConfig.ApiKey;

            return !string.IsNullOrEmpty(apiKey) && apiKey == expectedApiKey;
        }
    }
}
