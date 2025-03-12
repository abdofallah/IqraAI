using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Telephony.Call;
using IqraInfrastructure.Repositories.Telephony;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Telephony;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using IqraCore.Models.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Business;
using IqraInfrastructure.Managers.Integrations;
using System.Net.Http.Headers;
using IqraInfrastructure.Managers.Region;

namespace IqraInfrastructure.Managers.Server
{
    public class OutboundCallManager
    {
        private readonly ILogger<OutboundCallManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly BusinessManager _businessManager;
        private readonly ServerSelectionManager _serverSelectionService;
        private readonly CallQueueRepository _callQueueRepository;
        private readonly ModemTelManager _modemTelManager;
        private readonly TwilioManager _twilioManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly RegionManager _regionManager;

        public OutboundCallManager(
            ILogger<OutboundCallManager> logger,
            IHttpClientFactory httpClientFactory,

            BusinessManager businessManager,
            ServerSelectionManager serverSelectionService,
            CallQueueRepository callQueueRepository,
            ModemTelManager modemTelManager,
            TwilioManager twilioManager,
            IntegrationsManager integrationsManager,
            RegionManager regionManager
        )
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _businessManager = businessManager;
            _serverSelectionService = serverSelectionService;
            _callQueueRepository = callQueueRepository;
            _modemTelManager = modemTelManager;
            _integrationsManager = integrationsManager;
            _regionManager = regionManager;
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
                var numberValidation = await ValidatePhoneNumberAsync(request.BusinessId, request.PhoneNumberId);
                if (!numberValidation.Success)
                {
                    result.Message = numberValidation.Message;
                    _logger.LogWarning("Phone number validation failed: {Message}", numberValidation.Message);
                    return result;
                }

                // TODO - Disabled for now as business plans are not yet implemented
                //// 3. Check concurrent call limits
                //var planValidation = await _businessPlanManager.ValidateCallLimitsAsync(
                //    request.BusinessId, isOutbound: true);
                //if (!planValidation.Success)
                //{
                //    result.Message = planValidation.Message;
                //    _logger.LogWarning("Business plan validation failed: {Message}", planValidation.Message);
                //    return result;
                //}

                // 4. Select optimal server
                var serverSelection = await _serverSelectionService.SelectOptimalServerAsync(numberValidation.Data.RegionId);
                if (!serverSelection.Success)
                {
                    result.Message = serverSelection.Message;
                    _logger.LogWarning("Server selection failed: {Message}", serverSelection.Message);
                    return result;
                }

                // 5. Get Server API Key
                var regionData = await _regionManager.GetRegionById(numberValidation.Data.RegionId);
                if (regionData == null)
                {
                    result.Message = "Unable to get region data";
                    _logger.LogWarning("Unable to get region data for region {RegionId}", numberValidation.Data.RegionId);
                    return result;
                }
                var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == serverSelection.ServerEndpoint);
                if (regionServerData == null)
                {
                    result.Message = "Unable to get region server data";
                    _logger.LogWarning("Unable to get region server data for region {RegionId} and endpoint {Endpoint}", numberValidation.Data.RegionId, serverSelection.ServerEndpoint);
                    return result;
                }
                var serverApiKey = regionServerData.APIKey;

                // 6. Create call queue entry
                var callQueue = new CallQueueData
                {
                    BusinessId = request.BusinessId,
                    RegionId = numberValidation.Data.RegionId,
                    NumberId = request.PhoneNumberId,
                    RouteId = numberValidation.Data.RouteId,
                    Provider = numberValidation.Data.Provider,
                    CallerNumber = numberValidation.Data.Number,
                    Priority = 1, // Normal priority for outbound calls
                    IsOutbound = true,
                    ProcessingServerId = serverSelection.ServerId,
                    ProviderMetadata = request.Metadata ?? new Dictionary<string, string>()
                };

                string queueId = await _callQueueRepository.EnqueueCallAsync(callQueue);

                // 6. Initiate the outbound call through the backend app
                var initiateResult = await InitiateCallThroughBackendAsync(
                    serverSelection.ServerEndpoint,
                    serverApiKey,
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

        private async Task<FunctionReturnResult<BusinessNumberData?>> ValidatePhoneNumberAsync(long businessId, string phoneNumberId)
        {
            var result = new FunctionReturnResult<BusinessNumberData?>();

            try
            {
                // Check if the phone number exists and belongs to the business
                var businessNumber = await _businessManager.GetNumberManager().GetBusinessNumberById(businessId, phoneNumberId);
                if (businessNumber == null)
                {
                    result.Message = "Phone number not found or does not belong to the business";
                    return result;
                }

                if (businessNumber.Provider == TelephonyProviderEnum.Unknown)
                {
                    result.Message = "Unknown phone number provider";
                    return result;
                }

                var numberIntegrationData = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(businessId, businessNumber.IntegrationId);
                if (!numberIntegrationData.Success)
                {
                    result.Message = numberIntegrationData.Message;
                    return result;
                }

                // Verify the number's capabilities with the provider
                if (businessNumber.Provider == TelephonyProviderEnum.ModemTel)
                {
                    var apiKey = _integrationsManager.DecryptField(numberIntegrationData.Data.EncryptedFields["apikey"]);
                    var apiEndpoint = numberIntegrationData.Data.Fields["endpoint"];

                    // Get the phone number details to use for caller ID
                    var numberDetailsResult = await _modemTelManager.GetPhoneNumberDetailsAsync(apiKey, apiEndpoint, ((BusinessNumberModemTelData)businessNumber).ModemTelPhoneNumberId);
                    if (!numberDetailsResult.Success || numberDetailsResult.Data == null)
                    {
                        result.Message = $"Unable to get phone number details: {numberDetailsResult.Message}";
                        return result;
                    }

                    if (!numberDetailsResult.Data.IsActive)
                    {
                        result.Message = "Phone number is not active";
                        return result;
                    }

                    if (!numberDetailsResult.Data.CanMakeCalls)
                    {
                        result.Message = "Phone number cannot make calls";
                        return result;
                    }

                    result.Data = businessNumber;
                    result.Success = true;
                    return result;
                }
                else if (businessNumber.Provider == TelephonyProviderEnum.Twilio)
                {
                    string accountSid = numberIntegrationData.Data.Fields["accountsid"];
                    string authToken = _integrationsManager.DecryptField(numberIntegrationData.Data.EncryptedFields["authToken"]);

                    // Get the number details from Twilio
                    var numberDetailsResult = await _twilioManager.GetPhoneNumberDetailsAsync(accountSid, authToken, ((BusinessNumberTwilioData)businessNumber).TwilioPhoneNumberId);
                    if (!numberDetailsResult.Success || numberDetailsResult.Data == null)
                    {
                        _logger.LogWarning("Failed to get phone number details from Twilio: {Message}", numberDetailsResult.Message);
                        return result;
                    }

                    if (numberDetailsResult.Data.Status.Replace("_", "") != "in use")
                    {
                        _logger.LogWarning("Twilio phone number is not active: {businessId}/{PhoneNumberId}", businessId, phoneNumberId);
                        return result;
                    }

                    if (!numberDetailsResult.Data.Capabilities.Voice)
                    {
                        _logger.LogWarning("Twilio phone number does not support voice calls: {businessId}/{PhoneNumberId}", businessId, phoneNumberId);
                        return result;
                    }

                    result.Data = businessNumber;
                    result.Success = true;
                    return result;
                }

                // For other providers, implement similar validation
                result.Message = $"Unsupported provider: {businessNumber.Provider}";
                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error validating phone number: {ex.Message}";
                _logger.LogError(ex, "Error validating phone number");
                return result;
            }
        }

        private async Task<BackendInitiateCallResult> InitiateCallThroughBackendAsync(string serverEndpoint, string serverApiKey, CallQueueData callQueue, string toNumber)
        {
            var result = new BackendInitiateCallResult();

            try
            {
                // Create HttpClient
                using var client = _httpClientFactory.CreateClient();

                // Set headers
                client.DefaultRequestHeaders.Add("X-API-Key", serverApiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
                var responseData = JsonSerializer.Deserialize<BackendOutboundCallResponse>(responseContent);

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
    }
}