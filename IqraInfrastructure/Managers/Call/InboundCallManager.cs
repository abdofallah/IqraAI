using IqraCore.Entities.Helper.Telephony;
using IqraInfrastructure.Managers.Business;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using IqraCore.Models.Telephony;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Integrations;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Region;
using IqraCore.Entities.Business;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Telephony;
using IqraCore.Entities.Server.Call;
using IqraInfrastructure.Repositories.Call;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Call.Queue;

namespace IqraInfrastructure.Managers.Call
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

        private JsonSerializerOptions _seralizationOptionCamelCase = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
                    result.Code = "DistributeIncomingCall:1";
                    result.Message = "Unable to identify phone number";
                    return result;
                }

                long businessId = phoneNumberInfo.BusinessId;
                string? numberRouteId = phoneNumberInfo.RouteId;
                string regionId = phoneNumberInfo.RegionId;

                // Ignore the call for now if the business number has no route
                // TODO in future if phone number has options of what to do in case of no route, set it here
                if (string.IsNullOrWhiteSpace(numberRouteId))
                {
                    result.Code = "DistributeIncomingCall:2";
                    result.Message = "Business number has no route set";
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
                    result.Code = "DistributeIncomingCall:" + serverSelection.Code;
                    result.Message = serverSelection.Message;
                    return result;
                }

                // Get Region Api key
                var regionData = await _regionManager.GetRegionById(regionId);
                if (regionData == null)
                {
                    result.Code = "DistributeIncomingCall:3";
                    result.Message = $"Region not found: {regionId}";
                    _logger.LogWarning("Region not found: {RegionId}", regionId);
                    return result;
                }

                // Create call queue entry
                var callQueue = new CallQueueData
                {
                    BusinessId = businessId,
                    RegionId = regionId,
                    //NumberId = phoneNumberInfo.NumberId,
                    //RouteId = numberRouteId,
                    //Provider = webhookContext.Provider,
                    //ProviderCallId = webhookContext.CallId,
                    //CallerNumber = webhookContext.From,
                    //Priority = 2, // High priority for incoming calls
                    //IsOutbound = false,
                    ProcessingServerId = "PROCESSING",
                    ProviderMetadata = webhookContext.AdditionalData
                };
                callQueue.Id = await _callQueueRepository.EnqueueCallQueueAsync(callQueue);

                FunctionReturnResult forwardResult = new FunctionReturnResult();
                List<string> errorsList = new List<string>();
                int optimalServersTried = 0;
                while (optimalServersTried < serverSelection.Data.Count)
                {
                    var currentServer = serverSelection.Data[optimalServersTried];

                    var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == currentServer.ServerEndpoint);
                    if (regionServerData == null)
                    {
                        errorsList.Add($"{optimalServersTried}: Region server not found: {currentServer.ServerEndpoint}");
                        continue;
                    }

                    var regionServerId = regionServerData.Endpoint;
                    var regionServerApiKey = regionServerData.APIKey;
                    var resgionUseSSL = regionServerData.UseSSL;

                    callQueue.ProcessingServerId = regionServerId;
                    await _callQueueRepository.UpdateCallQueueProcessingServerAsync(callQueue.Id, regionServerId);

                    // 5. Forward call to selected backend server         
                    int forwardCallAttempt = 0;
                    while (forwardCallAttempt < 3)
                    {
                        forwardResult  = await ForwardCallToBackendAsync(regionServerId, regionServerApiKey, resgionUseSSL, webhookContext, callQueue);
                        if (!forwardResult.Success)
                        {
                            errorsList.Add($"Attempt {forwardCallAttempt}: {forwardResult.Code}: {forwardResult.Message}");
                        }
                        else
                        {
                            break;
                        }

                        forwardCallAttempt++;
                    }
                    
                    if (forwardResult.Success)
                    {
                        break;
                    }

                    optimalServersTried++;
                }
                
                if (!forwardResult.Success)
                {
                    await _callQueueRepository.UpdateCallQueueStatusAsync(callQueue.Id, CallQueueStatusEnum.Failed);
                    return result.SetFailureResult("Failed to distribute call", string.Join("\n", errorsList));
                }

                // Return success result
                return result.SetSuccessResult(
                    new DistributionResultModel()
                    {
                        QueueId = callQueue.Id,
                        BackendServerId = callQueue.ProcessingServerId
                    }
                );
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
                var callQueue = await _callQueueRepository.GetCallQueueByProviderCallIdAsync(provider, callId, businessId, phoneNumberId);
                if (callQueue == null)
                {
                    _logger.LogWarning("Call not found in queue for end notification: {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", callId, provider, businessId, phoneNumberId);
                    return;
                }

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
                    var regionUseSSL = regionServerData.UseSSL;

                    await NotifyBackendCallEndedAsync(callQueue.ProcessingServerId, regionServerApiKey, regionUseSSL, callQueue.SessionId, provider, phoneNumberId);
                    // todo notify backend and get success response to try again
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
                    return null;
                }

                var businessNumber = await _businessManager.GetNumberManager().GetBusinessNumberById(webhookContext.BusinessId, webhookContext.PhoneNumberId);
                if (businessNumber == null)
                {
                    return null;
                }

                var integratonData = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(webhookContext.BusinessId, businessNumber.IntegrationId);
                if (integratonData == null)
                {
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

        private async Task<FunctionReturnResult> ForwardCallToBackendAsync(string serverEndpoint, string serverApiKey, bool regionUseSSL, TelephonyWebhookContextModel webhookContext, CallQueueData callQueue)
        {
            var result = new FunctionReturnResult();

            try
            {
                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient("CallManagerServerForward");
                // ignore ssl validation etc for client

                // Set headers
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.Add("X-API-Key", serverApiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Prepare the request body
                var requestBody = new BackendIncomingCallRequest()
                {
                    Provider = webhookContext.Provider,
                    ProviderCallId = webhookContext.CallId,
                    QueueId = callQueue.Id,
                    BusinessId = callQueue.BusinessId,
                    PhoneNumberId = webhookContext.PhoneNumberId,
                    To = webhookContext.To,
                    From = webhookContext.From,
                    RouteId = "",//callQueue.RouteId,
                    AdditionalData = webhookContext.AdditionalData
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Send the request to the backend app
                if (regionUseSSL)
                {
                    serverEndpoint = "https://" + serverEndpoint;
                }
                else
                {
                    serverEndpoint = "http://" + serverEndpoint;
                }

                var baseUri = new Uri(serverEndpoint);
                baseUri = new Uri(baseUri, "/api/call/incoming");
                var response = await client.PostAsync(baseUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.Code = "ForwardCallToBackendAsync:1";
                    result.Message = "Error forwarding call to backend server";
                    _logger.LogError("Error forwarding call to backend server {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return result;
                }

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<FunctionReturnResult>(responseContent, _seralizationOptionCamelCase);
                if (responseData == null)
                {
                    result.Code = "ForwardCallToBackendAsync:2";
                    result.Message = "Invalid response from backend server";
                    _logger.LogError("Invalid response from backend server");
                    return result;
                }

                if (!responseData.Success)
                {
                    result.Code = "ForwardCallToBackendAsync:" + responseData.Code;
                    result.Message = responseData.Message;
                    _logger.LogError("Error forwarding call to backend server: {Code} - {Message}", responseData.Code, responseData.Message);
                    return result;
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Code = "ForwardCallToBackendAsync:-1";
                result.Message = $"Error forwarding call to backend: {ex.Message}";
                _logger.LogError(ex, "Error forwarding call to backend server");
                return result;
            }
        }

        private async Task NotifyBackendCallEndedAsync(string serverEndpoint, string apiKey, bool regionUseSSL, string sessionId, TelephonyProviderEnum provider, string phoneNumberId)
        {
            try
            {
                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient("CallManagerServerForward");    
                
                // Set headers
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                // Prepare the request body
                var requestBody = new CallEndNotifyBackendData()
                {
                    Provider = provider,
                    PhoneNumberId = phoneNumberId
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Send the notification
                if (regionUseSSL)
                {
                    serverEndpoint = "https://" + serverEndpoint;
                }
                else
                {
                    serverEndpoint = "http://" + serverEndpoint;
                }

                var baseUri = new Uri(serverEndpoint);
                baseUri = new Uri(baseUri, $"/api/call/{sessionId}/ended");
                var response = await client.PostAsync(baseUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to notify backend of call end: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                    return;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<FunctionReturnResult>(responseContent, _seralizationOptionCamelCase);
                if (responseData == null)
                {
                    _logger.LogError("Invalid response from backend server {ResponseContent}", responseContent);
                    return;
                }

                if (!responseData.Success)
                {
                    _logger.LogError("Error forwarding call ended notificaiton to backend server: {Code} - {Message}", responseData.Code, responseData.Message);
                    return;
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
    }
}