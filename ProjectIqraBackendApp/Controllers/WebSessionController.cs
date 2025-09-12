using IqraCore.Entities.Helpers;
using IqraCore.Entities.Server;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.WebSession;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackendApp.Controllers
{
    [ApiController]
    [Route("api/websession")]
    public class WebSessionController : Controller
    {
        private readonly ILogger<CallController> _logger;
        private readonly BackendAppConfig _backendAppConfig;

        private readonly BackendWebSessionProcessorManager _webSessionProcessorManager;

        public WebSessionController(ILogger<CallController> logger, BackendAppConfig backendAppConfig, BackendWebSessionProcessorManager webSessionProcessorManager)
        {
            _logger = logger;
            _backendAppConfig = backendAppConfig;
            _webSessionProcessorManager = webSessionProcessorManager;
        }

        [HttpPost("initiate")]
        public async Task<FunctionReturnResult<BackendInitiateWebSessionResultModel?>> InitiateWebSession([FromBody] BackendInitiateWebSessionRequestModel request)
        {
            var result = new FunctionReturnResult<BackendInitiateWebSessionResultModel?>();

            if (!ValidateApiKey())
            {
                return result.SetFailureResult(
                    "InitiateWebSession:INVALID_API_KEY",
                    "Invalid API key"
                );
            }

            try
            {
                if (string.IsNullOrEmpty(request.WebSessionId))
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:INVALID_WEB_SESSION_ID",
                        "Invalid web session ID"
                    );
                }

                var initateOutboundCallResult = await _webSessionProcessorManager.InitiateWebSessionConversationAsync(request.WebSessionId);
                if (!initateOutboundCallResult.Success)
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:" + initateOutboundCallResult.Code,
                        initateOutboundCallResult.Message
                    );
                }

                return result.SetSuccessResult(initateOutboundCallResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "InitiateWebSession:EXCEPTION",
                    "Internal server error: " + ex.Message
                );
            }
        }

        [NonAction]
        private bool ValidateApiKey()
        {
            if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
                return false;

            string apiKey = apiKeyHeader.ToString();
            string expectedApiKey = _backendAppConfig.ApiKey;

            return !string.IsNullOrEmpty(apiKey) && apiKey == expectedApiKey;
        }
    }
}
