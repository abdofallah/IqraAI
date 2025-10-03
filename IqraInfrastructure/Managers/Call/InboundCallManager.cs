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
using IqraInfrastructure.Repositories.Call;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Call.Queue;
using IqraInfrastructure.Managers.Billing;

namespace IqraInfrastructure.Managers.Call
{
    public class InboundCallManager
    {
        private readonly ILogger<InboundCallManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly InboundCallQueueRepository _inboundCallQueueRepository;
        private readonly ServerSelectionManager _serverSelectionService;
        private readonly BusinessManager _businessManager;
        private readonly ModemTelManager _modemTelManager;
        private readonly TwilioManager _twilioManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly RegionManager _regionManager;
        private readonly UserUsageValidationManager _billingValidationManager;

        private JsonSerializerOptions _seralizationOptionCamelCase = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public InboundCallManager(
            ILogger<InboundCallManager> logger,
            IHttpClientFactory httpClientFactory,

            InboundCallQueueRepository inboundCallQueueRepository,
            ServerSelectionManager serverSelectionService,
            BusinessManager businessManager,
            ModemTelManager modemTelManager,
            TwilioManager twilioManager,
            IntegrationsManager integrationsManager,
            RegionManager regionManager,
            UserUsageValidationManager billingValidationManager
        )
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _inboundCallQueueRepository = inboundCallQueueRepository;
            _serverSelectionService = serverSelectionService;
            _businessManager = businessManager;
            _modemTelManager = modemTelManager;
            _twilioManager = twilioManager;
            _integrationsManager = integrationsManager;
            _regionManager = regionManager;
            _billingValidationManager = billingValidationManager;
        }

        public async Task<FunctionReturnResult<ProcessedInboundCallResponse?>> DistributeIncomingCall(TelephonyWebhookContextModel webhookContext)
        {
            var result = new FunctionReturnResult<ProcessedInboundCallResponse?>();
            string? callQueueId = null;

            try
            {
                var phoneNumberInfo = await GetPhoneNumberInfo(webhookContext);
                if (phoneNumberInfo == null)
                {
                    _logger.LogError("Error distributing call {CallId} for provider {Provider}: phone number not found", webhookContext.CallId, webhookContext.Provider);
                    return result.SetFailureResult("DistributeIncomingCall:NUMBER_NOT_FOUND", "Unable to identify phone number");
                }

                long businessId = phoneNumberInfo.BusinessId;
                string? numberRouteId = phoneNumberInfo.RouteId;
                string regionId = phoneNumberInfo.RegionId;

                // Ignore the call for now if the business number has no route
                // TODO in future if phone number has options of what to do in case of no route, set it here
                if (string.IsNullOrWhiteSpace(numberRouteId))
                {
                    _logger.LogError("Error distributing call {CallId} for provider {Provider}: business number has no route set", webhookContext.CallId, webhookContext.Provider);
                    return result.SetFailureResult("DistributeIncomingCall:NO_ROUTE_SET", "Business number has no route set");
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
                    return result.SetFailureResult("DistributeIncomingCall:CALL_QUEUE_NOT_CREATED", "Unable to create call queue entry");
                }
                callQueue.Id = callQueueId;

                var planValidation = await _billingValidationManager.ValidateCallPermissionAsync(businessId, true);
                if (!planValidation.Success)
                {
                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(callQueue.Id, new CallQueueLog() { CreatedAt = DateTime.UtcNow, Message = $"[{planValidation.Code}]: {planValidation.Message}", Type = CallQueueLogTypeEnum.Error });

                    return result.SetFailureResult($"DistributeIncomingCall:{planValidation.Code}", planValidation.Message);
                }

                var serverSelection = await _serverSelectionService.SelectOptimalServerAsync(regionId);
                if (!serverSelection.Success)
                {
                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(callQueue.Id, new CallQueueLog() { CreatedAt = DateTime.UtcNow, Message = $"[{serverSelection.Code}]: {serverSelection.Message}", Type = CallQueueLogTypeEnum.Error });

                    return result.SetFailureResult($"DistributeIncomingCall:{serverSelection.Code}", serverSelection.Message);
                }

                var regionData = await _regionManager.GetRegionById(regionId);
                if (regionData == null)
                {
                    _logger.LogError("Error distributing call {CallId} for provider {Provider} in {businessId}/{phoneNumberId}: region not found {RegionId}", webhookContext.CallId, webhookContext.Provider, webhookContext.BusinessId, webhookContext.PhoneNumberId, regionId);

                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(callQueue.Id, new CallQueueLog() { CreatedAt = DateTime.UtcNow, Message = $"Region not found: {regionId}", Type = CallQueueLogTypeEnum.Error });
                    
                    return result.SetFailureResult("DistributeIncomingCall:REGION_NOT_FOUND", $"Region not found: {regionId}");
                }
               

                FunctionReturnResult<ProcessedInboundCallResponse?> forwardResult = new FunctionReturnResult<ProcessedInboundCallResponse?>();
                List<string> errorsList = new List<string>();
                int optimalServersTried = 0;
                while (optimalServersTried < serverSelection.Data.Count)
                {
                    forwardResult = new FunctionReturnResult<ProcessedInboundCallResponse?>();
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
                    return result.SetFailureResult("DistributeIncomingCall:BACKEND_ERROR", message);
                }
                
                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error distributing call {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", webhookContext.CallId, webhookContext.Provider, webhookContext.BusinessId, webhookContext.PhoneNumberId);

                if (callQueueId != null && !string.IsNullOrEmpty(callQueueId))
                {
                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(callQueueId, new CallQueueLog() { CreatedAt = DateTime.UtcNow, Message = ex.Message, Type = CallQueueLogTypeEnum.Error });
                }

                return result.SetFailureResult("DistributeIncomingCall:EXCEPTION", result.Message);
            }
        }
             
        private async Task<FunctionReturnResult<ProcessedInboundCallResponse?>> ForwardIncomingCallToBackendAsync(string serverEndpoint, string serverApiKey, bool regionUseSSL, TelephonyWebhookContextModel webhookContext, CallQueueData callQueue)
        {
            var result = new FunctionReturnResult<ProcessedInboundCallResponse?>();

            try
            {
                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient("CallManagerServerForward");

                // Set headers
                client.Timeout = TimeSpan.FromSeconds(30); // todo check if 14 seconds is good
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
                baseUri = new Uri(baseUri, $"{(baseUri.AbsolutePath != "/" ? baseUri.AbsolutePath : "")}/api/call/incoming");
                var response = await client.PostAsync(baseUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    _logger.LogError("Error forwarding call to backend server {StatusCode} - {Error}", response.StatusCode, errorContent);

                    return result.SetFailureResult("ForwardCallToBackendAsync:RESPONSE_" + response.StatusCode, errorContent);
                }

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<FunctionReturnResult<ProcessedInboundCallResponse?>>(responseContent, _seralizationOptionCamelCase);
                if (responseData == null) // should never happen tho
                {
                    _logger.LogError("Invalid response from backend server {ResponseContent}", responseContent);
         
                    return result.SetFailureResult("ForwardCallToBackendAsync:INVALID_RESPONSE", "Invalid response from backend server");
                }

                if (!responseData.Success)
                {
                    _logger.LogError("Error forwarding call to backend server: {Code} - {Message}", responseData.Code, responseData.Message);

                    return result.SetFailureResult("ForwardCallToBackendAsync:" + responseData.Code, responseData.Message);
                }

                return result.SetSuccessResult(responseData.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding call to backend server");

                return result.SetFailureResult("ForwardCallToBackendAsync:EXCEPTION", result.Message);
            }
        }


        /**
         * 
         * TELEPHONY PROVIDER PHONE NUMBER HELPER
         * 
        **/
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
                    string accountSid = integratonData.Data.Fields["sid"];
                    string authToken = _integrationsManager.DecryptField(integratonData.Data.EncryptedFields["auth"]);

                    // Get the number details from Twilio
                    var numberDetailsResult = await _twilioManager.GetPhoneNumberDetailsAsync(accountSid, authToken, ((BusinessNumberTwilioData)businessNumber).TwilioPhoneNumberId);
                    if (!numberDetailsResult.Success || numberDetailsResult.Data == null)
                    {
                        _logger.LogWarning("Failed to get phone number details from Twilio: {Message}", numberDetailsResult.Message);
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
        private class PhoneNumberInfo
        {
            public long BusinessId { get; set; }
            public string NumberId { get; set; } = string.Empty;
            public string? RouteId { get; set; } = string.Empty;
            public string RegionId { get; set; } = string.Empty;
        }
    }
}