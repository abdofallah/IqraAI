using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Telephony.Call;
using IqraInfrastructure.Repositories.Telephony;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Telephony;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using IqraCore.Models.Telephony;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Integrations;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Region;
using IqraCore.Entities.Business;

namespace IqraInfrastructure.Managers.Server
{
    public class InboundCallManager
    {
        private readonly ILogger<InboundCallManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly CallQueueRepository _callQueueRepository;
        private readonly ServerSelectionManager _serverSelectionService;
        private readonly BusinessManager _businessManager;
        private readonly ModemTelManager _modemTelManager;
        private readonly TwilioManager _twilioManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly RegionManager _regionManager;

        public InboundCallManager(
            ILogger<InboundCallManager> logger,
            IHttpClientFactory httpClientFactory,

            CallQueueRepository callQueueRepository,
            ServerSelectionManager serverSelectionService,
            BusinessManager businessManager,
            ModemTelManager modemTelManager,
            TwilioManager twilioManager,
            IntegrationsManager integrationsManager,
            RegionManager regionManager
        )
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _callQueueRepository = callQueueRepository;
            _serverSelectionService = serverSelectionService;
            _businessManager = businessManager;
            _modemTelManager = modemTelManager;
            _twilioManager = twilioManager;
            _integrationsManager = integrationsManager;
            _regionManager = regionManager;
        }

        public async Task<FunctionReturnResult<DistributionResultModel?>> DistributeIncomingCall(TelephonyWebhookContextModel webhookContext)
        {
            var result = new FunctionReturnResult<DistributionResultModel?>();

            try
            {
                // 1. Identify business and number information from webhook
                var phoneNumberInfo = await GetPhoneNumberInfo(webhookContext);
                if (phoneNumberInfo == null)
                {
                    result.Message = "Unable to identify phone number";
                    _logger.LogWarning("Unable to identify phone number for call {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", webhookContext.CallId, webhookContext.Provider, webhookContext.BusinessId, webhookContext.PhoneNumberId);
                    return result;
                }

                long businessId = phoneNumberInfo.BusinessId;
                string? numberRouteId = phoneNumberInfo.RouteId;
                string regionId = phoneNumberInfo.RegionId;

                // Ignore the call for now if the business number has no route
                // TODO in future if phone number has options of what to do in case of no route, set it here
                if (string.IsNullOrWhiteSpace(numberRouteId))
                {
                    result.Message = "Business number has no route set";
                    _logger.LogWarning("Business {BusinessId} phone number {PhoneNumberId} has no route set", businessId, phoneNumberInfo.NumberId);
                    return result;
                }

                // TODO - Disabled for now as business plans are not yet implemented
                //// 2. Validate business plan and concurrent call limits
                //var planValidation = await _businessPlanManager.ValidateCallLimitsAsync(businessId);
                //if (!planValidation.Success)
                //{
                //    result.Message = planValidation.Message;
                //    _logger.LogWarning("Business plan validation failed for call {CallId}: {Message}",
                //        webhookContext.CallId, planValidation.Message);
                //    return result;
                //}

                // 3. Select optimal server
                var serverSelection = await _serverSelectionService.SelectOptimalServerAsync(regionId);
                if (!serverSelection.Success)
                {
                    result.Message = serverSelection.Message;
                    _logger.LogWarning("Server selection failed for call {CallId} for provider {Provider} in {businessId}/{phoneNumberId}: {Message}",
                        webhookContext.CallId, webhookContext.Provider, webhookContext.BusinessId, webhookContext.PhoneNumberId, serverSelection.Message);
                    return result;
                }

                // Get Region Api key
                var regionData = await _regionManager.GetRegionById(regionId);
                if (regionData == null)
                {
                    result.Message = $"Region not found: {regionId}";
                    _logger.LogWarning("Region not found: {RegionId}", regionId);
                    return result;
                }
                var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == serverSelection.ServerEndpoint);
                if (regionServerData == null)
                {
                    result.Message = $"Region server not found: {serverSelection.ServerEndpoint}";
                    _logger.LogWarning("Region server not found: {ServerEndpoint}", serverSelection.ServerEndpoint);
                    return result;
                }
                var regionServerApiKey = regionServerData.APIKey;

                // 4. Create call queue entry
                var callQueue = new CallQueueData
                {
                    BusinessId = businessId,
                    RegionId = regionId,
                    NumberId = phoneNumberInfo.NumberId,
                    RouteId = numberRouteId,
                    Provider = webhookContext.Provider,
                    ProviderCallId = webhookContext.CallId,
                    CallerNumber = webhookContext.From,
                    Priority = 2, // High priority for incoming calls
                    IsOutbound = false,
                    ProcessingServerId = serverSelection.ServerId,
                    ProviderMetadata = webhookContext.AdditionalData
                };

                string queueId = await _callQueueRepository.EnqueueCallAsync(callQueue);

                // 5. Forward call to selected backend server
                var forwardResult = await ForwardCallToBackendAsync(serverSelection.ServerEndpoint, regionServerApiKey, webhookContext, callQueue);

                if (!forwardResult.Success)
                {
                    // If forwarding fails, mark the queue entry as failed
                    await _callQueueRepository.MarkCallAsCompletedAsync(queueId, false);
                    
                    result.Message = forwardResult.Message;
                    _logger.LogError("Call forwarding failed for call {CallId} with queue {queueId}: {Message}",
                        webhookContext.CallId, queueId, forwardResult.Message);
                    return result;
                }

                // 6. Return success result
                result.Success = true;
                result.Data = new DistributionResultModel()
                {
                    QueueId = queueId,
                    BackendServerId = serverSelection.ServerId
                };

                _logger.LogInformation("Call {CallId} distributed to server {ServerId} with queue ID {QueueId}",
                    webhookContext.CallId, serverSelection.ServerId, queueId);

                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error distributing call: {ex.Message}";
                _logger.LogError(ex, "Error distributing call {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", webhookContext.CallId, webhookContext.Provider, webhookContext.BusinessId, webhookContext.PhoneNumberId);
                return result;
            }
        }

        public async Task NotifyCallEnded(string callId, TelephonyProviderEnum provider, long businessId, string phoneNumberId)
        {
            try
            {
                // Find the call in the queue
                var callQueue = await _callQueueRepository.GetCallByProviderCallIdAsync(provider, callId, businessId, phoneNumberId);
                if (callQueue == null)
                {
                    _logger.LogWarning("Call not found in queue for end notification: {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", callId, provider, businessId, phoneNumberId);
                    return;
                }

                // Mark the call as completed
                await _callQueueRepository.MarkCallAsCompletedAsync(callQueue.Id, true);

                // If the call has a session ID, notify the backend app
                if (!string.IsNullOrEmpty(callQueue.SessionId) && !string.IsNullOrEmpty(callQueue.ProcessingServerId))
                {
                    // Get Region Api key
                    var regionData = await _regionManager.GetRegionById(callQueue.RegionId);
                    if (regionData == null)
                    {
                        _logger.LogWarning("Region not found: {RegionId}", callQueue.RegionId);
                        return;
                    }
                    var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == callQueue.ProcessingServerId);
                    if (regionServerData == null)
                    {
                        _logger.LogWarning("Region server not found: {ServerEndpoint}", callQueue.ProcessingServerId);
                        return;
                    }
                    var regionServerApiKey = regionServerData.APIKey;

                    await NotifyBackendCallEndedAsync(callQueue.ProcessingServerId, regionServerApiKey, callQueue.SessionId);
                }

                _logger.LogInformation("Call {CallId} marked as ended", callQueue.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing call end notification for {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", callId, provider, businessId, phoneNumberId);
            }
        }

        private async Task<PhoneNumberInfo?> GetPhoneNumberInfo(TelephonyWebhookContextModel webhookContext)
        {
            try
            {
                if (webhookContext.Provider == TelephonyProviderEnum.Unknown)
                {
                    _logger.LogWarning("Unknown provider");
                    return null;
                }

                var businessNumber = await _businessManager.GetNumberManager().GetBusinessNumberById(webhookContext.BusinessId, webhookContext.PhoneNumberId);
                if (businessNumber == null)
                {
                    _logger.LogWarning("No business found for ModemTel number ID {BusinessId}/{NumberId}", webhookContext.BusinessId, webhookContext.PhoneNumberId);
                    return null;
                }

                var integratonData = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(webhookContext.BusinessId, businessNumber.IntegrationId);
                if (integratonData == null)
                {
                    _logger.LogWarning("No integration found for ModemTel number ID {BusinessId}/{NumberId}", webhookContext.BusinessId, webhookContext.PhoneNumberId);
                    return null;
                }

                // ModemTel
                if (webhookContext.Provider == TelephonyProviderEnum.ModemTel)
                {
                    // Get the ModemTel credentials from integration fields
                    string apiKey = _integrationsManager.DecryptField(integratonData.Data.EncryptedFields["apikey"]);
                    string apiBaseUrl = integratonData.Data.Fields["endpoint"];

                    // Get the number details from ModemTel
                    var numberDetailsResult = await _modemTelManager.GetPhoneNumberDetailsAsync(apiKey, apiBaseUrl, ((BusinessNumberModemTelData)businessNumber).ModemTelPhoneNumberId);
                    if (!numberDetailsResult.Success || numberDetailsResult.Data == null)
                    {
                        _logger.LogWarning("Failed to get phone number details from ModemTel: {Message}", numberDetailsResult.Message);
                        return null;
                    }

                    if (!numberDetailsResult.Data.IsActive)
                    {
                        _logger.LogWarning("ModemTel number {businessId}/{NumberId} is not active", webhookContext.BusinessId, webhookContext.PhoneNumberId);
                        return null;
                    }

                    if (!numberDetailsResult.Data.CanMakeCalls)
                    {
                        _logger.LogWarning("ModemTel number {businessId}/{NumberId} cannot make calls", webhookContext.BusinessId, webhookContext.PhoneNumberId);
                        return null;
                    }

                    return new PhoneNumberInfo
                    {
                        BusinessId = webhookContext.BusinessId,
                        NumberId = businessNumber.Id,
                        RouteId = businessNumber.RouteId,
                        RegionId = businessNumber.RegionId
                    };
                }
                // Twilio
                else if (webhookContext.Provider == TelephonyProviderEnum.Twilio)
                {
                    string accountSid = integratonData.Data.Fields["accountsid"];
                    string authToken = _integrationsManager.DecryptField(integratonData.Data.EncryptedFields["authToken"]);

                    // Get the number details from Twilio
                    var numberDetailsResult = await _twilioManager.GetPhoneNumberDetailsAsync(accountSid, authToken, ((BusinessNumberTwilioData)businessNumber).TwilioPhoneNumberId);
                    if (!numberDetailsResult.Success || numberDetailsResult.Data == null)
                    {
                        _logger.LogWarning("Failed to get phone number details from Twilio: {Message}", numberDetailsResult.Message);
                        return null;
                    }

                    if (numberDetailsResult.Data.Status.Replace("_", "") != "in use")
                    {
                        _logger.LogWarning("Twilio phone number is not active: {businessId}/{PhoneNumberId}", webhookContext.BusinessId, webhookContext.PhoneNumberId);
                        return null;
                    }

                    if (!numberDetailsResult.Data.Capabilities.Voice)
                    {
                        _logger.LogWarning("Twilio phone number does not support voice calls: {businessId}/{PhoneNumberId}", webhookContext.BusinessId, webhookContext.PhoneNumberId);
                        return null;
                    }

                    return new PhoneNumberInfo
                    {
                        BusinessId = webhookContext.BusinessId,
                        NumberId = businessNumber.Id,
                        RouteId = businessNumber.RouteId,
                        RegionId = businessNumber.RegionId
                    };
                }
                else
                {
                    throw new NotImplementedException("GetPhoneNumberInfo not implemented for provider: " + webhookContext.Provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting phone number info");
                return null;
            }
        }

        private async Task<ForwardResult> ForwardCallToBackendAsync(string serverEndpoint, string apiKey, TelephonyWebhookContextModel webhookContext, CallQueueData callQueue)
        {
            var result = new ForwardResult();

            try
            {
                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient();
       
                // Set headers
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Prepare the request body
                var requestBody = new
                {
                    Provider = webhookContext.Provider.ToString(),
                    CallId = webhookContext.CallId,
                    QueueId = callQueue.Id,
                    BusinessId = callQueue.BusinessId,
                    PhoneNumberId = webhookContext.PhoneNumberId,
                    To = webhookContext.To,
                    From = webhookContext.From,
                    RouteId = callQueue.RouteId,
                    AdditionalData = webhookContext.AdditionalData
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Send the request to the backend app
                var response = await client.PostAsync($"{serverEndpoint}/api/call/incoming", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.Message = $"Backend server returned {response.StatusCode}: {errorContent}";
                    _logger.LogError("Backend server error: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                    return result;
                }

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<ForwardResponse>(responseContent);

                if (responseData == null || string.IsNullOrEmpty(responseData.MediaUrl))
                {
                    result.Message = "Invalid response from backend server";
                    _logger.LogError("Invalid response from backend server");
                    return result;
                }

                result.Success = true;
                result.MediaUrl = responseData.MediaUrl;
                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error forwarding call to backend: {ex.Message}";
                _logger.LogError(ex, "Error forwarding call to backend server");
                return result;
            }
        }

        private async Task NotifyBackendCallEndedAsync(string serverEndpoint, string apiKey,  string sessionId)
        {
            try
            {
                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient();    
                
                // Set headers
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                // Send the notification
                var response = await client.PostAsync(
                    $"{serverEndpoint}/api/call/{sessionId}/ended",
                    null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to notify backend of call end: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying backend of call end");
            }
        }

        private class PhoneNumberInfo
        {
            public long BusinessId { get; set; }
            public string NumberId { get; set; } = string.Empty;
            public string? RouteId { get; set; } = string.Empty;
            public string RegionId { get; set; } = string.Empty;
        }

        private class ForwardResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string MediaUrl { get; set; } = string.Empty;
        }

        private class ForwardResponse
        {
            public string MediaUrl { get; set; } = string.Empty;
            public string SessionId { get; set; } = string.Empty;
        }
    }
}