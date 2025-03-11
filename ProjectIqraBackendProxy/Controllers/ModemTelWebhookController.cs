using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Telephony.ModemTel;
using IqraCore.Models.Telephony;
using IqraInfrastructure.Managers.Server;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackendProxy.Controllers
{
    [ApiController]
    [Route("api/webhook/modemtel")]
    public class ModemTelWebhookController : ControllerBase
    {
        private readonly ILogger<ModemTelWebhookController> _logger;
        private readonly CallDistributionManager _callDistributionManager;

        public ModemTelWebhookController(
            ILogger<ModemTelWebhookController> logger,
            CallDistributionManager distributionService
        )
        {
            _logger = logger;
            _callDistributionManager = distributionService;
        }

        [HttpPost("incoming/{businessId}/{phoneNumberId}")]
        public async Task<IActionResult> HandleIncomingWebhook([FromBody] ModemTelWebhookData webhookData, [FromQuery] long businessId, [FromQuery] string phoneNumberId)
        {
            if (businessId < 0 || string.IsNullOrWhiteSpace(phoneNumberId) || webhookData == null)
            {
                _logger.LogWarning("Invalid request parameters from {IP}", HttpContext.Connection.RemoteIpAddress);
                return BadRequest("Invalid request parameters");
            }

            _logger.LogInformation("Received ModemTel webhook: {Event} for {BusinessId}/{PhoneNumberId}", webhookData.Event, businessId, phoneNumberId);

            // Validate the webhook signature
            if (!ValidateWebhookSignature())
            {
                _logger.LogWarning("Invalid webhook signature from {IP} for {BusinessId}/{PhoneNumberId}", HttpContext.Connection.RemoteIpAddress, businessId, phoneNumberId);
                return Unauthorized("Invalid signature");
            }

            // Process webhook based on event type
            switch (webhookData.Event?.ToLower())
            {
                case "call.incoming":
                    return await HandleNewCall(webhookData, businessId, phoneNumberId);

                case "call.answered":
                    return await HandleCallAnswered(webhookData, businessId, phoneNumberId);

                case "call.ended":
                    return await HandleCallEnded(webhookData, businessId, phoneNumberId);

                default:
                    _logger.LogInformation("Unhandled webhook event: {Event} for {BusinessId}/{PhoneNumberId}", webhookData.Event, businessId, phoneNumberId);
                    return Ok(); // Acknowledge receipt of unhandled events
            }
        }

        private async Task<IActionResult> HandleNewCall(ModemTelWebhookData webhookData, long businessId, string phoneNumberId)
        {
            try
            {
                var incomingCallData = webhookData.Data as ModemTelWebhookIncomingCallData;
                if (incomingCallData == null || incomingCallData.CallId == null || incomingCallData.To == null || incomingCallData.From == null || incomingCallData.Direction == null
                    || incomingCallData.Media == null || incomingCallData.Media.Token == null || incomingCallData.Media.WebSocketURL == null)
                {
                    _logger.LogWarning("Invalid incoming call webhook data for {BusinessId}/{PhoneNumberId}", businessId, phoneNumberId);
                    return BadRequest("Invalid incoming call webhook data");
                }

                // Extract business and number information from incoming call
                var distributionResult = await _callDistributionManager.DistributeIncomingCall(
                    new TelephonyWebhookContextModel
                    {
                        Provider = TelephonyProviderEnum.ModemTel,
                        CallId = incomingCallData.CallId,
                        BusinessId = businessId,
                        PhoneNumberId = phoneNumberId,
                        To = incomingCallData.To,
                        From = incomingCallData.From,
                        AdditionalData = new Dictionary<string, string>
                        {
                            ["event"] = webhookData.Event,
                            ["direction"] = incomingCallData.Direction,
                            // MediaSession
                            ["mediaSessionToken"] = incomingCallData.Media.Token,
                            ["mediaSessionWebSocketUrl"] = incomingCallData.Media.WebSocketURL
                        }
                    });

                if (!distributionResult.Success)
                {
                    _logger.LogWarning("Failed to distribute call {CallId}: {Message} for {BusinessId}/{PhoneNumberId}", incomingCallData.CallId, distributionResult.Message, businessId, phoneNumberId);
                    return NoContent();
                }

                _logger.LogInformation("Call {CallId} routed to {BackendUrl} for {BusinessId}/{PhoneNumberId}", incomingCallData.CallId, distributionResult.MediaUrl, businessId, phoneNumberId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing new call from ModemTel for {BusinessId}/{PhoneNumberId}", businessId, phoneNumberId);
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<IActionResult> HandleCallAnswered(ModemTelWebhookData webhookData, long businessId, string phoneNumberId)
        {
            var answeredData = webhookData.Data as ModemTelWebhookAnsweredCallData;
            if (answeredData == null || answeredData.CallId == null)
            {
                _logger.LogWarning("Invalid call answered webhook data for {BusinessId}/{PhoneNumberId}", businessId, phoneNumberId);
                return BadRequest("Invalid call answered webhook data");
            }

            _logger.LogInformation("Call {CallId} answered for {BusinessId}/{PhoneNumberId}", answeredData.CallId, businessId, phoneNumberId);
            return Ok();
        }

        private async Task<IActionResult> HandleCallEnded(ModemTelWebhookData webhookData, long businessId, string phoneNumberId)
        {
            var endedData = webhookData.Data as ModemTelWebhookEndedCallData;
            if (endedData == null || endedData.CallId == null)
            {
                _logger.LogWarning("Invalid call ended webhook data for {BusinessId}/{PhoneNumberId}", businessId, phoneNumberId);
                return BadRequest("Invalid call ended webhook data");
            }

            // Inform the backend app that the call has ended (through Redis pubsub)
            await _callDistributionManager.NotifyCallEnded(
                endedData.CallId,
                TelephonyProviderEnum.ModemTel
            );

            _logger.LogInformation("Call {CallId} ended for {BusinessId}/{PhoneNumberId}", endedData.CallId, businessId, phoneNumberId);
            return Ok();
        }

        private bool ValidateWebhookSignature()
        {
            // temporary placeholder function for now
            return true;

            // In future implement the user to generate their own api key or certificate
            // validate against the phone number webhook's api key or certificate with the one recieved in the request
        }
    }
}