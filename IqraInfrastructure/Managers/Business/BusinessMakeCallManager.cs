using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Models.Business.MakeCalls;
using IqraInfrastructure.Managers.Region;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using PhoneNumbers;
using IqraCore.Entities.Helper.Call.Outbound;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Utilities;
using Azure.Core;
using Deepgram.Models.Manage.v1;
using nietras.SeparatedValues;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessMakeCallManager
    {
        private readonly ILogger<BusinessMakeCallManager> _logger;
        private readonly BusinessManager _parentBusinessManager;
        private readonly RegionManager _regionManager;
        private readonly IHttpClientFactory _httpClientFactory;

        public BusinessMakeCallManager(
            ILogger<BusinessMakeCallManager> logger,
            BusinessManager parentBusinessManager,
            RegionManager regionManager,
            IHttpClientFactory httpClientFactory
            )
        {
            _logger = logger;
            _parentBusinessManager = parentBusinessManager;
            _regionManager = regionManager;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<FunctionReturnResult> ForwardCallInitiationRequestAsync(BusinessData businessData, MakeCallRequestDto callConfig, IFormFile? bulkCsvFile)
        {
            var result = new FunctionReturnResult();

            long businessId = businessData.Id;
            string businessDefaultLanguage = businessData.DefaultLanguage;
            List<string> businessLanguages = businessData.Languages;

            // General
            if (callConfig.General == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:1",
                    "Missing 'General' in configuration."
                );
            }
            if (string.IsNullOrEmpty(callConfig.General.Identifier))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:2",
                    "Missing 'General.Identifier' in configuration."
                );
            }
            if (string.IsNullOrEmpty(callConfig.General.Description))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:3",
                    "Missing 'General.Description' in configuration."
                );
            }

            // Number Details
            if (callConfig.NumberDetails == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:4",
                    "Missing 'NumberDetails' in configuration."
                );
            }
            if (callConfig.NumberDetails.Type == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:5",
                    "Invalid 'NumberDetails.Type' in configuration."
                );
            }
            if (string.IsNullOrWhiteSpace(callConfig.NumberDetails.FromNumberId))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:6",
                    "Missing 'NumberDetails.FromNumberId' in configuration."
                );
            }
            BusinessNumberData? defaultFromCallNumberData = await _parentBusinessManager.GetNumberManager().GetBusinessNumberById(businessId, callConfig.NumberDetails.FromNumberId);
            if (defaultFromCallNumberData == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:7",
                    "Unable to find 'NumberDetails.FromNumberId' in business data."
                );
            }
            if (callConfig.NumberDetails.Type == OutboundCallNumberType.Single)
            {
                if (string.IsNullOrWhiteSpace(callConfig.NumberDetails.ToNumber))
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:8",
                        "Missing 'NumberDetails.ToNumber' in configuration for single call type."
                    );
                }

                PhoneNumber parsedPhoneNumber = PhoneNumberUtil.GetInstance().Parse(callConfig.NumberDetails.ToNumber, "ZZ");
                if (!PhoneNumberUtil.GetInstance().IsValidNumber(parsedPhoneNumber))
                {
                    result.Code = "ForwardCallInitiationRequestAsync:9";
                    result.Message = "Number validation failed for 'NumberDetails.ToNumber'.";
                    return result;
                }
            }
            else if (callConfig.NumberDetails.Type == OutboundCallNumberType.Bulk)
            {
                if (bulkCsvFile == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:10",
                        "Missing 'bulk_file' for bulk call type."
                    );
                }
                // we validate the bulk calls at the end of the class validation
            }
            // Configuration
            if (callConfig.Configuration == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:12",
                    "Missing 'Configuration' in configuration."
                );
            }
            // Configuration.Schedule
            if (callConfig.Configuration.Schedule == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:13",
                    "Missing 'Configuration.Schedule' in configuration."
                );
            }
            if (callConfig.Configuration.Schedule.Type == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:14",
                    "Missing 'Configuration.Schedule.Type' in configuration."
                );
            }
            if (callConfig.Configuration.Schedule.Type == OutboundCallScheduleType.Scheduled)
            {
                if (callConfig.Configuration.Schedule.DateTimeUTC == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:15",
                        "Missing 'Configuration.Schedule.DateTimeUTC' in configuration."
                    );
                }

                if (callConfig.Configuration.Schedule.DateTimeUTC.Value.AddMinutes(10) < DateTime.UtcNow)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:16",
                        "Cannot schedule call in the past (must be atleast 10 minutes in the future)."
                    );
                }
            }
            // Configuration.RetryDecline
            if (callConfig.Configuration.RetryDecline == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:17",
                    "Missing 'Configuration.RetryDecline' in configuration."
                );
            }
            if (callConfig.Configuration.RetryDecline.Enabled == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:18",
                    "Missing 'Configuration.RetryDecline.Enabled' in configuration."
                );
            }
            if (callConfig.Configuration.RetryDecline.Enabled == true)
            {
                if (callConfig.Configuration.RetryDecline.Count == null || callConfig.Configuration.RetryDecline.Count < 1 || callConfig.Configuration.RetryDecline.Count > 5) // todo can have retires max count based on user/business
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:19",
                        "Missing or invalid 'Configuration.RetryDecline.Count' in configuration. Must be between 1 and 5."
                    );
                }
                if (callConfig.Configuration.RetryDecline.Delay == null || callConfig.Configuration.RetryDecline.Delay < 1)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:20",
                        "Missing or invalid 'Configuration.RetryDecline.Delay' in configuration. Must be greater than 0."
                    );
                }
                if (callConfig.Configuration.RetryDecline.Unit == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:21",
                        "Missing or invalid 'Configuration.RetryDecline.Unit' in configuration."
                    );
                }
            }
            // Configuration.RetryMiss
            if (callConfig.Configuration.RetryMiss == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:22",
                    "Missing 'Configuration.RetryMiss' in configuration."
                );
            }
            if (callConfig.Configuration.RetryMiss.Enabled == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:23",
                    "Missing 'Configuration.RetryMiss.Enabled' in configuration."
                );
            }
            if (callConfig.Configuration.RetryMiss.Enabled == true)
            {
                if (callConfig.Configuration.RetryMiss.Count == null || callConfig.Configuration.RetryMiss.Count < 1 || callConfig.Configuration.RetryMiss.Count > 5) // todo can have retires max count based on user/business, used below in a function too
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:24",
                        "Missing or invalid 'Configuration.RetryMiss.Count' in configuration. Must be between 1 and 5."
                    );
                }
                if (callConfig.Configuration.RetryMiss.Delay == null || callConfig.Configuration.RetryMiss.Delay < 1)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:25",
                        "Missing or invalid 'Configuration.RetryMiss.Delay' in configuration. Must be greater than 0."
                    );
                }
                if (callConfig.Configuration.RetryMiss.Unit == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:26",
                        "Missing or invalid 'Configuration.RetryMiss.Unit' in configuration."
                    );
                }
            }
            // Configuration.Timeouts
            if (callConfig.Configuration.Timeouts == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:27",
                    "Missing 'Configuration.Timeouts' in configuration."
                );
            }
            if (callConfig.Configuration.Timeouts.NotifyOnSilenceMS == null || callConfig.Configuration.Timeouts.NotifyOnSilenceMS < 0)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:28",
                    "Missing or invalid 'Configuration.Timeouts.NotifyOnSilenceMS' in configuration. Set 0 for disabled."
                );
            }
            if (callConfig.Configuration.Timeouts.EndOnSilenceMS == null || callConfig.Configuration.Timeouts.EndOnSilenceMS < 0)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:29",
                    "Missing or invalid 'Configuration.Timeouts.EndOnSilenceMS' in configuration. Set 0 for disabled."
                );
            }
            if (callConfig.Configuration.Timeouts.MaxCallTimeS == null || callConfig.Configuration.Timeouts.MaxCallTimeS < 1 || callConfig.Configuration.Timeouts.MaxCallTimeS > 1800) // this is also used in BusinessRoutings so we should make this a const
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:30",
                    "Missing or invalid 'Configuration.Timeouts.CallTimeoutMS' in configuration. Must be between 1 and 1800 seconds."
                );
            }
            // AgentSettings
            if (callConfig.AgentSettings == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:31",
                    "Missing 'AgentSettings' in configuration."
                );
            }
            if (string.IsNullOrWhiteSpace(callConfig.AgentSettings.AgentId))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:32",
                    "Missing or invalid 'AgentSettings.AgentId' in configuration."
                );
            }
            BusinessAppAgent? defaultAgentData = await _parentBusinessManager.GetAgentsManager().GetAgentById(businessId, callConfig.AgentSettings.AgentId);
            if (defaultAgentData == null) {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:33",
                    $"Default selected agent with id {callConfig.AgentSettings.AgentId} not found in business data."
                );
            }
            if (string.IsNullOrWhiteSpace(callConfig.AgentSettings.ScriptId))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:34",
                    "Missing or invalid 'AgentSettings.ScriptId' in configuration."
                );
            }
            BusinessAppAgentScript? selectedDefaultAgentScriptData = defaultAgentData.Scripts.Find(s => s.Id == callConfig.AgentSettings.ScriptId);
            if (selectedDefaultAgentScriptData == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:35",
                    $"Default selected agent selected script with id {callConfig.AgentSettings.ScriptId} not found in business data."
                );
            }
            if (string.IsNullOrEmpty(callConfig.AgentSettings.LanguageCode))
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:36",
                    "Missing or invalid 'AgentSettings.LanguageCode' in configuration."
                );
            }
            if (businessLanguages.Contains(callConfig.AgentSettings.LanguageCode) == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:37",
                    $"Language code '{callConfig.AgentSettings.LanguageCode}' not found in business languages."
                );
            }
            if (callConfig.AgentSettings.Timezones == null || callConfig.AgentSettings.Timezones.Count == 0)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:38",
                    "Missing or invalid 'AgentSettings.Timezones' in configuration."
                );
            }
            foreach (var timezone in callConfig.AgentSettings.Timezones)
            {
                if (TimeZoneHelper.ValidateOffsetString(timezone) == false)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:38.1",
                        $"Failed to validate 'AgentSettings.Timezones' in configuration. Timezone: {timezone}"
                    );
                }
            }    
            if (callConfig.AgentSettings.IncludeFromNumberInContext == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:39",
                    "Missing or invalid 'AgentSettings.IncludeFromNumberInContext' in configuration."
                );
            }
            if (callConfig.AgentSettings.IncludeToNumberInContext == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:40",
                    "Missing or invalid 'AgentSettings.IncludeToNumberInContext' in configuration."
                );
            }
            // AgentSettings.Interruption
            if (callConfig.AgentSettings.Interruption == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:41",
                    "Missing 'AgentSettings.Interruption' in configuration."
                );
            }
            if (callConfig.AgentSettings.Interruption.Type == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:42",
                    "Missing or invalid 'AgentSettings.Interruption.Type' in configuration."
                );
            }
            if (callConfig.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.TurnByTurn)
            {
                if (callConfig.AgentSettings.Interruption.UseInterruptedResponseInNextTurn == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:43",
                        "Missing or invalid 'AgentSettings.Interruption.UseInterruptedResponseInNextTurn' in configuration for intertuption type 'Turn by Turn'."
                    );
                }
            }
            else if (callConfig.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.InterruptibleViaVAD)
            {
                if (callConfig.AgentSettings.Interruption.VadDurationMS == null || callConfig.AgentSettings.Interruption.VadDurationMS < 100) // todo this is also set in BusinessRoutingsmanager, make it const
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:44",
                        "Missing or invalid 'AgentSettings.Interruption.VadDurationMS' in configuration. Must be at least 100 ms."
                    );
                }
            }
            else if (callConfig.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.InterruptibleViaAI)
            {
                if (callConfig.AgentSettings.Interruption.UseAgentLLM == null)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:45",
                        "Missing or invalid 'AgentSettings.Interruption.UseAgentLLM' in configuration for intertuption type 'Via AI'."
                    );
                }
                if (callConfig.AgentSettings.Interruption.UseAgentLLM == false)
                {
                    if (string.IsNullOrWhiteSpace(callConfig.AgentSettings.Interruption.LLMIntegrationId))
                    {
                        return result.SetFailureResult(
                            "ForwardCallInitiationRequestAsync:46",
                            "Missing or invalid 'AgentSettings.Interruption.LlmIntegrationId' in configuration for intertuption type 'Via AI' when 'UseAgentLLM' is false."
                        );
                    }

                    // todo check if llm integration exists
                    // validate integration config

                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:47",
                        "Interruption type 'Via AI' not implemented yet."
                    );
                }
            }
            // Actions
            if (callConfig.Actions == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:48",
                    "Missing 'Actions' in configuration."
                );
            }
            // Actions.Declines
            if (callConfig.Actions.Declined == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:49",
                    "Missing 'Actions.Declined' in configuration."
                );
            }
            var ringingToolValidationResult = await ValidateActionData(businessId, businessDefaultLanguage, callConfig.Actions.Declined, "Declined");
            if (ringingToolValidationResult.Success == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:" + ringingToolValidationResult.Code,
                    ringingToolValidationResult.Message
                );
            }
            // Actions.Missed
            if (callConfig.Actions.Missed == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:50",
                    "Missing 'Actions.Missed' in configuration."
                );
            }
            ringingToolValidationResult = await ValidateActionData(businessId, businessDefaultLanguage, callConfig.Actions.Missed, "Missed");
            if (ringingToolValidationResult.Success == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:" + ringingToolValidationResult.Code,
                    ringingToolValidationResult.Message
                );
            }
            // Actions.Answered
            if (callConfig.Actions.Answered == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:51",
                    "Missing 'Actions.Answered' in configuration."
                );
            }
            ringingToolValidationResult = await ValidateActionData(businessId, businessDefaultLanguage, callConfig.Actions.Answered, "Answered");
            if (ringingToolValidationResult.Success == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:" + ringingToolValidationResult.Code,
                    ringingToolValidationResult.Message
                );
            }
            // Actions.Ended
            if (callConfig.Actions.Ended == null)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:52",
                    "Missing 'Actions.Ended' in configuration."
                );
            }
            ringingToolValidationResult = await ValidateActionData(businessId, businessDefaultLanguage, callConfig.Actions.Ended, "Ended");
            if (ringingToolValidationResult.Success == false)
            {
                return result.SetFailureResult(
                    "ForwardCallInitiationRequestAsync:" + ringingToolValidationResult.Code,
                    ringingToolValidationResult.Message
                );
            }

            if (callConfig.NumberDetails.Type == OutboundCallNumberType.Single)
            {
                var singleForwardResult = await ForwardSingleCallToRegionProxy(businessData, callConfig, defaultAgentData, defaultFromCallNumberData);
                if (!singleForwardResult.Success)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + singleForwardResult.Code,
                        singleForwardResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            else if (callConfig.NumberDetails.Type == OutboundCallNumberType.Bulk)
            {
                FunctionReturnResult<(List<OutboundBulkCallRowData> callsRows, Dictionary<string, string> numberRegions)?> bulkCallFileResult = await ValidateAndBuildBulkCSVCallFile(businessData, bulkCsvFile!, callConfig, defaultFromCallNumberData, defaultAgentData);
                if (!bulkCallFileResult.Success)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + bulkCallFileResult.Code,
                        bulkCallFileResult.Message
                    );
                }

                var bulkForwardResult = await ForwardBulkCallsToRegionProxies(businessData, callConfig, defaultAgentData, defaultFromCallNumberData, bulkCallFileResult.Data!.Value.callsRows, bulkCallFileResult.Data!.Value.numberRegions);
                if (!bulkForwardResult.Success)
                {
                    return result.SetFailureResult(
                        "ForwardCallInitiationRequestAsync:" + bulkForwardResult.Code,
                        bulkForwardResult.Message
                    );
                }
            }

            return result.SetSuccessResult();
        }

        private async Task<FunctionReturnResult> ForwardSingleCallToRegionProxy(BusinessData businessData, MakeCallRequestDto callConfig, BusinessAppAgent businessAppAgent, BusinessNumberData businessNumberData)
        {
            var result = new FunctionReturnResult();

            var selectedRegion = businessNumberData.RegionId;
            var regionData = await _regionManager.GetRegionById(selectedRegion);
            if (regionData == null)
            {
                return result.SetFailureResult(
                    "ForwardSingleCallToRegionProxy:1",
                    "Phone number region not found."
                );
            }

            var anyProxyServerForRegion = regionData.Servers.Find(s => s.Type == ServerTypeEnum.Proxy);
            if (anyProxyServerForRegion == null)
            {
                return result.SetFailureResult(
                    "ForwardSingleCallToRegionProxy:2",
                    "No proxy server found for phone number region."
                );
            }

            var proxyServerEndpoint = anyProxyServerForRegion.Endpoint;
            var proxyServerUseSSL = anyProxyServerForRegion.UseSSL;
            var proxyServerApiKey = anyProxyServerForRegion.APIKey;

            var proxyServerCallURI = new Uri((proxyServerUseSSL ? "https://" : "http://") + proxyServerEndpoint);
            proxyServerCallURI = new Uri(proxyServerCallURI, "/api/makecall/single");

            string configJson = JsonSerializer.Serialize(callConfig);
            var requestContent = new StringContent(configJson, Encoding.UTF8, "application/json");

            using (var client = _httpClientFactory.CreateClient("ProxyForwarder"))
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("X-API-Key", proxyServerApiKey);

                HttpResponseMessage response = await client.PostAsync(proxyServerCallURI, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();

                    FunctionReturnResult? resultError = null;
                    try
                    {
                        resultError = JsonSerializer.Deserialize<FunctionReturnResult>(error);
                    }
                    catch {
                        // ignore failure for now
                    }

                    if (resultError == null)
                    {
                        return result.SetFailureResult(
                            "ForwardSingleCallToRegionProxy:3",
                            "Failed to forward call to region proxy server. Error: " + error
                        );
                    }

                    return result.SetFailureResult(
                        "ForwardSingleCallToRegionProxy:" + resultError.Code,
                        resultError.Message
                    );
                }

                return result.SetSuccessResult();
            }
        }

        private async Task<FunctionReturnResult> ForwardBulkCallsToRegionProxies(BusinessData businessData, MakeCallRequestDto callConfig, BusinessAppAgent businessAppAgent, BusinessNumberData businessNumberData, List<OutboundBulkCallRowData> callsRows, Dictionary<string, string> numberRegions)
        {
            var result = new FunctionReturnResult();



            return result.SetSuccessResult();
        }

        //private async Task<FunctionReturnResult> ForwardCallCampaignToBackend()
        //{
        //    // --- Forwarding ---
        //    string targetUrl;
        //    HttpContent requestContent;
        //    bool isBulk = bulkCsvFile != null;

        //    try
        //    {
        //        if (isBulk)
        //        {
        //            targetUrl = $"{proxyEndpoint.TrimEnd('/')}/api/makecall/bulk";
        //            var multipartContent = new MultipartFormDataContent();

        //            // Add config JSON part
        //            string configJson = JsonSerializer.Serialize(callConfig);
        //            multipartContent.Add(new StringContent(configJson, Encoding.UTF8, "application/json"), "config");

        //            // Add file part
        //            var fileStreamContent = new StreamContent(bulkCsvFile!.OpenReadStream());
        //            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv"); // Set content type
        //            multipartContent.Add(fileStreamContent, "bulk_file", bulkCsvFile.FileName);

        //            requestContent = multipartContent;
        //        }
        //        else
        //        {
        //            targetUrl = $"{proxyEndpoint.TrimEnd('/')}/api/makecall/single";
        //            string configJson = JsonSerializer.Serialize(callConfig);
        //            requestContent = new StringContent(configJson, Encoding.UTF8, "application/json");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error preparing request content for proxy forwarding.");
        //        result.Code = "ForwardCall:7"; result.Message = "Error preparing request data."; return result;
        //    }

        //    try
        //    {
        //        using var client = _httpClientFactory.CreateClient("ProxyForwarder"); // Use named client
        //        client.DefaultRequestHeaders.Clear();
        //        client.DefaultRequestHeaders.Add("X-API-Key", proxyApiKey);
        //        if (!isBulk) client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        _logger.LogInformation("Forwarding call initiation request to {TargetUrl}", targetUrl);

        //        HttpResponseMessage response = await client.PostAsync(targetUrl, requestContent);

        //        // --- Response Handling ---
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            string errorBody = await response.Content.ReadAsStringAsync();
        //            _logger.LogWarning("Proxy server at {TargetUrl} returned error {StatusCode}. Body: {ErrorBody}", targetUrl, response.StatusCode, errorBody);
        //            result.Code = $"ForwardCall:Proxy{(int)response.StatusCode}";
        //            // Try to parse error response from proxy if it follows FunctionReturnResult structure
        //            try
        //            {
        //                var proxyErrorResult = JsonSerializer.Deserialize<FunctionReturnResult<object>>(errorBody);
        //                result.Message = $"Proxy Error: {proxyErrorResult?.Message ?? response.ReasonPhrase}";
        //            }
        //            catch
        //            {
        //                result.Message = $"Proxy returned status {response.StatusCode}: {response.ReasonPhrase}";
        //            }

        //            // Clean up multipart content if created
        //            if (isBulk) requestContent?.Dispose();

        //            return result;
        //        }

        //        // Success from Proxy
        //        var responseBody = await response.Content.ReadAsStringAsync();
        //        _logger.LogInformation("Received successful response from proxy: {ResponseBody}", responseBody);
        //        try
        //        {
        //            // Assume proxy returns FunctionReturnResult<T> structure
        //            var proxyResult = JsonSerializer.Deserialize<FunctionReturnResult<object>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        //            result.Success = proxyResult?.Success ?? true; // Assume success if proxy returned 2xx and valid JSON
        //            result.Message = proxyResult?.Message ?? "Request processed by proxy.";
        //            result.Data = proxyResult?.Data; // Forward any data returned by the proxy
        //        }
        //        catch (JsonException jsonEx)
        //        {
        //            _logger.LogWarning(jsonEx, "Failed to deserialize successful proxy response body: {ResponseBody}", responseBody);
        //            result.Success = true; // Still treat as overall success because proxy returned 2xx
        //            result.Message = "Request processed by proxy (response format unclear).";
        //            result.Data = responseBody; // Return raw response body as data
        //        }

        //        // Clean up multipart content if created
        //        if (isBulk) requestContent?.Dispose();

        //        return result;

        //    }
        //    catch (HttpRequestException httpEx)
        //    {
        //        _logger.LogError(httpEx, "HTTP error forwarding request to proxy {TargetUrl}", targetUrl);
        //        result.Code = "ForwardCall:HttpErr"; result.Message = $"Error communicating with proxy server: {httpEx.Message}";
        //        if (isBulk) requestContent?.Dispose();
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Unexpected error forwarding request to proxy {TargetUrl}", targetUrl);
        //        result.Code = "ForwardCall:Ex"; result.Message = $"Internal error forwarding request: {ex.Message}";
        //        if (isBulk) requestContent?.Dispose();
        //        return result;
        //    }
        //}

        private async Task<FunctionReturnResult<(List<OutboundBulkCallRowData> callsRows, Dictionary<string, string> numberRegions)?>> ValidateAndBuildBulkCSVCallFile(BusinessData businessData, IFormFile formFile, MakeCallRequestDto callConfig, BusinessNumberData defaultCallNumber, BusinessAppAgent defaultCallAgent)
        {
            var result = new FunctionReturnResult<(List<OutboundBulkCallRowData> callsRows, Dictionary<string, string> numberRegions)?>();

            long businessId = businessData.Id;

            var cachedBusinessNumberRegionData = new Dictionary<string, string>();
            // add default number and region to cache
            cachedBusinessNumberRegionData.Add(defaultCallNumber.Id.ToString(), defaultCallNumber.RegionId);

            var innerCachedBusinessAgentsScriptsData = new Dictionary<string, List<string>>();
            // add default agent and scripts ids to cache
            innerCachedBusinessAgentsScriptsData.Add(defaultCallAgent.Id.ToString(), defaultCallAgent.Scripts.Select(s => s.Id).ToList<string>());

            try
            {
                var rowsDataList = new List<OutboundBulkCallRowData>();

                using (var reader = Sep.Reader(o => o with { HasHeader = true, Sep = Sep.New(','), DisableColCountCheck = false }).From(formFile.OpenReadStream()))
                {
                    var header = reader.Header;
                    if (header.ColNames.Count != 9)
                    {
                        return result.SetFailureResult(
                            "ValidateAndBuildBulkCSVCallFile:1",
                            "Invalid number of columns in CSV file."
                        );
                    }

                    // Skip header
                    await reader.MoveNextAsync();
                    foreach (SepReader.Row readRow in reader)
                    {
                        var currentOutboundCallRow = new OutboundBulkCallRowData();
                        var currentRowLine = readRow.LineNumberFrom;

                        string? from_number_id;
                        string? to_number;
                        string? dynamic_variables;
                        string? override_retry_on_call_declined;
                        string? override_retry_on_missed_call;
                        string? override_agent_id;
                        string? override_agent_script_id;
                        string? override_agent_language_code;
                        string? override_agent_timezones;

                        try
                        {
                            from_number_id = readRow["from_number_id"].ToString();
                            if (!string.IsNullOrWhiteSpace(from_number_id))
                            {
                                if (!cachedBusinessNumberRegionData.ContainsKey(from_number_id))
                                {
                                    BusinessNumberData? defaultFromCallNumberData = await _parentBusinessManager.GetNumberManager().GetBusinessNumberById(businessId, from_number_id);
                                    if (defaultFromCallNumberData == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:3",
                                            $"Unable to find number {from_number_id} in row {currentRowLine} in business data."
                                        );
                                    }
                                    cachedBusinessNumberRegionData.Add(from_number_id, defaultFromCallNumberData.RegionId);
                                }
                                currentOutboundCallRow.FromNumberId = from_number_id;
                            }

                            to_number = readRow["to_number"].ToString();
                            if (string.IsNullOrWhiteSpace(to_number))
                            {
                                return result.SetFailureResult(
                                    "ValidateAndBuildBulkCSVCallFile:4",
                                    $"Missing 'to_number' in row {currentRowLine}."
                                );
                            }
                            PhoneNumber parsedPhoneNumber = PhoneNumberUtil.GetInstance().Parse(to_number, "ZZ");
                            if (!PhoneNumberUtil.GetInstance().IsValidNumber(parsedPhoneNumber))
                            {
                                return result.SetFailureResult(
                                    "ValidateAndBuildBulkCSVCallFile:5",
                                    $"Number validation failed for 'to_number' in row {currentRowLine}."
                                );
                            }
                            currentOutboundCallRow.ToNumber = to_number;

                            dynamic_variables = readRow["dynamic_variables"].ToString();
                            Dictionary<string, string>? dynamicVariablesDictionary = null;
                            if (!string.IsNullOrWhiteSpace(dynamic_variables))
                            {
                                try
                                {
                                    dynamicVariablesDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(dynamic_variables);
                                    if (dynamicVariablesDictionary == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:6",
                                            $"Error deserializing dynamic variables for row {currentRowLine}."
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:7",
                                        $"Error deserializing dynamic variables for row {currentRowLine}: {ex.Message}"
                                    );
                                }
                            }
                            currentOutboundCallRow.DynamicVariables = dynamicVariablesDictionary;

                            override_retry_on_call_declined = readRow["override_retry_on_call_declined"].ToString();
                            OutboundBulkCallRowDataRetryData? outboundBulkCallRowDataRetryDeclinedData = null;
                            if (!string.IsNullOrWhiteSpace(override_retry_on_call_declined))
                            {
                                try
                                {
                                    outboundBulkCallRowDataRetryDeclinedData = JsonSerializer.Deserialize<OutboundBulkCallRowDataRetryData>(override_retry_on_call_declined);
                                    if (outboundBulkCallRowDataRetryDeclinedData == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:8",
                                            $"Error deserializing retry declined data for row {currentRowLine}."
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:9",
                                        $"Error deserializing retry declined data for row {currentRowLine}: {ex.Message}"
                                    );
                                }
                                if (outboundBulkCallRowDataRetryDeclinedData.Enabled == null)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:10",
                                        $"Missing 'Enabled' in retry declined data for row {currentRowLine}."
                                    );
                                }
                                if (outboundBulkCallRowDataRetryDeclinedData.Enabled == true)
                                {
                                    if (outboundBulkCallRowDataRetryDeclinedData.Count == null || outboundBulkCallRowDataRetryDeclinedData.Count < 1 || outboundBulkCallRowDataRetryDeclinedData.Count > 5) // todo can have retires max count based on user/business, used below in a function too
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:11",
                                            $"Missing or invalid 'Count' in retry declined data for row {currentRowLine}. Should be between 1 and 5."
                                        );
                                    }
                                    if (outboundBulkCallRowDataRetryDeclinedData.Delay == null || outboundBulkCallRowDataRetryDeclinedData.Delay < 1)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:12",
                                            $"Missing or invalid 'Delay' in retry declined data for row {currentRowLine}. Should be greater than 0."
                                        );
                                    }
                                    if (outboundBulkCallRowDataRetryDeclinedData.Unit == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:13",
                                            $"Missing or invalid 'Unit' in retry declined data for row {currentRowLine}."
                                        );
                                    }
                                }
                            }
                            currentOutboundCallRow.OverrideRetryCallDeclinedData = outboundBulkCallRowDataRetryDeclinedData;

                            override_retry_on_missed_call = readRow["override_retry_on_missed_call"].ToString();
                            OutboundBulkCallRowDataRetryData? outboundBulkCallRowDataRetryMissedData = null;
                            if (!string.IsNullOrWhiteSpace(override_retry_on_missed_call))
                            {
                                try
                                {
                                    outboundBulkCallRowDataRetryMissedData = JsonSerializer.Deserialize<OutboundBulkCallRowDataRetryData>(override_retry_on_missed_call);
                                    if (outboundBulkCallRowDataRetryMissedData == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:14",
                                            $"Error deserializing retry missed call data for row {currentRowLine}."
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:15",
                                        $"Error deserializing retry missed call data for row {currentRowLine}: {ex.Message}"
                                    );
                                }
                                if (outboundBulkCallRowDataRetryMissedData.Enabled == null)
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:16",
                                        $"Missing 'Enabled' in retry missed call data for row {currentRowLine}."
                                    );
                                }
                                if (outboundBulkCallRowDataRetryMissedData.Enabled == true)
                                {
                                    if (outboundBulkCallRowDataRetryMissedData.Count == null || outboundBulkCallRowDataRetryMissedData.Count < 1 || outboundBulkCallRowDataRetryMissedData.Count > 5) // todo can have retires max count based on user/business, used below in a function too
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:11",
                                            $"Missing or invalid 'Count' in retry missed data for row {currentRowLine}. Should be between 1 and 5."
                                        );
                                    }
                                    if (outboundBulkCallRowDataRetryMissedData.Delay == null || outboundBulkCallRowDataRetryMissedData.Delay < 1)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:12",
                                            $"Missing or invalid 'Delay' in retry missed data for row {currentRowLine}. Should be greater than 0."
                                        );
                                    }
                                    if (outboundBulkCallRowDataRetryMissedData.Unit == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:13",
                                            $"Missing or invalid 'Unit' in retry missed data for row {currentRowLine}."
                                        );
                                    }
                                }
                            }
                            currentOutboundCallRow.OverrideRetryCallMissedData = outboundBulkCallRowDataRetryMissedData;

                            override_agent_id = readRow["override_agent_id"].ToString();
                            if (!string.IsNullOrWhiteSpace(override_agent_id))
                            {
                                if (!innerCachedBusinessAgentsScriptsData.ContainsKey(override_agent_id))
                                {
                                    BusinessAppAgent? agent = await _parentBusinessManager.GetAgentsManager().GetAgentById(businessId, override_agent_id);
                                    if (agent == null)
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:17",
                                            $"Agent with id {override_agent_id} not found in business for row {currentRowLine}."
                                        );
                                    }
                                    innerCachedBusinessAgentsScriptsData.Add(override_agent_id, agent.Scripts.Select(s => s.Id).ToList<string>());
                                }
                                currentOutboundCallRow.OverrideAgentId = override_agent_id;
                            }

                            override_agent_script_id = readRow["override_agent_script_id"].ToString();
                            if (!string.IsNullOrWhiteSpace(override_agent_script_id))
                            {
                                string agentIdToCheckAgainst = string.IsNullOrWhiteSpace(override_agent_id) ? defaultCallAgent.Id : override_agent_id;
                                if (!innerCachedBusinessAgentsScriptsData.TryGetValue(agentIdToCheckAgainst, out List<string>? overrideAgentScripts) || !overrideAgentScripts.Contains(override_agent_script_id))
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:18",
                                        $"Agent {agentIdToCheckAgainst} does not have script with id {override_agent_script_id} not found in business for row {currentRowLine}."
                                    );
                                }
                                currentOutboundCallRow.OverrideSelectedAgentScriptId = override_agent_script_id;
                            }

                            override_agent_language_code = readRow["override_agent_language_code"].ToString();
                            if (!string.IsNullOrEmpty(override_agent_language_code))
                            {
                                if (!businessData.Languages.Contains(override_agent_language_code))
                                {
                                    return result.SetFailureResult(
                                        "ValidateAndBuildBulkCSVCallFile:19",
                                        $"Agent language code {override_agent_language_code} not found in business for row {currentRowLine}."
                                    );
                                }
                                currentOutboundCallRow.OverrideAgentLanguageCode = override_agent_language_code;
                            }

                            override_agent_timezones = readRow["override_agent_timezones"].ToString();
                            if (!string.IsNullOrEmpty(override_agent_timezones))
                            {
                                List<string> timezonesSplit = override_agent_timezones.Split(',').ToList();
                                foreach (string zone in timezonesSplit)
                                {
                                    if (!TimeZoneHelper.ValidateOffsetString(zone))
                                    {
                                        return result.SetFailureResult(
                                            "ValidateAndBuildBulkCSVCallFile:20",
                                            $"Agent timezone {zone} validation failed for row {currentRowLine}."
                                        );
                                    }
                                }
                                currentOutboundCallRow.OverrideAgentTimezones = timezonesSplit;
                            }

                            rowsDataList.Add(currentOutboundCallRow);
                        }
                        catch (Exception ex)
                        {
                            return result.SetFailureResult(
                                "ValidateAndBuildBulkCSVCallFile:",
                                $"Error reading row {currentRowLine}: {ex.Message}"
                            );
                        }
                    }
                }

                return result.SetSuccessResult(
                    (rowsDataList, cachedBusinessNumberRegionData)
                );
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ValidateAndBuildBulkCSVCallFile:-1",
                    $"Error reading CSV file: {ex.Message}"
                );
            }


        }

        private async Task<FunctionReturnResult> ValidateActionData(long businessId, string businessDefaultLanguage, MakeCallActionToolConfigDto data, string actionType)
        {
            var result = new FunctionReturnResult();

            string? selectedToolId = data.ToolId;
            if (selectedToolId == null) return result.SetSuccessResult();

            var selectedToolData = await _parentBusinessManager.GetToolsManager().GetBusinessAppTool(businessId, selectedToolId);
            if (selectedToolData == null)
            {
                result.Code = "ValidateActionData:1";
                result.Message = $"{actionType} tool not found in business.";
                return result;
            }

            Dictionary<string, object>? argumentsList = data.Arguments;
            if (argumentsList == null) argumentsList = new Dictionary<string, object>();

            foreach (var toolInputArgument in selectedToolData.Configuration.InputSchemea)
            {
                bool foundProperty = argumentsList.TryGetValue(toolInputArgument.Id, out var argumentValueProperty);

                if (!foundProperty && toolInputArgument.IsRequired)
                {
                    return result.SetFailureResult(
                        "ValidateActionData:2",
                        $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} not found but is required."
                    );
                }
                else if (foundProperty)
                {
                    // Handle Array Type
                    if (toolInputArgument.IsArray)
                    {
                        if (argumentValueProperty.GetType() != typeof(object[]))
                        {
                            return result.SetFailureResult(
                                "ValidateActionData:4",
                                $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} should be an array."
                            );
                        }

                        var arrayValuesCount = 0;
                        foreach (var arrayElement in argumentValueProperty as object[])
                        {
                            var validationResult = BusinessAppToolPropertyValidator.ValidateArgumentValue(businessDefaultLanguage, arrayElement, toolInputArgument, actionType);
                            if (!validationResult.Success)
                            {
                                return result.SetFailureResult(
                                    "ValidateActionData:" + validationResult.Code,
                                    validationResult.Message
                                );
                            }
                            arrayValuesCount++;
                        }

                        if (toolInputArgument.IsRequired && arrayValuesCount == 0)
                        {
                            return result.SetFailureResult(
                                "ValidateActionData:5",
                                $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} array cannot be empty as it is required."
                            );
                        }
                    }
                    // Handle Single Value
                    else
                    {
                        var validationResult = BusinessAppToolPropertyValidator.ValidateArgumentValue(businessDefaultLanguage, argumentValueProperty, toolInputArgument, actionType);
                        if (!validationResult.Success)
                        {
                            return result.SetFailureResult(
                                "ValidateActionData:" + validationResult.Code,
                                validationResult.Message
                            );
                        }
                    }
                }
            }

            return result.SetSuccessResult();
        }
    }
}
