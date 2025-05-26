using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Call;
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
        public async Task<FunctionReturnResult> HandleIncomingCall([FromBody] BackendInboundCallRequest request)
        {
            var result = new FunctionReturnResult();

            // Validate API key
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

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("HandleIncomingCall:EXCEPTION", "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("outbound")]
        public async Task<FunctionReturnResult> InitiateOutboundCall([FromBody] BackendOutboundCallRequest request)
        {
            var result = new FunctionReturnResult();

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

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("InitiateOutboundCall:EXCEPTION", "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("{sessionId}/ended")]
        public async Task<FunctionReturnResult> HandleCallEnded(string sessionId, [FromBody] CallEndNotifyBackendData request)
        {
            var result = new FunctionReturnResult();

            // Validate API key
            if (!ValidateApiKey())
            {
                result.Code = "HandleCallEnded:1";
                result.Message = "Invalid API key";
                return result;
            }

            try
            {
                // Validate the request
                if (string.IsNullOrWhiteSpace(sessionId) || request.Provider == TelephonyProviderEnum.Unknown || string.IsNullOrWhiteSpace(request.PhoneNumberId))
                {
                    result.Code = "HandleCallEnded:2";
                    result.Message = "Invalid request parameters";
                    return result;
                }

                // End the conversation session
                await _callProcessorManager.EndClientConnectionFromConversation(sessionId, "Call ended by provider via webhook", request.Provider, request.PhoneNumberId);

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling call ended notification");

                result.Code = "HandleCallEnded:-1";
                result.Message = "Internal server error";
                return result;
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