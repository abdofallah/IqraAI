using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Telephony.Call;
using IqraInfrastructure.Repositories.Telephony;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Telephony;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using IqraCore.Models.Server;

namespace IqraInfrastructure.Managers.Server
{
    public class OutboundCallManager
    {
        private readonly ILogger<OutboundCallManager> _logger;
        private readonly BusinessManager _businessManager;
        private readonly BusinessPlanService _businessPlanService;
        private readonly ServerSelectionManager _serverSelectionService;
        private readonly CallQueueRepository _callQueueRepository;
        private readonly ModemTelManager _modemTelManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public OutboundCallManager(
            ILogger<OutboundCallManager> logger,
            BusinessManager businessManager,
            BusinessPlanService businessPlanService,
            ServerSelectionManager serverSelectionService,
            CallQueueRepository callQueueRepository,
            ModemTelManager modemTelManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _businessManager = businessManager;
            _businessPlanService = businessPlanService;
            _serverSelectionService = serverSelectionService;
            _callQueueRepository = callQueueRepository;
            _modemTelManager = modemTelManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<OutboundCallServiceResult> InitiateOutboundCallAsync(OutboundCallRequestModel request)
        {
            var result = new OutboundCallServiceResult();

            try
            {
                // 1. Validate business and permissions
                var businessResult = await _businessManager.GetUserBusinessById(request.BusinessId, "InitiateOutboundCallAsync");
                if (!businessResult.Success || businessResult.Data == null)
                {
                    result.Message = $"Business not found: {request.BusinessId}";
                    _logger.LogWarning("Business not found: {BusinessId}", request.BusinessId);
                    return result;
                }

                var business = businessResult.Data;

                // Check if business is active
                if (business.Permission.DisabledFullAt != null)
                {
                    result.Message = "Business is disabled";
                    _logger.LogWarning("Business {BusinessId} is disabled", request.BusinessId);
                    return result;
                }

                // Check outbound call permission
                if (business.Permission.MakeCall.DisabledCallingAt != null)
                {
                    result.Message = "Outbound calls are disabled for this business";
                    _logger.LogWarning("Outbound calls are disabled for business {BusinessId}", request.BusinessId);
                    return result;
                }

                // 2. Verify number ownership and capabilities
                var numberValidation = await ValidatePhoneNumberAsync(
                    request.BusinessId, request.PhoneNumberId);
                
                if (!numberValidation.Success)
                {
                    result.Message = numberValidation.Message;
                    _logger.LogWarning("Phone number validation failed: {Message}", numberValidation.Message);
                    return result;
                }

                // 3. Check concurrent call limits
                var planValidation = await _businessPlanService.ValidateCallLimitsAsync(
                    request.BusinessId, isOutbound: true);
                
                if (!planValidation.Success)
                {
                    result.Message = planValidation.Message;
                    _logger.LogWarning("Business plan validation failed: {Message}", planValidation.Message);
                    return result;
                }

                // 4. Determine region if not specified
                string regionId = request.RegionId ?? await GetPhoneNumberRegion(request.BusinessId, request.PhoneNumberId);

                // 5. Select optimal server
                var serverSelection = await _serverSelectionService.SelectOptimalServerAsync(regionId, request.BusinessId);
                if (!serverSelection.Success)
                {
                    result.Message = serverSelection.Message;
                    _logger.LogWarning("Server selection failed: {Message}", serverSelection.Message);
                    return result;
                }

                // 6. Create call queue entry
                var callQueue = new CallQueueData
                {
                    BusinessId = request.BusinessId,
                    RegionId = regionId,
                    NumberId = request.PhoneNumberId,
                    RouteId = request.RouteId,
                    Provider = TelephonyProviderEnum.ModemTel, // Hardcoded for now
                    CallerNumber = numberValidation.PhoneNumber,
                    Priority = 2, // Higher priority for outbound calls
                    IsOutbound = true,
                    ProcessingServerId = serverSelection.ServerId,
                    ProviderMetadata = request.Metadata ?? new Dictionary<string, string>()
                };

                string queueId = await _callQueueRepository.EnqueueCallAsync(callQueue);

                // 7. Initiate the outbound call through the backend app
                var initiateResult = await InitiateCallThroughBackendAsync(
                    serverSelection.ServerEndpoint, 
                    callQueue,
                    request.ToNumber);

                if (!initiateResult.Success)
                {
                    // Mark queue entry as failed
                    await _callQueueRepository.MarkCallAsCompletedAsync(queueId, false);
                    
                    result.Message = initiateResult.Message;
                    _logger.LogError("Call initiation failed: {Message}", initiateResult.Message);
                    return result;
                }

                // Return success result
                result.Success = true;
                result.QueueId = queueId;
                result.CallId = initiateResult.CallId;
                result.Status = initiateResult.Status;

                _logger.LogInformation("Outbound call initiated: QueueId={QueueId}, CallId={CallId}",
                    queueId, initiateResult.CallId);

                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error initiating outbound call: {ex.Message}";
                _logger.LogError(ex, "Error initiating outbound call");
                return result;
            }
        }

        private async Task<PhoneNumberValidationResult> ValidatePhoneNumberAsync(
            long businessId, string phoneNumberId)
        {
            var result = new PhoneNumberValidationResult();

            try
            {
                // Check if the phone number exists and belongs to the business
                var businessNumber = await _businessManager.GetNumberManager()
                    .GetBusinessNumberById(businessId, phoneNumberId);

                if (businessNumber == null)
                {
                    result.Message = "Phone number not found or does not belong to the business";
                    return result;
                }

                // Verify the number's capabilities with the provider
                if (businessNumber.Provider == TelephonyProviderEnum.ModemTel)
                {
                    // Get the ModemTel credentials from configuration
                    string apiKey = _configuration["ModemTel:ApiKey"];
                    string apiBaseUrl = _configuration["ModemTel:ApiBaseUrl"];

                    var numberValidation = await _modemTelManager.ValidatePhoneNumberAsync(
                        apiKey, apiBaseUrl, businessNumber.Id, requireCallCapability: true);

                    if (!numberValidation.Success)
                    {
                        result.Message = $"Phone number validation failed: {numberValidation.Message}";
                        return result;
                    }

                    // Get the phone number details to use for caller ID
                    var numberDetailsResult = await _modemTelManager.GetPhoneNumberDetailsAsync(
                        apiKey, apiBaseUrl, businessNumber.Id);

                    if (!numberDetailsResult.Success || numberDetailsResult.Data == null)
                    {
                        result.Message = $"Unable to get phone number details: {numberDetailsResult.Message}";
                        return result;
                    }

                    result.PhoneNumber = $"{numberDetailsResult.Data.CountryCode}{numberDetailsResult.Data.Number}";
                }
                else
                {
                    // For other providers, implement similar validation
                    result.Message = $"Unsupported provider: {businessNumber.Provider}";
                    return result;
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error validating phone number: {ex.Message}";
                _logger.LogError(ex, "Error validating phone number");
                return result;
            }
        }

        private async Task<string> GetPhoneNumberRegion(long businessId, string phoneNumberId)
        {
            try
            {
                var businessNumber = await _businessManager.GetNumberManager()
                    .GetBusinessNumberById(businessId, phoneNumberId);

                if (businessNumber != null && !string.IsNullOrEmpty(businessNumber.RegionId))
                {
                    return businessNumber.RegionId;
                }

                // Default region if none specified
                return _configuration["DefaultRegion"] ?? "OM-MCT";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting phone number region");
                return _configuration["DefaultRegion"] ?? "OM-MCT";
            }
        }

        private async Task<InitiateCallResult> InitiateCallThroughBackendAsync(
            string serverEndpoint, 
            CallQueueData callQueue,
            string toNumber)
        {
            var result = new InitiateCallResult();

            try
            {
                // Create HttpClient
                using var client = _httpClientFactory.CreateClient();
                
                // Get the API key for backend authentication
                string apiKey = _configuration["Security:BackendApiKey"];
                
                // Set headers
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Prepare request body
                var requestBody = new
                {
                    QueueId = callQueue.Id,
                    BusinessId = callQueue.BusinessId,
                    PhoneNumberId = callQueue.NumberId,
                    ToNumber = toNumber,
                    RouteId = callQueue.RouteId,
                    Metadata = callQueue.ProviderMetadata
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Send request to backend
                var response = await client.PostAsync(
                    $"{serverEndpoint}/api/call/outbound",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.Message = $"Backend server returned {response.StatusCode}: {errorContent}";
                    return result;
                }

                // Parse response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<OutboundCallResponse>(responseContent);

                if (responseData == null || string.IsNullOrEmpty(responseData.CallId))
                {
                    result.Message = "Invalid response from backend server";
                    return result;
                }

                result.Success = true;
                result.CallId = responseData.CallId;
                result.Status = responseData.Status;
                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error initiating call through backend: {ex.Message}";
                return result;
            }
        }

        private class PhoneNumberValidationResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
        }

        private class InitiateCallResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string CallId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        private class OutboundCallResponse
        {
            public string CallId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
    }

    public class OutboundCallServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string QueueId { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}