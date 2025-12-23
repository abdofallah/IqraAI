using IqraCore.Entities.Billing;
using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Interfaces.User;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.Call;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Call.Outbound
{
    public class OutboundCallProcessingOrchestrator
    {
        private readonly ILogger<OutboundCallProcessingOrchestrator> _logger;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepo;
        private readonly IUserUsageValidationManager _billingValidationManager;
        private readonly ServerSelectionManager _serverSelectionManager;
        private readonly BusinessManager _businessManager;
        private readonly RegionManager _regionManager;
        private readonly ModemTelManager _modemTelManager;
        private readonly TwilioManager _twilioManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CampaignActionExecutorService _campaignActionExecutorService;
        private readonly UserManager _userManager;

        private readonly JsonSerializerOptions _camelCaseSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public OutboundCallProcessingOrchestrator(
            ILogger<OutboundCallProcessingOrchestrator> logger,
            OutboundCallQueueRepository outboundCallQueueRepo,
            IUserUsageValidationManager billingValidationManager,
            ServerSelectionManager serverSelectionManager,
            BusinessManager businessManager,
            RegionManager regionManager,
            ModemTelManager modemTelManager,
            TwilioManager twilioManager,
            IntegrationsManager integrationsManager,
            IHttpClientFactory httpClientFactory,
            CampaignActionExecutorService campaignActionExecutorService,
            UserManager userManager
        )
        {
            _logger = logger;
            _outboundCallQueueRepo = outboundCallQueueRepo;
            _billingValidationManager = billingValidationManager;
            _serverSelectionManager = serverSelectionManager;
            _regionManager = regionManager;
            _businessManager = businessManager;
            _modemTelManager = modemTelManager;
            _twilioManager = twilioManager;
            _integrationsManager = integrationsManager;
            _httpClientFactory = httpClientFactory;
            _campaignActionExecutorService = campaignActionExecutorService;
            _userManager = userManager;
        }

        public async Task ProcessCallAsync(OutboundCallQueueData callQueueData)
        {
            if (callQueueData.MaxScheduleForDateTime <= DateTime.UtcNow)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Expired,
                    new CallQueueLogEntry {
                        Type = CallQueueLogTypeEnum.Information,
                        Message = "Call expired by reaching max schedule date time."
                    },
                    completedAt: DateTime.UtcNow
                );
                return;
            }

            var validationResult = await _billingValidationManager.ValidateCallPermissionAsync(callQueueData.BusinessId);
            if (!validationResult.Success)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Canceled,
                    new CallQueueLogEntry {
                        Message = $"Billing validation failed: [{validationResult.Code}] {validationResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
                return;
            }

            var preCheckConcurrency = await _billingValidationManager.CheckUsageConcurrency(callQueueData.BusinessId, BillingFeatureKey.CallConcurrency);
            if (!preCheckConcurrency.Success)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Queued
                    // DO NOT LOG, we do not want crazy recheck amounts
                );
                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(callQueueData.BusinessId, "ProcessCallAsync");
            if (!businessDataResult.Success || businessDataResult.Data == null)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Canceled,
                    new CallQueueLogEntry {
                        Message = $"System error: Business {callQueueData.BusinessId} not found.",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
                return;
            }
            var isUserAdmin = await _userManager.CheckUserIsAdmin(businessDataResult.Data.MasterUserEmail);

            var businessPhoneNumber = await _businessManager.GetNumberManager().GetBusinessNumberById(callQueueData.BusinessId, callQueueData.CallingNumberId);
            if (businessPhoneNumber == null)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Canceled,
                    new CallQueueLogEntry {
                        Message = $"System error: Business number {callQueueData.CallingNumberId} not found.",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
            }

            var businessPhoneIntegration = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(callQueueData.BusinessId, businessPhoneNumber.IntegrationId);
            if (businessPhoneIntegration == null)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Canceled,
                    new CallQueueLogEntry {
                        Message = $"System error: Business number {callQueueData.CallingNumberId} integration {businessPhoneNumber.IntegrationId} not found.",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
            }

            switch (businessPhoneNumber.Provider)
            {
                case TelephonyProviderEnum.ModemTel:
                    {
                        var modemTelPhonenumberId = ((BusinessNumberModemTelData)businessPhoneNumber).ModemTelPhoneNumberId;
                        var modemtelStatusToCheck = new List<string>() { "Queued", "CallInProgress", "RingingIncoming", "RingingOutgoing" };

                        var currentNumberCalls = await _modemTelManager.GetCallsByStatusForPhoneNumber(_integrationsManager.DecryptField(businessPhoneIntegration.Data.EncryptedFields["apikey"]), businessPhoneIntegration.Data.Fields["endpoint"], modemTelPhonenumberId, modemtelStatusToCheck, 1);
                        if (!currentNumberCalls.Success)
                        {
                            await OnUpdateCallQueueStatusAndSendCampaignAction(
                                callQueueData,
                                CallQueueStatusEnum.Canceled,
                                new CallQueueLogEntry {
                                    Message = $"[{currentNumberCalls.Code}] {currentNumberCalls.Message}",
                                    Type = CallQueueLogTypeEnum.Error
                                },
                                completedAt: DateTime.UtcNow
                            );
                            return;
                        }

                        if (currentNumberCalls.Data.Count > 0)
                        {
                            await OnUpdateCallQueueStatusAndSendCampaignAction(callQueueData, CallQueueStatusEnum.Queued);
                            return;
                        }
                        break;
                    }

                case TelephonyProviderEnum.Twilio:
                    {
                        await OnUpdateCallQueueStatusAndSendCampaignAction(callQueueData, CallQueueStatusEnum.Queued);
                        break;
                    }

                default:
                    {
                        await OnUpdateCallQueueStatusAndSendCampaignAction(
                            callQueueData,
                            CallQueueStatusEnum.Canceled,
                            new CallQueueLogEntry {
                                Message = $"Unknown calling number provider: {businessPhoneNumber.Provider}.",
                                Type = CallQueueLogTypeEnum.Error
                            },
                            completedAt: DateTime.UtcNow
                        );
                        return;
                    }
            }

            await OnUpdateCallQueueStatusAndSendCampaignAction(
                callQueueData,
                CallQueueStatusEnum.ProcessingProxy,
                new CallQueueLogEntry {
                    Message = $"Processing Queue within proxy",
                    Type = CallQueueLogTypeEnum.Information
                },
                processingStartedAt: DateTime.UtcNow
            );

            var serverSelectionResult = await _serverSelectionManager.SelectOptimalServerAsync(callQueueData.RegionId, isUserAdmin);
            if (!serverSelectionResult.Success || !serverSelectionResult.Data.Any())
            {
                // todo this should happen very critically but should we kill the queue because of it?
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Failed,
                    new CallQueueLogEntry {
                        Message = $"No backend server available for region {callQueueData.RegionId}. {serverSelectionResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
                return;
            }

            RegionData? regionDetails = await _regionManager.GetRegionById(callQueueData.RegionId);
            if (regionDetails == null)
            {
                _logger.LogError("Region details not found for {RegionId} during call {QueueId} processing.", callQueueData.RegionId, callQueueData.Id);
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Failed,
                    new CallQueueLogEntry {
                        Message = $"System error: Region details for {callQueueData.RegionId} not found.",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
                return;
            }

            foreach (var optimalServer in serverSelectionResult.Data)
            {
                RegionServerData? backendServerDetails = regionDetails.Servers.FirstOrDefault(s => s.Id == optimalServer.ServerId && s.Type == ServerTypeEnum.Backend);
                if (backendServerDetails == null)
                {
                    await OnUpdateCallQueueStatusAndSendCampaignAction(
                        callQueueData,
                        CallQueueStatusEnum.Failed,
                        new CallQueueLogEntry {
                            Message = $"System error: Region details for {callQueueData.RegionId} not found.",
                            Type = CallQueueLogTypeEnum.Error
                        },
                        completedAt: DateTime.UtcNow
                    );
                    return;
                }

                var requestDto = new BackendOutboundCallRequest
                {
                    QueueId = callQueueData.Id
                };

                var forwardResponse = await ForwardToBackendAsync(backendServerDetails, requestDto);
                if (!forwardResponse.Success)
                {
                    await OnUpdateCallQueueStatusAndSendCampaignAction(
                        callQueueData,
                        CallQueueStatusEnum.Failed,
                        new CallQueueLogEntry
                        {
                            Message = $"Failed to forward to backend. [{forwardResponse.Code}] {forwardResponse.Message}",
                            Type = CallQueueLogTypeEnum.Error
                        },
                        completedAt: DateTime.UtcNow
                    );
                    break;
                }
                else
                {
                    var backendResult = forwardResponse.Data!;

                    if (!backendResult.Success)
                    {
                        if (backendResult.Data!.ShouldRequeue)
                        {
                            string? requeueReason = backendResult.Message ?? "";
                            await OnUpdateCallQueueStatusAndSendCampaignAction(
                                callQueueData,
                                CallQueueStatusEnum.Queued,
                                new CallQueueLogEntry
                                {
                                    Message = $"Requeuing call{(string.IsNullOrWhiteSpace(requeueReason) ? "" : $": {requeueReason}")}",
                                    Type = CallQueueLogTypeEnum.Information
                                }
                            );
                        }
                        else
                        {
                            await OnUpdateCallQueueStatusAndSendCampaignAction(
                                callQueueData,
                                CallQueueStatusEnum.Failed,
                                new CallQueueLogEntry
                                {
                                    Message = $"Backend call processing failure: [{backendResult.Code}] {backendResult.Message}",
                                    Type = CallQueueLogTypeEnum.Error
                                },
                                completedAt: DateTime.UtcNow
                            );
                        }
                    }
                    else
                    {
                        // if success everything else related to queue is handled by backend app
                    }

                    break;
                }
            }
        }

        private async Task<FunctionReturnResult<FunctionReturnResult<BackendInitiateOutboundCallResultModel>>> ForwardToBackendAsync(RegionServerData backendServer, BackendOutboundCallRequest requestDto)
        {
            var result = new FunctionReturnResult<FunctionReturnResult<BackendInitiateOutboundCallResultModel>>();
            string endpoint = (backendServer.UseSSL ? "https://" : "http://") + backendServer.Endpoint;

            var baseUri = new Uri(endpoint);
            baseUri = new Uri(baseUri, $"{(baseUri.AbsolutePath != "/" ? baseUri.AbsolutePath : "")}/api/call/outbound");

            try
            {
                using var client = _httpClientFactory.CreateClient("OutboundCallForwardClient");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(backendServer.APIKey))
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", backendServer.APIKey);
                }

                var jsonPayload = JsonSerializer.Serialize(requestDto, _camelCaseSerializationOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(baseUri, content);

                var responseContentString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return result.SetFailureResult($"ForwardToBackend:{response.StatusCode}", $"Backend returned error: {response.StatusCode}. Details: {responseContentString}");
                }

                FunctionReturnResult<BackendInitiateOutboundCallResultModel?>? backendResponse;
                try
                {
                    backendResponse = JsonSerializer.Deserialize<FunctionReturnResult<BackendInitiateOutboundCallResultModel?>?>(responseContentString, _camelCaseSerializationOptions);
                }
                catch (Exception ex)
                {
                    return result.SetFailureResult(
                        "ForwardToBackend:BACKEND_RESPONSE_PARSE_FAIL",
                        $"Backend failed to process or invalid response format. Exception: {ex.Message}, Response: {responseContentString}"
                    );
                }

                if (backendResponse == null || backendResponse.Data == null)
                {
                    return result.SetFailureResult(
                        "ForwardToBackend:BACKEND_RESPONSE_PARSED_BUT_NULL",
                        $"Backend returned null response. Response: {responseContentString}"
                    );
                }

                return result.SetSuccessResult(backendResponse!);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request exception while forwarding call {QueueId} to {EndpointUrl}.", requestDto.QueueId, baseUri.ToString());
                return result.SetFailureResult("ForwardToBackend:HttpRequestError", $"HTTP request error: {httpEx.Message}");
            }
            catch (TaskCanceledException tex)
            {
                return result.SetFailureResult("ForwardToBackend:Timeout", "Request to backend timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic exception while forwarding call {QueueId} to {EndpointUrl}.", requestDto.QueueId, baseUri.ToString());
                return result.SetFailureResult("ForwardToBackend:GenericError", $"Exception: {ex.Message}");
            }
        }
    
        public async Task OnUpdateCallQueueStatusAndSendCampaignAction(
            OutboundCallQueueData callQueueData, CallQueueStatusEnum newStatus,
            CallQueueLogEntry? log = null,
            string? newProcessingServerId = null,
            DateTime? processingStartedAt = null,
            DateTime? completedAt = null,
            Dictionary<string, string>? providerMetadata = null,
            string? providerCallId = null)
        {
            await _outboundCallQueueRepo.UpdateCallStatusAsync(callQueueData.Id, newStatus, log, newProcessingServerId, processingStartedAt, completedAt, providerMetadata, providerCallId);

            // Run action in background
            var logMessage = "Unknown";
            if (log != null)
            {
                logMessage = $"[{log.Type.ToString()}] {log.Message}";
            }

            if (newStatus == CallQueueStatusEnum.Failed || newStatus == CallQueueStatusEnum.Canceled || newStatus == CallQueueStatusEnum.Expired)
            {
                _ = _campaignActionExecutorService.SendOutboundCallQueueTelephonyCampaignAction(callQueueData.Id, logMessage);
            }
        }
    }
}
