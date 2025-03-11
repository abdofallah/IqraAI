using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Telephony.Call;
using IqraInfrastructure.Repositories.Telephony;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Telephony;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using IqraCore.Models.Telephony;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Integrations;

namespace IqraInfrastructure.Managers.Server
{
    public class CallDistributionManager
    {
        private readonly ILogger<CallDistributionManager> _logger;
        private readonly CallQueueRepository _callQueueRepository;
        private readonly ServerSelectionManager _serverSelectionService;
        private readonly BusinessPlanService _businessPlanService;
        private readonly BusinessManager _businessManager;
        private readonly ModemTelManager _modemTelManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public CallDistributionManager(
            ILogger<CallDistributionManager> logger,
            CallQueueRepository callQueueRepository,
            ServerSelectionManager serverSelectionService,
            BusinessPlanService businessPlanService,
            BusinessManager businessManager,
            ModemTelManager modemTelManager,
            IntegrationsManager integrationsManager,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _callQueueRepository = callQueueRepository;
            _serverSelectionService = serverSelectionService;
            _businessPlanService = businessPlanService;
            _businessManager = businessManager;
            _modemTelManager = modemTelManager;
            _integrationsManager = integrationsManager;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<DistributionResultModel> DistributeIncomingCall(TelephonyWebhookContextModel webhookContext)
        {
            var result = new DistributionResultModel();

            try
            {
                // 1. Identify business and number information from webhook
                var phoneNumberInfo = await GetPhoneNumberInfo(webhookContext);
                if (phoneNumberInfo == null)
                {
                    result.Message = "Unable to identify phone number";
                    _logger.LogWarning("Unable to identify phone number for call {CallId}", webhookContext.CallId);
                    return result;
                }

                long businessId = phoneNumberInfo.BusinessId;
                string numberRouteId = phoneNumberInfo.RouteId;
                string regionId = phoneNumberInfo.RegionId;

                // 2. Validate business plan and concurrent call limits
                var planValidation = await _businessPlanService.ValidateCallLimitsAsync(businessId);
                if (!planValidation.Success)
                {
                    result.Message = planValidation.Message;
                    _logger.LogWarning("Business plan validation failed for call {CallId}: {Message}",
                        webhookContext.CallId, planValidation.Message);
                    return result;
                }

                // 3. Select optimal server
                var serverSelection = await _serverSelectionService.SelectOptimalServerAsync(regionId, businessId);
                if (!serverSelection.Success)
                {
                    result.Message = serverSelection.Message;
                    _logger.LogWarning("Server selection failed for call {CallId}: {Message}",
                        webhookContext.CallId, serverSelection.Message);
                    return result;
                }

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
                    Priority = 1, // Normal priority for incoming calls
                    IsOutbound = false,
                    ProcessingServerId = serverSelection.ServerId,
                    ProviderMetadata = webhookContext.AdditionalData
                };

                string queueId = await _callQueueRepository.EnqueueCallAsync(callQueue);

                // 5. Forward call to selected backend server
                var forwardResult = await ForwardCallToBackendAsync(
                    serverSelection.ServerEndpoint,
                    webhookContext,
                    callQueue);

                if (!forwardResult.Success)
                {
                    // If forwarding fails, mark the queue entry as failed
                    await _callQueueRepository.MarkCallAsCompletedAsync(queueId, false);
                    
                    result.Message = forwardResult.Message;
                    _logger.LogError("Call forwarding failed for call {CallId}: {Message}",
                        webhookContext.CallId, forwardResult.Message);
                    return result;
                }

                // 6. Return success result with media endpoint
                result.Success = true;
                result.QueueId = queueId;
                result.MediaUrl = forwardResult.MediaUrl;
                result.BackendServerId = serverSelection.ServerId;

                _logger.LogInformation("Call {CallId} distributed to server {ServerId} with queue ID {QueueId}",
                    webhookContext.CallId, serverSelection.ServerId, queueId);

                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error distributing call: {ex.Message}";
                _logger.LogError(ex, "Error distributing call {CallId}", webhookContext.CallId);
                return result;
            }
        }

        public async Task NotifyCallEnded(string callId, TelephonyProviderEnum provider)
        {
            try
            {
                // Find the call in the queue
                var callQueue = await _callQueueRepository.GetCallByProviderCallIdAsync(provider, callId);
                if (callQueue == null)
                {
                    _logger.LogWarning("Call not found in queue for end notification: {CallId}", callId);
                    return;
                }

                // Mark the call as completed
                await _callQueueRepository.MarkCallAsCompletedAsync(callQueue.Id, true);

                // If the call has a session ID, notify the backend app
                if (!string.IsNullOrEmpty(callQueue.SessionId) && !string.IsNullOrEmpty(callQueue.ProcessingServerId))
                {
                    await NotifyBackendCallEndedAsync(
                        callQueue.ProcessingServerId,
                        callQueue.SessionId
                    );
                }

                _logger.LogInformation("Call {CallId} marked as ended", callId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing call end notification for {CallId}", callId);
            }
        }

        private async Task<PhoneNumberInfo?> GetPhoneNumberInfo(TelephonyWebhookContextModel webhookContext)
        {
            try
            {
                // For ModemTel, use the PhoneNumberId from the webhook
                if (webhookContext.Provider == TelephonyProviderEnum.ModemTel && !string.IsNullOrEmpty(webhookContext.PhoneNumberId))
                {
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

                    // Get the ModemTel credentials from integration fields
                    string apiKey = _integrationsManager.DecryptField(integratonData.Data.EncryptedFields["apikey"]);
                    string apiBaseUrl = integratonData.Data.Fields["endpoint"];

                    // Get the number details from ModemTel
                    var numberDetailsResult = await _modemTelManager.GetPhoneNumberDetailsAsync(apiKey, apiBaseUrl, webhookContext.PhoneNumberId);
                    if (!numberDetailsResult.Success || numberDetailsResult.Data == null)
                    {
                        _logger.LogWarning("Failed to get phone number details from ModemTel: {Message}",
                            numberDetailsResult.Message);
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

        private async Task<ForwardResult> ForwardCallToBackendAsync(
            string serverEndpoint,
            TelephonyWebhookContextModel webhookContext,
            CallQueueData callQueue)
        {
            var result = new ForwardResult();

            try
            {
                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient();
                
                // Get the API key for backend authentication
                string apiKey = _configuration["Security:BackendApiKey"];
                
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
                var response = await client.PostAsync(
                    $"{serverEndpoint}/api/call/incoming",
                    content);

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

        private async Task NotifyBackendCallEndedAsync(string serverId, string sessionId)
        {
            try
            {
                // Get server endpoint from the serverId
                string serverEndpoint = serverId; // In a real implementation, map to actual endpoint

                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient();
                
                // Get the API key for backend authentication
                string apiKey = _configuration["Security:BackendApiKey"];
                
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
            public string RouteId { get; set; } = string.Empty;
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

    // Add this method to the CallQueueRepository
    public static class CallQueueRepositoryExtensions
    {
        public static async Task<CallQueueData?> GetCallByProviderCallIdAsync(
            this CallQueueRepository repository, 
            TelephonyProviderEnum provider, 
            string providerCallId)
        {
            // Implementation would filter calls by provider and providerCallId
            // For now, we'll just implement a placeholder
            return await repository.GetCallByProviderCallIdAsync(provider, providerCallId);
        }
    }
}