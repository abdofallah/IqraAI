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
        private readonly InboundCallManager _callDistributionManager;

        public TwilioWebhookController(
            ILogger<TwilioWebhookController> logger,
            InboundCallManager callDistributionManager
        )
        {
            _logger = logger;
            _callDistributionManager = callDistributionManager;
        }

        [HttpPost("incoming/{businessId}/{phoneNumberId}")]
        public async Task<IActionResult> HandleIncomingCall([FromForm] TwilioWebhookDataModel webhookData, long businessId, string phoneNumberId)
        {
            // Validate business and phone number
            if (businessId <= 0 || string.IsNullOrWhiteSpace(phoneNumberId))
            {
                _logger.LogWarning("Invalid request parameters for Twilio webhook: {BusinessId}/{PhoneNumberId}", businessId, phoneNumberId);
                return BadRequest("Invalid request parameters");
            }

            _logger.LogInformation("Received Twilio webhook for {BusinessId}/{PhoneNumberId}, CallSid: {CallSid}", businessId, phoneNumberId, webhookData.CallSid);

            // Validate the webhook signature
            if (!ValidateTwilioSignature())
            {
                _logger.LogWarning("Invalid Twilio signature from {IP} for {BusinessId}/{PhoneNumberId}", HttpContext.Connection.RemoteIpAddress, businessId, phoneNumberId);
                return Unauthorized("Invalid signature");
            }

            try
            {
                // Prepare the webhook context
                var webhookContext = new TelephonyWebhookContextModel
                {
                    Provider = TelephonyProviderEnum.Twilio,
                    CallId = webhookData.CallSid,
                    BusinessId = businessId,
                    PhoneNumberId = phoneNumberId,
                    To = webhookData.To,
                    From = webhookData.From,
                    AdditionalData = new Dictionary<string, string>
                    {
                        ["direction"] = webhookData.Direction,
                        ["callStatus"] = webhookData.CallStatus,
                        ["accountSid"] = webhookData.AccountSid,
                        ["apiVersion"] = webhookData.ApiVersion
                    }
                };

                // Process the call through our distribution manager
                var distributionResult = await _callDistributionManager.DistributeIncomingCall(webhookContext);
                if (!distributionResult.Success)
                {
                    _logger.LogWarning("Failed to distribute Twilio call {CallSid}: {Message}", webhookData.CallSid, distributionResult.Message);

                    // Return TwiML response for failure
                    return Content(TwiMLForFailure, "application/xml");
                }

                // Get the backend server URL for redirect
                var backendUrl = GetBackendUrlForRedirect(distributionResult.Data.BackendServerId, webhookData.CallSid);

                _logger.LogInformation("Twilio call {CallSid} routed to {BackendUrl}", webhookData.CallSid, backendUrl);

                // Return TwiML response with redirect
                return Content(GenerateTwiMLForRedirect(backendUrl), "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Twilio webhook for call {CallSid}", webhookData.CallSid);
                return Content(TwiMLForError, "application/xml");
            }
        }

        [HttpPost("status/{businessId}/{phoneNumberId}")]
        public async Task<IActionResult> HandleCallStatus([FromForm] TwilioStatusCallbackDataModel statusData, long businessId, string phoneNumberId)
        {
            // Validate the webhook signature
            if (!ValidateTwilioSignature())
            {
                _logger.LogWarning("Invalid Twilio signature for status callback from {IP}", HttpContext.Connection.RemoteIpAddress);
                return Unauthorized("Invalid signature");
            }

            _logger.LogInformation("Received Twilio status callback: {CallSid}, Status: {CallStatus}", statusData.CallSid, statusData.CallStatus);

            // Check if this is a call completion status
            if (statusData.CallStatus == "completed" || 
                statusData.CallStatus == "failed" || 
                statusData.CallStatus == "busy" || 
                statusData.CallStatus == "no-answer" || 
                statusData.CallStatus == "canceled")
            {
                await _callDistributionManager.NotifyCallEnded(statusData.CallSid, TelephonyProviderEnum.Twilio, businessId, phoneNumberId);
            }

            return Ok();
        }

        private bool ValidateTwilioSignature()
        {
            // temporary placeholder function for now
            return true;

            // In future implement the user to generate their own api key or certificate
            // validate against the phone number webhook's api key or certificate with the one recieved in the request
        }

        private string GetBackendUrlForRedirect(string backendServerEndpoint, string callSid)
        {
            var baseUri = new Uri(backendServerEndpoint);
            return new Uri(baseUri, $"/api/twilio/call/{callSid}").ToString();
        }

        private string GenerateTwiMLForRedirect(string redirectUrl)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
            <Response>
                <Redirect method=""POST"">{redirectUrl}</Redirect>
            </Response>";
        }

        private static string TwiMLForFailure = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <Response>
                <Say>We're sorry, but all of our agents are currently busy. Please try your call again later.</Say>
                <Hangup />
            </Response>";

        private static string TwiMLForError = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <Response>
                <Say>We're sorry, but we encountered a problem with your call. Please try again later.</Say>
                <Hangup />
            </Response>";
    }
}