using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Repositories.Call;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Call
{
    public class OutboundCallProcessingOrchestrator
    {
        private readonly ILogger<OutboundCallProcessingOrchestrator> _logger;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepo;
        private readonly BillingValidationManager _billingValidationManager;
        private readonly ServerSelectionManager _serverSelectionManager;
        private readonly RegionManager _regionManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _camelCaseSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };


        public OutboundCallProcessingOrchestrator(
            ILogger<OutboundCallProcessingOrchestrator> logger,
            OutboundCallQueueRepository outboundCallQueueRepo,
            BillingValidationManager billingValidationManager,
            ServerSelectionManager serverSelectionManager,
            RegionManager regionManager,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _outboundCallQueueRepo = outboundCallQueueRepo;
            _billingValidationManager = billingValidationManager;
            _serverSelectionManager = serverSelectionManager;
            _regionManager = regionManager;
            _httpClientFactory = httpClientFactory;
        }

        public async Task ProcessCallAsync(OutboundCallQueueData call)
        {
            _logger.LogInformation("Processing outbound call {QueueId} for Business {BusinessId} to {RecipientNumber}.",
                call.Id, call.BusinessId, call.RecipientNumber);

            // a. Credit & Concurrency Check
            var validationResult = await _billingValidationManager.CheckCreditAndConcurrencyAsync(call.BusinessId, "outbound call");
            if (!validationResult.Success)
            {
                _logger.LogWarning("Validation failed for call {QueueId}: {Message} (Code: {Code})", call.Id, validationResult.Message, validationResult.Code);
                if (validationResult.Message != null && validationResult.Message.Contains("credit balance")) // A bit brittle check
                {
                    await _outboundCallQueueRepo.MoveToArchivedAsync(call.Id, CallQueueStatusEnum.Canceled,
                        new CallQueueLog { Message = $"Validation failed: {validationResult.Message}", Type = CallQueueLogTypeEnum.Error });
                }
                else // Concurrency or other temporary issue
                {
                    await _outboundCallQueueRepo.UpdateCallStatusAsync(call.Id, CallQueueStatusEnum.Queued,
                        new CallQueueLog { Message = $"Validation failed (will retry): {validationResult.Message}", Type = CallQueueLogTypeEnum.Info },
                        newProcessingServerId: null); // Reset ProcessingServerId
                }
                return;
            }

            // b. Select Backend Server
            // The call.RegionId should be the region of the RecipientNumber, used for selecting appropriate backend server
            var serverSelectionResult = await _serverSelectionManager.SelectOptimalServerAsync(call.RegionId);
            if (!serverSelectionResult.Success || !serverSelectionResult.Data.Any())
            {
                _logger.LogWarning("No optimal backend server found for call {QueueId} in region {RegionId}. Error: {Message}",
                    call.Id, call.RegionId, serverSelectionResult.Message);
                await _outboundCallQueueRepo.MoveToArchivedAsync(call.Id, CallQueueStatusEnum.Failed,
                    new CallQueueLog { Message = $"No backend server available for region {call.RegionId}. {serverSelectionResult.Message}", Type = CallQueueLogTypeEnum.Error });
                return;
            }

            // c. Forward to Backend (Iterate through optimal servers)
            RegionData? regionDetails = await _regionManager.GetRegionById(call.RegionId); // Region of the call (for API keys etc.)
            if (regionDetails == null)
            {
                _logger.LogError("Region details not found for {RegionId} during call {QueueId} processing.", call.RegionId, call.Id);
                await _outboundCallQueueRepo.MoveToArchivedAsync(call.Id, CallQueueStatusEnum.Failed,
                    new CallQueueLog { Message = $"System error: Region details for {call.RegionId} not found.", Type = CallQueueLogTypeEnum.Error });
                return;
            }

            bool successfullyForwarded = false;
            foreach (var optimalServer in serverSelectionResult.Data) // OptimalServer is your DTO for server endpoint/load
            {
                RegionServerData? backendServerDetails = regionDetails.Servers.FirstOrDefault(s => s.Endpoint == optimalServer.ServerEndpoint && s.Type == IqraCore.Entities.Helper.Server.ServerTypeEnum.CallProcessor);
                if (backendServerDetails == null)
                {
                    _logger.LogWarning("Details for backend server endpoint {Endpoint} not found in region {RegionId} configuration for call {QueueId}.",
                        optimalServer.ServerEndpoint, call.RegionId, call.Id);
                    continue;
                }

                var requestDto = new BackendOutboundCallRequest
                {
                    QueueId = call.Id,
                    BusinessId = call.BusinessId,
                    RegionId = call.RegionId, // Region of the call itself
                    CampaignId = call.CampaignId,
                    CallingNumberId = call.CallingNumberId,
                    RecipientNumber = call.RecipientNumber,
                    DynamicVariables = call.DynamicVariables,
                    AgentId = call.AgentId,
                    AgentScriptId = call.AgentScriptId,
                    AgentLanguageCode = call.AgentLanguageCode,
                    AgentTimeZone = call.AgentTimeZone
                };

                var forwardResponse = await ForwardToBackendAsync(backendServerDetails, requestDto);
                if (forwardResponse.Success)
                {
                    _logger.LogInformation("Successfully forwarded call {QueueId} to backend server {Endpoint}.", call.Id, backendServerDetails.Endpoint);
                    // Backend should ideally return its internal SessionId or ProviderCallId if known quickly
                    // For now, just mark as Processing. Backend can update ProviderCallId later via another API if needed.
                    await _outboundCallQueueRepo.UpdateCallStatusAsync(call.Id, CallQueueStatusEnum.Processing,
                        new CallQueueLog { Message = $"Forwarded to backend {backendServerDetails.Endpoint}.", Type = CallQueueLogTypeEnum.Info },
                        newProcessingServerId: backendServerDetails.Endpoint, // Update to backend server ID
                        providerCallId: null /* Or from forwardResponse if available */);
                    successfullyForwarded = true;
                    break; // Exit loop once successfully forwarded
                }
                else
                {
                    _logger.LogWarning("Failed to forward call {QueueId} to backend {Endpoint}: {Message} (Code: {Code})",
                        call.Id, backendServerDetails.Endpoint, forwardResponse.Message, forwardResponse.Code);
                    // Log in call queue for this attempt? Maybe too verbose.
                }
            }

            // d. Handle Overall Forwarding Failure
            if (!successfullyForwarded)
            {
                _logger.LogError("Failed to forward call {QueueId} to any backend server after trying all optimal options.", call.Id);
                await _outboundCallQueueRepo.MoveToArchivedAsync(call.Id, CallQueueStatusEnum.Failed,
                    new CallQueueLog { Message = "Failed to forward to any backend server.", Type = CallQueueLogTypeEnum.Error });
            }
        }

        private async Task<FunctionReturnResult> ForwardToBackendAsync(RegionServerData backendServer, BackendOutboundCallRequest requestDto)
        {
            var result = new FunctionReturnResult();
            string endpoint = backendServer.UseSSL ? "https://" : "http://";
            endpoint += backendServer.Endpoint;
            string apiPath = "/api/call/outbound/initiate"; // Make this configurable

            try
            {
                using var client = _httpClientFactory.CreateClient("OutboundCallForwardClient");
                client.Timeout = TimeSpan.FromSeconds(10); // Configurable
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(backendServer.APIKey))
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", backendServer.APIKey);
                }

                var jsonPayload = JsonSerializer.Serialize(requestDto, _camelCaseSerializationOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogDebug("Forwarding outbound call {QueueId} to {EndpointUrl}{ApiPath} with payload: {Payload}", requestDto.QueueId, endpoint, apiPath, jsonPayload);

                var response = await client.PostAsync(new Uri(new Uri(endpoint), apiPath), content);

                var responseContentString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error response from backend {EndpointUrl}{ApiPath} for call {QueueId}. Status: {StatusCode}. Response: {Response}",
                        endpoint, apiPath, requestDto.QueueId, response.StatusCode, responseContentString);
                    return result.SetFailureResult($"ForwardToBackend:{response.StatusCode}", $"Backend returned error: {response.StatusCode}. Details: {responseContentString}");
                }

                // Assuming backend returns a FunctionReturnResult or similar structure
                var backendResponse = JsonSerializer.Deserialize<FunctionReturnResult>(responseContentString, _camelCaseSerializationOptions);
                if (backendResponse == null || !backendResponse.Success)
                {
                    _logger.LogWarning("Backend {EndpointUrl}{ApiPath} indicated failure for call {QueueId}. Message: {BackendMessage}. Code: {BackendCode}. Raw: {Response}",
                        endpoint, apiPath, requestDto.QueueId, backendResponse?.Message, backendResponse?.Code, responseContentString);
                    return result.SetFailureResult(backendResponse?.Code ?? "BackendParseFail", backendResponse?.Message ?? "Backend failed to process or invalid response format.");
                }

                _logger.LogInformation("Backend {EndpointUrl}{ApiPath} successfully accepted call {QueueId}.", endpoint, apiPath, requestDto.QueueId);
                // Optionally, backendResponse.Data could contain ProviderCallId or BackendSessionId
                return result.SetSuccessResult(backendResponse.Data); // Passing data from backend if any
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request exception while forwarding call {QueueId} to {EndpointUrl}{ApiPath}.", requestDto.QueueId, endpoint, apiPath);
                return result.SetFailureResult("ForwardToBackend:HttpRequestError", $"HTTP request error: {httpEx.Message}");
            }
            catch (TaskCanceledException tex) // Catches timeouts
            {
                _logger.LogError(tex, "Timeout while forwarding call {QueueId} to {EndpointUrl}{ApiPath}.", requestDto.QueueId, endpoint, apiPath);
                return result.SetFailureResult("ForwardToBackend:Timeout", "Request to backend timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic exception while forwarding call {QueueId} to {EndpointUrl}{ApiPath}.", requestDto.QueueId, endpoint, apiPath);
                return result.SetFailureResult("ForwardToBackend:GenericError", $"Exception: {ex.Message}");
            }
        }
    }
}
