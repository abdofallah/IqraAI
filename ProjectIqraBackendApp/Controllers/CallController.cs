using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Server.Call;
using IqraCore.Models.Server;
using IqraCore.Models.Telephony;
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
        private readonly IConfiguration _configuration;

        public CallController(
            ILogger<CallController> logger,
            CallProcessorManager callProcessorManager,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _callProcessorManager = callProcessorManager;
            _configuration = configuration;
        }

        [HttpPost("incoming")]
        public async Task<FunctionReturnResult> HandleIncomingCall([FromBody] BackendIncomingCallRequest request)
        {
            var result = new FunctionReturnResult();

            // Validate API key
            if (!ValidateApiKey())
            {
                result.Code = "HandleIncomingCall:1";
                result.Message = "Invalid API key";
                return result;
            }

            _logger.LogInformation("Received incoming call request for provider {Provider}, call ID {CallId}, queue ID {QueueId}",
                request.Provider, request.ProviderCallId, request.QueueId);

            try
            {
                // Validate the request
                if (string.IsNullOrEmpty(request.QueueId) || string.IsNullOrEmpty(request.ProviderCallId) || request.BusinessId < 0)
                {
                    result.Code = "HandleIncomingCall:2";
                    result.Message = "Invalid request parameters";
                    return result;
                }

                // Create the conversation configuration
                var conversationConfig = new ConversationSessionConfiguration
                {
                    BusinessId = request.BusinessId,
                    QueueId = request.QueueId,
                    RouteId = request.RouteId
                };

                // Create telephony client data based on provider
                TelephonyWebhookContextModel clientData;

                switch (request.Provider)
                {
                    case TelephonyProviderEnum.ModemTel:
                        clientData = CreateModemTelClientData(request);
                        break;
                    case TelephonyProviderEnum.Twilio:
                        clientData = CreateTwilioClientData(request);
                        break;
                    default:
                        result.Code = "HandleIncomingCall:3";
                        result.Message = "Unsupported telephony provider";
                        return result;
                }

                // Start the conversation session
                var sessionId = await _callProcessorManager.CreateConversationSessionAsync(
                    conversationConfig,
                    clientData,
                    CancellationToken.None
                );

                if (string.IsNullOrEmpty(sessionId))
                {
                    result.Code = "HandleIncomingCall:4";
                    result.Message = "Failed to create conversation session";
                    return result;
                }

                // Return success with session ID
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling incoming call");
                result.Code = "HandleIncomingCall:-1";
                result.Message = "Internal server error";
                return result;
            }
        }

        [HttpPost("outbound")]
        public async Task<IActionResult> InitiateOutboundCall([FromBody] OutboundCallRequestModel request)
        {
            // Validate API key
            if (!ValidateApiKey())
            {
                return Unauthorized(new { success = false, message = "Invalid API key" });
            }

            _logger.LogInformation("Received outbound call request to {ToNumber} for business {BusinessId}",
                request.ToNumber, request.BusinessId);

            try
            {
                // Validate the request
                if (string.IsNullOrEmpty(request.QueueId) || request.BusinessId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid request parameters" });
                }

                // Initialize telephony provider for outbound call
                var result = await _callProcessorManager.InitiateOutboundCallAsync(
                    request.BusinessId,
                    request.PhoneNumberId,
                    request.ToNumber,
                    request.QueueId,
                    request.RouteId
                );

                if (!result.Success)
                {
                    return BadRequest(new { success = false, message = result.Message });
                }

                // Return success with call details
                return Ok(new
                {
                    success = true,
                    callId = result.CallId,
                    status = result.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating outbound call");
                return StatusCode(500, new { success = false, message = "Internal server error" });
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

            _logger.LogInformation("Received call ended notification for session {SessionId}", sessionId);

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

        private bool ValidateApiKey()
        {
            if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
                return false;

            string apiKey = apiKeyHeader.ToString();
            string expectedApiKey = _configuration["Security:ApiKey"];

            return !string.IsNullOrEmpty(apiKey) && apiKey == expectedApiKey;
        }

        private TelephonyWebhookContextModel CreateModemTelClientData(BackendIncomingCallRequest request)
        {
            if (!request.AdditionalData.TryGetValue("mediaSessionToken", out var token))
            {
                throw new ArgumentException("Missing required ModemTel media session data");
            }

            return new TelephonyWebhookContextModel
            {
                Provider = TelephonyProviderEnum.ModemTel,
                CallId = request.ProviderCallId,
                BusinessId = request.BusinessId,
                PhoneNumberId = request.PhoneNumberId,
                To = request.To,
                From = request.From,
                AdditionalData = new Dictionary<string, string>
                {
                    ["mediaSessionToken"] = token
                }
            };
        }

        private TelephonyWebhookContextModel CreateTwilioClientData(BackendIncomingCallRequest request)
        {
            // Extract Twilio-specific data from request
            if (!request.AdditionalData.TryGetValue("accountSid", out var accountSid))
            {
                throw new ArgumentException("Missing required Twilio account SID");
            }

            // For Twilio, we need to create a callback URL for TwiML
            var callbackUrl = $"TODO/api/call/twilio/{request.ProviderCallId}/twiml";

            return new TelephonyWebhookContextModel
            {
                Provider = TelephonyProviderEnum.Twilio,
                CallId = request.ProviderCallId,
                BusinessId = request.BusinessId,
                PhoneNumberId = request.PhoneNumberId,
                To = request.To,
                From = request.From,
                AdditionalData = new Dictionary<string, string>
                {
                    ["accountSid"] = accountSid,
                    ["callbackUrl"] = callbackUrl
                }
            };
        }
    }
}