using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Server;
using IqraCore.Models.Server;
using IqraCore.Models.Telephony;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackendApp.Controllers
{
    [ApiController]
    [Route("api/call")]
    public class CallController : ControllerBase
    {
        private readonly ILogger<CallController> _logger;
        private readonly CallProcessorManager _callProcessorManager;
        private readonly BusinessManager _businessManager;
        private readonly IConfiguration _configuration;
        private readonly ServerConfig _serverConfig;

        public CallController(
            ILogger<CallController> logger,
            CallProcessorManager callProcessorManager,
            BusinessManager businessManager,
            IConfiguration configuration,
            ServerConfig serverConfig)
        {
            _logger = logger;
            _callProcessorManager = callProcessorManager;
            _businessManager = businessManager;
            _configuration = configuration;
            _serverConfig = serverConfig;
        }

        [HttpPost("incoming")]
        public async Task<IActionResult> HandleIncomingCall([FromBody] BackendIncomingCallRequest request)
        {
            // Validate API key
            if (!ValidateApiKey())
            {
                return Unauthorized(new { success = false, message = "Invalid API key" });
            }

            _logger.LogInformation("Received incoming call request for provider {Provider}, call ID {CallId}, queue ID {QueueId}",
                request.Provider, request.CallId, request.QueueId);

            try
            {
                // Validate the request
                if (string.IsNullOrEmpty(request.QueueId) || string.IsNullOrEmpty(request.CallId) || request.BusinessId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid request parameters" });
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
                        return BadRequest(new { success = false, message = $"Unsupported provider: {request.Provider}" });
                }

                // Start the conversation session
                var sessionId = await _callProcessorManager.CreateConversationSessionAsync(
                    conversationConfig,
                    clientData,
                    CancellationToken.None
                );

                if (string.IsNullOrEmpty(sessionId))
                {
                    return StatusCode(500, new { success = false, message = "Failed to create conversation session" });
                }

                // Return success with session ID
                return Ok(new
                    {
                        success = true,
                        sessionId = sessionId
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling incoming call");
                return StatusCode(500, new { success = false, message = "Internal server error" });
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
        public async Task<IActionResult> HandleCallEnded(string sessionId)
        {
            // Validate API key
            if (!ValidateApiKey())
            {
                return Unauthorized(new { success = false, message = "Invalid API key" });
            }

            _logger.LogInformation("Received call ended notification for session {SessionId}", sessionId);

            try
            {
                // End the conversation session
                await _callProcessorManager.EndConversationSessionAsync(sessionId, "Call ended by provider");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling call ended notification");
                return StatusCode(500, new { success = false, message = "Internal server error" });
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
            if (!request.AdditionalData.TryGetValue("mediaSessionToken", out var token) ||
                !request.AdditionalData.TryGetValue("mediaSessionWebSocketUrl", out var wsUrl))
            {
                throw new ArgumentException("Missing required ModemTel media session data");
            }

            return new TelephonyWebhookContextModel
            {
                Provider = TelephonyProviderEnum.ModemTel,
                CallId = request.CallId,
                BusinessId = request.BusinessId,
                PhoneNumberId = request.PhoneNumberId,
                To = request.To,
                From = request.From,
                AdditionalData = new Dictionary<string, string>
                {
                    ["mediaSessionToken"] = token,
                    ["mediaSessionWebSocketUrl"] = wsUrl
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
            var callbackUrl = $"{_serverConfig.PublicBaseUrl}/api/call/twilio/{request.CallId}/twiml";

            return new TelephonyWebhookContextModel
            {
                Provider = TelephonyProviderEnum.Twilio,
                CallId = request.CallId,
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