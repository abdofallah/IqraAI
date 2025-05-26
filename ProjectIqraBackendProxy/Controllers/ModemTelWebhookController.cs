using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Telephony.ModemTel;
using IqraCore.Models.Telephony;
using IqraInfrastructure.Managers.Call;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackendProxy.Controllers
{
    [ApiController]
    [Route("api/modemtel/webhook")]
    public class ModemTelWebhookController : ControllerBase
    {
        private readonly ILogger<ModemTelWebhookController> _logger;
        private readonly InboundCallManager _inboundCallManager;
        private readonly CallStatusManager _callStatusManager;

        public ModemTelWebhookController(
            ILogger<ModemTelWebhookController> logger,
            InboundCallManager inboundCallManager,
            CallStatusManager callStatusManager
        )
        {
            _logger = logger;
            _inboundCallManager = inboundCallManager;
            _callStatusManager = callStatusManager;
        }

        [HttpPost("incoming/{businessId}/{phoneNumberId}")]
        public async Task<IActionResult> HandleIncomingWebhook([FromBody] ModemTelWebhookStatusData webhookData, [FromRoute] long businessId, [FromRoute] string phoneNumberId)
        {
            if (businessId < 0 || string.IsNullOrWhiteSpace(phoneNumberId) || webhookData == null)
            {
                return BadRequest("Invalid request parameters");
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

            _logger.LogInformation($"Recieved modemtel hook with status: {webhookData.CallStatus}");

            switch (webhookData.CallStatus?.ToLower())
            {
                case "incoming":
                    {
                        var distributionResult = await _inboundCallManager.DistributeIncomingCall(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return Ok(@$"<?xml version=""1.0"" encoding=""UTF-8""?><Response><Say language=""en_US"" voice=""lessac"">Hey there! We are currently at capacity or facing some issues. Please try again later.</Say><Hangup /></Response>");
                        }

                        return Ok(@$"<?xml version=""1.0"" encoding=""UTF-8""?><Response><Connect><Stream url=""{distributionResult.Data.WebhookUrl}"" track=""both_tracks"" /></Connect><Hangup /></Response>");
                    }

                case "ringing":
                    {
                        var distributionResult = await _callStatusManager.NotifyCallRinging(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return NoContent();
                        }

                        return Ok();
                    }

                case "no-answer":
                case "busy":
                    {
                        var distributionResult = await _callStatusManager.NotifyCallBusy(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return NoContent();
                        }

                        return Ok();
                    }

                case "in-progress":
                    {
                        var distributionResult = await _callStatusManager.NotifyCallStarted(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return NoContent();
                        }

                        return Ok();
                    }

                case "completed":
                    {
                        var distributionResult = await _callStatusManager.NotifyCallEnded(webhookContext);
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