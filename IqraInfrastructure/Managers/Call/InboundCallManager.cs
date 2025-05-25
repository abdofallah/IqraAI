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
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Repositories.Conversation;

namespace IqraInfrastructure.Managers.Call
{
    public class InboundCallManager
    {
        private readonly ILogger<InboundCallManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly InboundCallQueueRepository _inboundCallQueueRepository;
        private readonly ServerSelectionManager _serverSelectionService;
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly PlanManager _planManager;
        private readonly ModemTelManager _modemTelManager;
        private readonly TwilioManager _twilioManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly RegionManager _regionManager;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BillingValidationManager _billingValidationManager;

        private JsonSerializerOptions _seralizationOptionCamelCase = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public InboundCallManager(
            ILogger<InboundCallManager> logger,
            IHttpClientFactory httpClientFactory,

            InboundCallQueueRepository inboundCallQueueRepository,
            ServerSelectionManager serverSelectionService,
            UserManager userManager,
            BusinessManager businessManager,
            PlanManager planManager,
            ModemTelManager modemTelManager,
            TwilioManager twilioManager,
            IntegrationsManager integrationsManager,
            RegionManager regionManager,
            ConversationStateRepository conversationStateRepository,
            BillingValidationManager billingValidationManager
        )
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _inboundCallQueueRepository = inboundCallQueueRepository;
            _serverSelectionService = serverSelectionService;
            _businessManager = businessManager;
            _userManager = userManager;
            _planManager = planManager;
            _modemTelManager = modemTelManager;
            _twilioManager = twilioManager;
            _integrationsManager = integrationsManager;
            _regionManager = regionManager;
            _conversationStateRepository = conversationStateRepository;
            _billingValidationManager = billingValidationManager;
        }

        public async Task<FunctionReturnResult<DistributionResultModel?>> DistributeIncomingCall(TelephonyWebhookContextModel webhookContext)
        {
            var result = new FunctionReturnResult<DistributionResultModel?>();
            string? callQueueId = null;

            try
            {
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

                // Create call queue entry
                InboundCallQueueData callQueue = new InboundCallQueueData
                {
                    EnqueuedAt = DateTime.UtcNow,
                    Status = CallQueueStatusEnum.ProcessingProxy,

                    BusinessId = businessId,
                    RegionId = regionId,

                    RouteId = numberRouteId,
                    RouteNumberId = phoneNumberInfo.NumberId,

                    RouteNumberProvider = webhookContext.Provider,
                    ProviderCallId = webhookContext.CallId,
                    CallerNumber = webhookContext.From,

                    ProviderMetadata = webhookContext.AdditionalData
                };
                callQueueId = await _inboundCallQueueRepository.EnqueueInboundCallQueueAsync(callQueue);
                if (string.IsNullOrWhiteSpace(callQueue.Id))
                {
                    result.Code = "DistributeIncomingCall:3";
                    result.Message = "Unable to create call queue entry";
                    return result;
                }
                callQueue.Id = callQueueId;

                var planValidation = await _billingValidationManager.CheckCreditAndConcurrencyAsync(businessId, "inbound call");
                if (!planValidation.Success)
                {
                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(callQueue.Id, new CallQueueLog() { CreatedAt = DateTime.UtcNow, Message = $"[{planValidation.Code}]: {planValidation.Message}", Type = CallQueueLogTypeEnum.Error });
                    result.Message = planValidation.Message;
                    return result;
                }

                var serverSelection = await _serverSelectionService.SelectOptimalServerAsync(regionId);
                if (!serverSelection.Success)
                {
                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(callQueue.Id, new CallQueueLog() { CreatedAt = DateTime.UtcNow, Message = $"[{serverSelection.Code}]: {serverSelection.Message}", Type = CallQueueLogTypeEnum.Error });
                    result.Code = "DistributeIncomingCall:" + serverSelection.Code;
                    result.Message = serverSelection.Message;
                    return result;
                }

                var regionData = await _regionManager.GetRegionById(regionId);
                if (regionData == null)
                {
                    _logger.LogError("Error distributing call {CallId} for provider {Provider} in {businessId}/{phoneNumberId}: region not found {RegionId}", webhookContext.CallId, webhookContext.Provider, webhookContext.BusinessId, webhookContext.PhoneNumberId, regionId);

                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(callQueue.Id, new CallQueueLog() { CreatedAt = DateTime.UtcNow, Message = $"Region not found: {regionId}", Type = CallQueueLogTypeEnum.Error });
                    result.Code = "DistributeIncomingCall:4";
                    result.Message = $"Region not found: {regionId}";     
                    return result;
                }
               

                FunctionReturnResult forwardResult = new FunctionReturnResult();
                List<string> errorsList = new List<string>();
                int optimalServersTried = 0;
                while (optimalServersTried < serverSelection.Data.Count)
                {
                    forwardResult = new FunctionReturnResult();
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

                    callQueue.ProcessingBackendServerId = regionServerId;
                    await _inboundCallQueueRepository.UpdateInboundCallQueueProcessingBackendServerIdAsync(callQueue.Id, regionServerId);
                    await _inboundCallQueueRepository.UpdateInboundCallQueueStatusAsync(callQueue.Id, CallQueueStatusEnum.ProcessedProxy);

                    // 5. Forward call to selected backend server         
                    int forwardCallAttempt = 0;
                    while (forwardCallAttempt < 3)
                    {
                        forwardResult  = await ForwardIncomingCallToBackendAsync(regionServerId, regionServerApiKey, resgionUseSSL, webhookContext, callQueue);
                        if (!forwardResult.Success)
                        {
                            await _inboundCallQueueRepository.UpdateInboundCallQueueStatusAsync(callQueue.Id, CallQueueStatusEnum.ProcessingProxy);
                            errorsList.Add($"Attempt {forwardCallAttempt}: [{forwardResult.Code}] {forwardResult.Message}");
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
                    var message = string.Join("\n", errorsList);
                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(callQueue.Id, new CallQueueLog() { CreatedAt = DateTime.UtcNow, Message = message, Type = CallQueueLogTypeEnum.Error });
                    return result.SetFailureResult("DistributeIncomingCall:5", message);
                }
                
                return result.SetSuccessResult(
                    new DistributionResultModel()
                    {
                        QueueId = callQueue.Id,
                        BackendServerId = callQueue.ProcessingBackendServerId
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error distributing call {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", webhookContext.CallId, webhookContext.Provider, webhookContext.BusinessId, webhookContext.PhoneNumberId);

                if (callQueueId != null && !string.IsNullOrEmpty(callQueueId))
                {
                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(callQueueId, new CallQueueLog() { CreatedAt = DateTime.UtcNow, Message = ex.Message, Type = CallQueueLogTypeEnum.Error });
                }

                result.Code = "DistributeIncomingCall:-1";
                result.Message = $"Error distributing call: {ex.Message}";    
                return result;
            }
        }

        public async Task<FunctionReturnResult> NotifyCallRinging(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {
            var result = new FunctionReturnResult();




            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> NotifyCallBusy(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {
            var result = new FunctionReturnResult();




            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> NotifyCallStarted(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {
            var result = new FunctionReturnResult();




            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> NotifyCallEnded(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {

            var result = new FunctionReturnResult();




            return result.SetSuccessResult();


            try
            {
                // Find the call in the queue
                var callQueue = await _inboundCallQueueRepository.GetInboundCallQueueByProviderCallIdAsync(provider, callId, businessId, phoneNumberId);
                if (callQueue == null)
                {
                    _logger.LogWarning("Call not found in queue for end notification: {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", callId, provider, businessId, phoneNumberId);
                    return;
                }

                // If the call has a session ID, notify the backend app
                if (!string.IsNullOrEmpty(callQueue.SessionId) && !string.IsNullOrEmpty(callQueue.ProcessingBackendServerId))
                {
                    // Get Region Api key
                    var regionData = await _regionManager.GetRegionById(callQueue.RegionId);
                    if (regionData == null)
                    {
                        _logger.LogWarning("Region not found: {RegionId}", callQueue.RegionId);
                        return;
                    }
                    var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == callQueue.ProcessingBackendServerId);
                    if (regionServerData == null)
                    {
                        _logger.LogWarning("Region server not found: {ServerEndpoint}", callQueue.ProcessingBackendServerId);
                        return;
                    }
                    var regionServerApiKey = regionServerData.APIKey;
                    var regionUseSSL = regionServerData.UseSSL;

                    await NotifyBackendCallEndedAsync(callQueue.ProcessingBackendServerId, regionServerApiKey, regionUseSSL, callQueue.SessionId, provider, phoneNumberId);
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

        private async Task<FunctionReturnResult> ForwardIncomingCallToBackendAsync(string serverEndpoint, string serverApiKey, bool regionUseSSL, TelephonyWebhookContextModel webhookContext, CallQueueData callQueue)
        {
            var result = new FunctionReturnResult();

            try
            {
                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient("CallManagerServerForward");

                // Set headers
                client.Timeout = TimeSpan.FromSeconds(7); // todo check if 7 seconds is good
                client.DefaultRequestHeaders.Add("X-API-Key", serverApiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Prepare the request body
                var requestBody = new BackendInboundCallRequest()
                {
                    QueueId = callQueue.Id,
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

                    _logger.LogError("Error forwarding call to backend server {StatusCode} - {Error}", response.StatusCode, errorContent);

                    result.Code = "ForwardCallToBackendAsync:1";
                    result.Message = "Error forwarding call to backend server";
                    return result;
                }

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<FunctionReturnResult>(responseContent, _seralizationOptionCamelCase);
                if (responseData == null) // should never happen tho
                {
                    _logger.LogError("Invalid response from backend server {ResponseContent}", responseContent);

                    result.Code = "ForwardCallToBackendAsync:2";
                    result.Message = "Invalid response from backend server";              
                    return result;
                }

                if (!responseData.Success)
                {
                    _logger.LogError("Error forwarding call to backend server: {Code} - {Message}", responseData.Code, responseData.Message);

                    result.Code = "ForwardCallToBackendAsync:" + responseData.Code;
                    result.Message = responseData.Message;
                    return result;
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding call to backend server");

                result.Code = "ForwardCallToBackendAsync:-1";
                result.Message = $"Error forwarding call to backend: {ex.Message}";
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
                client.Timeout = TimeSpan.FromSeconds(15); // todo check if 15seconds is good
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

                    _logger.LogError("Failed to notify backend of call end: {StatusCode} - {Error}", response.StatusCode, errorContent);

                    return;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<FunctionReturnResult>(responseContent, _seralizationOptionCamelCase);
                if (responseData == null) // should never hapopen tho
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