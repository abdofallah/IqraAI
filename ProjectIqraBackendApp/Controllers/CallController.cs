using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Server;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Call;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackendApp.Controllers
{
    [ApiController]
    [Route("api/call")]
    public class CallController : ControllerBase
    {
        private readonly ILogger<CallController> _logger;
        private readonly CallProcessorManager _callProcessorManager;
        private readonly BackendAppConfig _backendAppConfig;

        public CallController(
            ILogger<CallController> logger,
            CallProcessorManager callProcessorManager,
            BackendAppConfig backendAppConfig
        )
        {
            _logger = logger;
            _callProcessorManager = callProcessorManager;
            _backendAppConfig = backendAppConfig;
        }

        [HttpPost("incoming")]
        public async Task<FunctionReturnResult<ProcessedInboundCallResponse?>> HandleIncomingCall([FromBody] BackendInboundCallRequest request)
        {
            var result = new FunctionReturnResult<ProcessedInboundCallResponse?>();

            if (!ValidateApiKey())
            {
                return result.SetFailureResult("HandleIncomingCall:INVALID_API_KEY", "Invalid API key");
            }

            try
            {
                // Validate the request
                if (string.IsNullOrEmpty(request.QueueId))
                {
                    return result.SetFailureResult("HandleIncomingCall:INVALID_QUEUE_ID", "Invalid queue ID");
                }

                var processInboundCallResult = await _callProcessorManager.ProcessInboundCallAsync(request.QueueId);
                if (!processInboundCallResult.Success)
                {
                    return result.SetFailureResult("InitiateOutboundCall:" + processInboundCallResult.Code, processInboundCallResult.Message);
                }

                return result.SetSuccessResult(processInboundCallResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("HandleIncomingCall:EXCEPTION", "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("outbound")]
        public async Task<FunctionReturnResult<InitiateOutboundCallResultModel>> InitiateOutboundCall([FromBody] BackendOutboundCallRequest request)
        {
            var result = new FunctionReturnResult<InitiateOutboundCallResultModel>();

            if (!ValidateApiKey())
            {
                return result.SetFailureResult("InitiateOutboundCall:INVALID_API_KEY", "Invalid API key");
            }

            try
            {
                if (string.IsNullOrEmpty(request.QueueId))
                {
                    return result.SetFailureResult("InitiateOutboundCall:INVALID_QUEUE_ID", "Invalid queue ID");
                }

                var initateOutboundCallResult = await _callProcessorManager.InitiateOutboundCallAsync(request.QueueId);
                if (!initateOutboundCallResult.Success)
                {
                    return result.SetFailureResult("InitiateOutboundCall:" + initateOutboundCallResult.Code, initateOutboundCallResult.Message);
                }

                return result.SetSuccessResult(initateOutboundCallResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("InitiateOutboundCall:EXCEPTION", "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("{sessionId}/telephonyclient/status")]
        public async Task<FunctionReturnResult> HandleSessionClientStatus(string sessionId, [FromBody] TelephonyStatusNotifyToBackendModel request)
        {
            var result = new FunctionReturnResult();

            if (!ValidateApiKey())
            {
                return result.SetFailureResult("HandleSessionClientStatus:INVALID_API_KEY", "Invalid API key");
            }

            try
            {
                if (string.IsNullOrWhiteSpace(sessionId) || request.Provider == TelephonyProviderEnum.Unknown || string.IsNullOrWhiteSpace(request.PhoneNumberId) || string.IsNullOrEmpty(request.Status))
                {
                    return result.SetFailureResult("HandleSessionClientStatus:BAD_REQUEST_DATA", "Invalid request parameters");
                }

                var notifyResult = await _callProcessorManager.NotifyTelephonyClientStatus(sessionId, request);
                if (!notifyResult.Success)
                {
                    return result.SetFailureResult($"HandleSessionClientStatus:{result.Code}", result.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling telephony call status notification");

                return result.SetFailureResult($"HandleSessionClientStatus:EXCEPTON", result.Message);
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