using IqraCore.Entities.Helpers;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Server;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraBackendProxy.Controllers
{
    [ApiController]
    [Route("api/call/outbound")]
    public class OutboundCallController : ControllerBase
    {
        private readonly ILogger<OutboundCallController> _logger;
        private readonly OutboundCallManager _outboundCallManager;
        private readonly IConfiguration _configuration;

        public OutboundCallController(
            ILogger<OutboundCallController> logger,
            OutboundCallManager outboundCallService,
            IConfiguration configuration)
        {
            _logger = logger;
            _outboundCallManager = outboundCallService;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult<FunctionReturnResult<OutboundCallResultModel>>> InitiateOutboundCall([FromBody] OutboundCallRequestModel request)
        {
            var result = new FunctionReturnResult<OutboundCallResultModel>();

            // Validate the API key
            if (!ValidateApiKey())
            {
                result.Code = "InitiateOutboundCall:1";
                result.Message = "Invalid API key";
                return Unauthorized(result);
            }

            // Basic request validation
            if (request == null || request.BusinessId <= 0 || string.IsNullOrEmpty(request.PhoneNumberId) || string.IsNullOrEmpty(request.ToNumber) || string.IsNullOrEmpty(request.CallConfigurationId))
            {
                result.Code = "InitiateOutboundCall:2";
                result.Message = "Invalid request parameters";
                return BadRequest(result);
            }

            try
            {
                // Initiate the outbound call
                var callResult = await _outboundCallManager.InitiateOutboundCallAsync(request);

                if (!callResult.Success)
                {
                    result.Code = "InitiateOutboundCall:3";
                    result.Message = callResult.Message;
                    return BadRequest(result);
                }

                // Return success result
                result.Success = true;
                result.Data = new OutboundCallResultModel
                {
                    QueueId = callResult.QueueId,
                    CallId = callResult.CallId,
                    Status = callResult.Status
                };

                _logger.LogInformation("Outbound call initiated: {QueueId}", callResult.QueueId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                result.Code = "InitiateOutboundCall:4";
                result.Message = $"Error initiating outbound call: {ex.Message}";
                _logger.LogError(ex, "Error initiating outbound call");
                return StatusCode(500, result);
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
    }  
}