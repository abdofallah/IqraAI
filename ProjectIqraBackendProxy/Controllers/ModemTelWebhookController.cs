using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Telephony.ModemTel;
using IqraCore.Models.Telephony;
using IqraInfrastructure.Managers.Call;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ProjectIqraBackendProxy.Controllers
{
    [ApiController]
    [Route("api/modemtel/webhook")]
    public class ModemTelWebhookController : ControllerBase
    {
        private readonly ILogger<ModemTelWebhookController> _logger;
        private readonly InboundCallManager _callDistributionManager;

        public ModemTelWebhookController(
            ILogger<ModemTelWebhookController> logger,
            InboundCallManager distributionService
        )
        {
            _logger = logger;
            _callDistributionManager = distributionService;
        }

        [HttpPost("status/{businessId}/{phoneNumberId}")]
        public async Task<IActionResult> HandleSattusWebhook([FromBody] ModemTelWebhookStatusData webhookData, [FromRoute] long businessId, [FromRoute] string phoneNumberId)
        {
            if (businessId < 0 || string.IsNullOrWhiteSpace(phoneNumberId) || webhookData == null)
            {
                return BadRequest("Invalid request parameters");
            }

            // Validate the webhook signature
            if (!ValidateWebhookSignature())
            {
                return Unauthorized("Invalid signature");
            }

            var webhookContext = new TelephonyWebhookContextModel
            {
                Provider = TelephonyProviderEnum.ModemTel,
                CallId = webhookData.CallId,
                BusinessId = businessId,
                PhoneNumberId = phoneNumberId,
                To = webhookData.To,
                From = webhookData.From,
                Direction = webhookData.Direction == "inbound" ? "inbound" : "outbound"
            };

            // Process webhook based on event type
            switch (webhookData.CallStatus?.ToLower())
            {
                case "incoming":
                    {
                        var distributionResult = await _callDistributionManager.DistributeIncomingCall(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return NoContent();
                        }

                        return Ok();
                    }

                case "in-progress":
                    {
                        var distributionResult = await _callDistributionManager.NotifyCallStarted(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return NoContent();
                        }

                        return Ok();
                    }

                case "completed":
                    {
                        var distributionResult = await _callDistributionManager.NotifyCallEnded(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return NoContent();
                        }

                        return Ok();
                    }

                case "no-answer":
                case "busy":
                    {
                        var distributionResult = await _callDistributionManager.NotifyCallBusy(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return NoContent();
                        }

                        return Ok();
                    }

                default:
                    return BadRequest("Unhandled event type");
            }
        }
    }
}