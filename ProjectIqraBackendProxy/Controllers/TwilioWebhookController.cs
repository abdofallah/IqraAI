using IqraCore.Entities.Helper.Telephony;
using IqraCore.Models.Telephony;
using IqraInfrastructure.Managers.Call;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackendProxy.Controllers
{
    [ApiController]
    [Route("api/twilio/webhook")]
    public class TwilioWebhookController : ControllerBase
    {
        private readonly ILogger<TwilioWebhookController> _logger;
        private readonly InboundCallManager _inboundCallManager;
        private readonly CallStatusManager _callStatusManager;

        public TwilioWebhookController(
            ILogger<TwilioWebhookController> logger,
            InboundCallManager inboundCallManager,
            CallStatusManager callStatusManager
        )
        {
            _logger = logger;
            _inboundCallManager = inboundCallManager;
            _callStatusManager = callStatusManager;
        }

        [HttpPost("status/{businessId}/{phoneNumberId}")]
        public async Task<IActionResult> HandleStatusWebhook([FromBody] TwilioWebhookDataModel webhookData, [FromRoute] long businessId, [FromRoute] string phoneNumberId)
        {
            if (businessId < 0 || string.IsNullOrWhiteSpace(phoneNumberId) || webhookData == null)
            {
                return BadRequest("Invalid request parameters");
            }

            var webhookContext = new TelephonyWebhookContextModel
            {
                Provider = TelephonyProviderEnum.ModemTel,
                CallId = webhookData.CallSid,
                BusinessId = businessId,
                PhoneNumberId = phoneNumberId,
                To = webhookData.To,
                From = webhookData.From,
                Direction = webhookData.Direction == "inbound" ? "inbound" : "outbound"
            };

            switch (webhookData.CallStatus?.ToLower())
            {
                case "incoming":
                    {
                        var distributionResult = await _inboundCallManager.DistributeIncomingCall(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return Ok(@$"<?xml version=""1.0"" encoding=""UTF-8""?><Response><Say>Hey there! We are currently at capacity or facing some issues. Please try again later.</Say><Hangup /></Response>");
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