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

        [HttpPost("voice/status/{businessId}/{phoneNumberId}")]
        public async Task<IActionResult> HandleStatusWebhook([FromForm] TwilioWebhookDataModel webhookData, [FromRoute] long businessId, [FromRoute] string phoneNumberId)
        {
            if (businessId < 0 || string.IsNullOrWhiteSpace(phoneNumberId) || webhookData == null)
            {
                return BadRequest("Invalid request parameters");
            }

            var webhookContext = new TelephonyWebhookContextModel
            {
                Provider = TelephonyProviderEnum.Twilio,
                CallId = webhookData.CallSid,
                BusinessId = businessId,
                PhoneNumberId = phoneNumberId,
                To = webhookData.To,
                From = webhookData.From,
                Direction = webhookData.Direction == "inbound" ? "inbound" : "outbound"
            };

            switch (webhookData.CallStatus?.ToLower())
            {
                case "no-answer":
                case "busy":
                    {
                        // probably the incoming call was missed
                        // future ask user what to do with missed calls

                        return Ok();
                    }

                case "in-progress":
                    {
                        var distributionResult = await _callStatusManager.NotifyInboundCallStarted(webhookContext);
                        if (!distributionResult.Success)
                        {
                            return NoContent();
                        }

                        return Ok();
                    }

                case "completed":
                    {
                        var distributionResult = await _callStatusManager.NotifyInboundCallEnded(webhookContext);
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

        [HttpPost("voice/incoming/{businessId}/{phoneNumberId}")]
        public async Task<IActionResult> HandleIncomingWebhook([FromForm] TwilioWebhookDataModel webhookData, [FromRoute] long businessId, [FromRoute] string phoneNumberId)
        {
            if (businessId < 0 || string.IsNullOrWhiteSpace(phoneNumberId) || webhookData == null)
            {
                return BadRequest("Invalid request parameters");
            }

            var webhookContext = new TelephonyWebhookContextModel
            {
                Provider = TelephonyProviderEnum.Twilio,
                CallId = webhookData.CallSid,
                BusinessId = businessId,
                PhoneNumberId = phoneNumberId,
                To = webhookData.To,
                From = webhookData.From,
                Direction = webhookData.Direction == "inbound" ? "inbound" : "outbound"
            };

            var form = Request.Form;

            var distributionResult = await _inboundCallManager.DistributeIncomingCall(webhookContext);
            if (!distributionResult.Success)
            {
                return Ok(); // for debug do not pick up calls
                // todo ask user what to do with failed calls
                return Ok(@$"<?xml version=""1.0"" encoding=""UTF-8""?><Response><Say language=""en_US"" voice=""lessac"">Hey there! We are currently at capacity or facing some issues. Please try again later.</Say><Hangup /></Response>");
            }

            distributionResult.Data.WebhookUrl = distributionResult.Data.WebhookUrl.Replace("192.168.100.9:5250", "iqrabackend.om-mct-s-dev.ddns.iqra.bot/devserver").Replace("ws://", "wss://");

            return Ok(@$"<?xml version=""1.0"" encoding=""UTF-8""?><Response><Connect><Stream url=""{distributionResult.Data.WebhookUrl}"" /></Connect><Say>.</Say></Response>");
        }

        [HttpPost("voice/session/incoming/{sessionId}")]
        public async Task<IActionResult> HandleSessionIncomingWebhook([FromBody] TwilioWebhookDataModel webhookData, [FromRoute] string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || webhookData == null)
            {
                return BadRequest("Invalid request parameters");
            }

            var webhookContext = new TelephonyWebhookContextModel
            {
                Provider = TelephonyProviderEnum.Twilio,
                CallId = webhookData.CallSid,
                To = webhookData.To,
                From = webhookData.From,
                Direction = webhookData.Direction == "inbound" ? "inbound" : "outbound"
            };

            var distributionResult = await _callStatusManager.NotifyOutboundCallStatus(sessionId, webhookContext, webhookData.CallStatus?.ToLower());
            if (!distributionResult.Success)
            {
                return NoContent();
            }

            return Ok();
        }
    }
}