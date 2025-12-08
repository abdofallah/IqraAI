using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.WebSession;
using IqraCore.Models.Server;
using IqraCore.Models.WebSession;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.WebSession;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessWebSessionManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly WebSessionRepository _webSessionRepoistory;
        private readonly UserUsageValidationManager _billingValidationManager;
        private readonly ServerSelectionManager _serverSelectionManager;
        private readonly RegionManager _regionManager;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly JsonSerializerOptions _camelCaseSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public BusinessWebSessionManager(
            BusinessManager parentManager,
            WebSessionRepository webSessionRepoistory,
            UserUsageValidationManager billingValidationManager,
            ServerSelectionManager serverSelectionManager,
            RegionManager regionManager,
            IHttpClientFactory httpClientFactory
        ) {
            _parentBusinessManager = parentManager;
            _webSessionRepoistory = webSessionRepoistory;
            _billingValidationManager = billingValidationManager;
            _serverSelectionManager = serverSelectionManager;
            _regionManager = regionManager;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<FunctionReturnResult<InitiateWebSessionResultModel?>> InitiateWebSession(BusinessData businessData, InitiateWebSessionRequestModel modelData)
        {
            var result = new FunctionReturnResult<InitiateWebSessionResultModel?>();

            try
            {
                WebSessionData newWebSessionData = new WebSessionData()
                {
                    Id = Guid.NewGuid().ToString(),
                    BusinessId = businessData.Id,
                    CreatedAt = DateTime.UtcNow,
                    Status = WebSessionStatusEnum.Queued,
                    Logs = new List<WebSessionLog>(),
                };
                BusinessAppWebCampaign webCampaignData;

                // Web Campaign Id
                if (string.IsNullOrWhiteSpace(modelData.WebCampaignId))
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:CONFIG_WEB_CAMPAIGN_ID_NOT_FOUND",
                        "Web Campaign ID not found in config data."
                    );
                }
                else
                {
                    var campaignDataResult = await _parentBusinessManager.GetCampaignManager().GetWebCampaignById(businessData.Id, modelData.WebCampaignId);
                    if (!campaignDataResult.Success && campaignDataResult.Data != null)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CAMPAIGN_NOT_FOUND",
                            "Campaign not found in business."
                        );
                    }

                    webCampaignData = campaignDataResult.Data!;
                    newWebSessionData.WebCampaignId = modelData.WebCampaignId;
                }

                // Region Id
                if (string.IsNullOrWhiteSpace(modelData.RegionId))
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:CONFIG_REGION_ID_NOT_FOUND",
                        "Region ID not found in config data."
                    );
                }
                else
                {
                    var regionDataResult = await _regionManager.GetRegionById(modelData.RegionId);
                    if (regionDataResult == null)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:REGION_NOT_FOUND",
                            "Region not found in business."
                        );
                    }

                    if (regionDataResult.DisabledAt != null)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:REGION_DISABLED",
                            "Region is disabled."
                        );
                    }

                    if (regionDataResult.Servers.Count == 0)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:REGION_NO_SERVERS",
                            "Region has no servers."
                        );
                    }

                    if (!regionDataResult.Servers.Any(s => (s.DisabledAt == null && s.Type == ServerTypeEnum.Backend)))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:REGION_NO_AVAILABLE_SERVERS",
                            "Region has no available servers."
                        );
                    }

                    newWebSessionData.RegionId = modelData.RegionId;
                }

                // ClientIdentifier String
                if (string.IsNullOrWhiteSpace(modelData.ClientIdentifier))
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:CONFIG_CLIENT_IDENTIFIER_NOT_FOUND",
                        "Client Identifier not found in config data."
                    );
                }
                else
                {
                    if (modelData.ClientIdentifier.Length > 256)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_CLIENT_IDENTIFIER_TOO_LONG",
                            "Client Identifier is too long. Max length: 256."
                        );
                    }

                    if (modelData.ClientIdentifier.Contains("/") || modelData.ClientIdentifier.Contains("\\"))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_CLIENT_IDENTIFIER_INVALID",
                            "Client Identifier contains invalid characters. '/' and '\\' are not allowed."
                        );
                    }

                    newWebSessionData.ClientIdentifier = modelData.ClientIdentifier;
                }

                // Audio Input Configuration
                if (modelData.AudioInputConfiguration == null)
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:CONFIG_AUDIO_INPUT_CONFIGURATION_NOT_FOUND",
                        "Audio Input Configuration not found in config data."
                    );
                }
                else
                {
                    if (modelData.AudioInputConfiguration.SampleRate < 8000 || modelData.AudioInputConfiguration.SampleRate > 96000) {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_INPUT_CONFIGURATION_SAMPLE_RATE_INVALID",
                            "Audio Configuration sample rate is invalid. Allowed values: 8000~96000."
                        );
                    }
                    newWebSessionData.AudioInputConfiguration.SampleRate = modelData.AudioInputConfiguration.SampleRate;

                    if (modelData.AudioInputConfiguration.BitsPerSample != 8 && modelData.AudioInputConfiguration.BitsPerSample != 16 && modelData.AudioInputConfiguration.BitsPerSample != 24 && modelData.AudioInputConfiguration.BitsPerSample != 32) {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_INPUT_CONFIGURATION_BITS_PER_SAMPLE_INVALID",
                            "Audio Input Configuration bits per sample is invalid. Allowed values: 8, 16, 24, 32."
                        );
                    }
                    newWebSessionData.AudioInputConfiguration.BitsPerSample = modelData.AudioInputConfiguration.BitsPerSample;

                    if (!Enum.IsDefined(typeof(AudioEncodingTypeEnum), modelData.AudioInputConfiguration.AudioEncodingType))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_INPUT_CONFIGURATION_AUDIO_ENCODING_TYPE_INVALID",
                            "Audio Input Configuration audio encoding type is invalid."
                        );
                    }
                    newWebSessionData.AudioInputConfiguration.AudioEncodingType = modelData.AudioInputConfiguration.AudioEncodingType;

                    if (!Enum.IsDefined(typeof(AudioEncoderFallbackOptimizationMode), modelData.AudioInputConfiguration.AudioEncodingFallbackMode))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_INPUT_CONFIGURATION_AUDIO_ENCODING_FALLBACK_MODE_INVALID",
                            "Audio Input Configuration audio encoding fallback mode is invalid."
                        );
                    }
                    newWebSessionData.AudioInputConfiguration.AudioEncodingFallbackMode = modelData.AudioInputConfiguration.AudioEncodingFallbackMode;
                }

                // Audio Output Configuration
                if (modelData.AudioOutputConfiguration == null)
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_NOT_FOUND",
                        "Audio Output Configuration not found in config data."
                    );
                }
                else
                {
                    if (modelData.AudioOutputConfiguration.SampleRate < 8000 || modelData.AudioOutputConfiguration.SampleRate > 96000)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_SAMPLE_RATE_INVALID",
                            "Audio Configuration sample rate is invalid. Allowed values: 8000~96000."
                        );
                    }
                    newWebSessionData.AudioOutputConfiguration.SampleRate = modelData.AudioOutputConfiguration.SampleRate;

                    if (modelData.AudioOutputConfiguration.BitsPerSample != 8 && modelData.AudioOutputConfiguration.BitsPerSample != 16 && modelData.AudioOutputConfiguration.BitsPerSample != 24 && modelData.AudioOutputConfiguration.BitsPerSample != 32)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_BITS_PER_SAMPLE_INVALID",
                            "Audio Output Configuration bits per sample is invalid. Allowed values: 8, 16, 24, 32."
                        );
                    }
                    newWebSessionData.AudioOutputConfiguration.BitsPerSample = modelData.AudioOutputConfiguration.BitsPerSample;

                    if (!Enum.IsDefined(typeof(AudioEncodingTypeEnum), modelData.AudioOutputConfiguration.AudioEncodingType))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_AUDIO_ENCODING_TYPE_INVALID",
                            "Audio Output Configuration audio encoding type is invalid."
                        );
                    }
                    newWebSessionData.AudioOutputConfiguration.AudioEncodingType = modelData.AudioOutputConfiguration.AudioEncodingType;

                    if (!Enum.IsDefined(typeof(AudioEncoderFallbackOptimizationMode), modelData.AudioOutputConfiguration.AudioEncodingFallbackMode))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_AUDIO_ENCODING_FALLBACK_MODE_INVALID",
                            "Audio Output Configuration audio encoding fallback mode is invalid."
                        );
                    }
                    newWebSessionData.AudioOutputConfiguration.AudioEncodingFallbackMode = modelData.AudioOutputConfiguration.AudioEncodingFallbackMode;

                    // int FrameDurationMs, MaxBufferAheadMs, InitialSegmentDurationMs
                    if (modelData.AudioOutputConfiguration.FrameDurationMs < 20 || modelData.AudioOutputConfiguration.FrameDurationMs > 150)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_FRAME_DURATION_MS_INVALID",
                            "Audio Output Configuration frame duration is invalid. Allowed values: 20~150."
                        );
                    }
                    newWebSessionData.AudioOutputConfiguration.FrameDurationMs = modelData.AudioOutputConfiguration.FrameDurationMs;

                    if (modelData.AudioOutputConfiguration.MaxBufferAheadMs < modelData.AudioOutputConfiguration.FrameDurationMs || modelData.AudioOutputConfiguration.MaxBufferAheadMs > 5000)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_MAX_BUFFER_AHEAD_MS_INVALID",
                            "Audio Output Configuration max buffer ahead is invalid. Allowed values: more than frame duration and less than 5000."
                        );
                    }
                    newWebSessionData.AudioOutputConfiguration.MaxBufferAheadMs = modelData.AudioOutputConfiguration.MaxBufferAheadMs;

                    if (modelData.AudioOutputConfiguration.InitialSegmentDurationMs < modelData.AudioOutputConfiguration.FrameDurationMs || modelData.AudioOutputConfiguration.InitialSegmentDurationMs > 5000)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_INITIAL_SEGMENT_DURATION_MS_INVALID",
                            "Audio Output Configuration initial segment duration is invalid. Allowed values: more than frame duration and less than 5000."
                        );
                    }
                    newWebSessionData.AudioOutputConfiguration.InitialSegmentDurationMs = modelData.AudioOutputConfiguration.InitialSegmentDurationMs;
                }

                // Dynamic Variables
                if (modelData.DynamicVariables != null && modelData.DynamicVariables.Count > 0)
                {
                    newWebSessionData.DynamicVariables = modelData.DynamicVariables;
                }
                if (webCampaignData.Variables.DynamicVariables.Count > 0)
                {
                    foreach (var variableData in webCampaignData.Variables.DynamicVariables)
                    {
                        var dynamicVariableItem = newWebSessionData.DynamicVariables.FirstOrDefault(x => x.Key == variableData.Key);

                        if (dynamicVariableItem.Key == null)
                        {
                            if (variableData.IsRequired)
                            {
                                return result.SetFailureResult(
                                    "InitiateWebSession:CONFIG_DYNAMIC_VARIABLES_REQUIRED_NOT_FOUND",
                                    $"Dynamic variable required not found in config data for {variableData.Key}. Web campaign variable rule."
                                );
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(dynamicVariableItem.Value) && !variableData.IsEmptyOrNullAllowed)
                            {
                                return result.SetFailureResult(
                                    "InitiateWebSession:CONFIG_DYNAMIC_VARIABLES_REQUIRED_NOT_FOUND",
                                    $"Dynamic variable cannot be empty in config data for {variableData.Key}. Web campaign variable rule."
                                );
                            }
                        }
                    }
                }

                // Metadata
                if (modelData.Metadata != null && modelData.Metadata.Count > 0)
                {
                    newWebSessionData.Metadata = modelData.Metadata;
                }
                if (webCampaignData.Variables.Metadata.Count > 0)
                {
                    foreach (var variableData in webCampaignData.Variables.Metadata)
                    {
                        var metadataItem = newWebSessionData.Metadata.FirstOrDefault(x => x.Key == variableData.Key);

                        if (metadataItem.Key == null)
                        {
                            if (variableData.IsRequired)
                            {
                                return result.SetFailureResult(
                                    "QueueCallInitiationRequestAsync:CONFIG_METADATA_REQUIRED_NOT_FOUND",
                                    $"Metadata required not found in config data for {variableData.Key}. Web campaign variable rule."
                                );
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(metadataItem.Value) && !variableData.IsEmptyOrNullAllowed)
                            {
                                return result.SetFailureResult(
                                    "QueueCallInitiationRequestAsync:CONFIG_METADATA_REQUIRED_NOT_FOUND",
                                    $"Metadata cannot be empty in config data for {variableData.Key}. Web campaign variable rule."
                                );
                            }
                        }
                    }
                }

                var addWebSessionResult = await _webSessionRepoistory.AddWebSessionAsync(newWebSessionData);
                if (!addWebSessionResult)
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:WEB_SESSION_ADD_DB_FAIL",
                        "Failed to add web session to database."
                    );
                }

                var checkBalanceOrMinutes = await _billingValidationManager.ValidateCallPermissionAsync(businessData.Id);
                if (!checkBalanceOrMinutes.Success)
                {
                    await _webSessionRepoistory.UpdateStatusAndAddLogAsync(
                        newWebSessionData.Id,
                        WebSessionStatusEnum.Failed,
                        new WebSessionLog {
                            Message = $"[InitiateWebSession:{checkBalanceOrMinutes.Code}] {checkBalanceOrMinutes.Message}",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );

                    return result.SetFailureResult(
                        "InitiateWebSession:" + checkBalanceOrMinutes.Code,
                        checkBalanceOrMinutes.Message
                    );
                }

                var serverSelectionResult = await _serverSelectionManager.SelectOptimalServerAsync(newWebSessionData.RegionId);
                if (!serverSelectionResult.Success || !serverSelectionResult.Data.Any())
                {
                    // todo this should happen very critically but should we kill the queue because of it?
                    await _webSessionRepoistory.UpdateStatusAndAddLogAsync(
                        newWebSessionData.Id,
                        WebSessionStatusEnum.Failed,
                        new WebSessionLog {
                            Message = $"[InitiateWebSession:{serverSelectionResult.Code}] {serverSelectionResult.Message}",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );

                    return result.SetFailureResult(
                        "InitiateWebSession:" + serverSelectionResult.Code,
                        serverSelectionResult.Message
                    );
                }

                RegionData? regionDetails = await _regionManager.GetRegionById(newWebSessionData.RegionId);
                if (regionDetails == null)
                {
                    await _webSessionRepoistory.UpdateStatusAndAddLogAsync(
                        newWebSessionData.Id,
                        WebSessionStatusEnum.Failed,
                        new WebSessionLog {
                            Message = $"[InitiateWebSession:REGION_NOT_FOUND] Region details for {newWebSessionData.RegionId} not found.",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );

                    return result.SetFailureResult(
                        "InitiateWebSession:REGION_NOT_FOUND",
                        $"Region details for {newWebSessionData.RegionId} not found."
                    );
                }

                var webSessionRequestBackendModel = new BackendInitiateWebSessionRequestModel
                {
                    WebSessionId = newWebSessionData.Id
                };

                bool successfullyForwarded = false;
                string? webSessionWebSocketUrl = null;
                foreach (var optimalServer in serverSelectionResult.Data)
                {
                    RegionServerData? backendServerDetails = regionDetails.Servers.FirstOrDefault(s => s.Endpoint == optimalServer.ServerEndpoint && s.Type == ServerTypeEnum.Backend);
                    if (backendServerDetails == null)
                    {
                        await _webSessionRepoistory.UpdateStatusAndAddLogAsync(
                            newWebSessionData.Id,
                            WebSessionStatusEnum.Failed,
                            new WebSessionLog {
                                Message = $"[InitiateWebSession:BACKEND_SERVER_NOT_FOUND] Backend server details for {optimalServer.ServerEndpoint} not found.",
                                Type = WebSessionLogTypeEnum.Error
                            }
                        );
                        continue;
                    } 

                    var forwardWebSessionRequestResponse = await ForwardInitiateWebSessionRequestToBackendAsync(backendServerDetails, webSessionRequestBackendModel);
                    if (!forwardWebSessionRequestResponse.Success)
                    {
                        await _webSessionRepoistory.UpdateStatusAndAddLogAsync(
                            newWebSessionData.Id,
                            WebSessionStatusEnum.Failed,
                            new WebSessionLog {
                                Message = $"[InitiateWebSession:BACKEND_FORWARD_FAIL] Failed to forward to backend:\n\n[{forwardWebSessionRequestResponse.Code}] {forwardWebSessionRequestResponse.Message}",
                                Type = WebSessionLogTypeEnum.Error
                            }
                        );
                        break;
                    }
                    else
                    {
                        var backendResult = forwardWebSessionRequestResponse.Data!;
                        if (!backendResult.Success)
                        {
                            successfullyForwarded = false;

                            await _webSessionRepoistory.UpdateStatusAndAddLogAsync(
                                newWebSessionData.Id,
                                WebSessionStatusEnum.Failed,
                                new WebSessionLog {
                                    Message = $"[InitiateWebSession:BACKEND_CALL_PROCESS_FAIL] Backend call processing failure:\n\n[{backendResult.Code}] {backendResult.Message}",
                                    Type = WebSessionLogTypeEnum.Error
                                }
                            );

                            return result.SetFailureResult(
                                "InitiateWebSession:BACKEND_CALL_PROCESS_FAIL",
                                $"[InitiateWebSession:BACKEND_CALL_PROCESS_FAIL] Backend call processing failure:\n\n[{backendResult.Code}] {backendResult.Message}"
                            );
                        }
                        else
                        {
                            successfullyForwarded = true;
                            webSessionWebSocketUrl = backendResult.Data.WebSocketURL;
                        }

                        break;
                    }
                }

                if (!successfullyForwarded)
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:BACKEND_CALL_PROCESS_FAIL",
                        "Failed to forward to backend server."
                    );
                }

                return result.SetSuccessResult(
                    new InitiateWebSessionResultModel()
                    {
                        SessionId = newWebSessionData.Id,
                        SessionWebSocketURL = webSessionWebSocketUrl!
                    }
                );
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "InitiateWebSession:EXCEPTION",
                    $"Error initiating web session: {ex.Message}"
                );
            }
        }

        private async Task<FunctionReturnResult<FunctionReturnResult<BackendInitiateWebSessionResultModel>>> ForwardInitiateWebSessionRequestToBackendAsync(RegionServerData backendServer, BackendInitiateWebSessionRequestModel requestDto)
        {
            var result = new FunctionReturnResult<FunctionReturnResult<BackendInitiateWebSessionResultModel>>();
            string endpoint = (backendServer.UseSSL ? "https://" : "http://") + backendServer.Endpoint;

            var baseUri = new Uri(endpoint);
            baseUri = new Uri(baseUri, $"{(baseUri.AbsolutePath != "/" ? baseUri.AbsolutePath : "")}/api/websession/initiate");

            try
            {
                using var client = _httpClientFactory.CreateClient("WebSessionForwardClient");
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
                    return result.SetFailureResult(
                        $"ForwardInitiateWebSessionRequestToBackendAsync:{response.StatusCode}",
                        $"Backend returned error: {response.StatusCode}. Details: {responseContentString}"
                    );
                }

                FunctionReturnResult<BackendInitiateWebSessionResultModel?>? backendResponse;
                try
                {
                    backendResponse = JsonSerializer.Deserialize<FunctionReturnResult<BackendInitiateWebSessionResultModel?>?>(responseContentString, _camelCaseSerializationOptions);
                }
                catch (Exception ex)
                {
                    return result.SetFailureResult(
                        "ForwardInitiateWebSessionRequestToBackendAsync:BACKEND_RESPONSE_PARSE_FAIL",
                        $"Backend failed to process or invalid response format. Exception: {ex.Message}, Response: {responseContentString}"
                    );
                }

                if (backendResponse == null || backendResponse.Data == null)
                {
                    return result.SetFailureResult(
                        "ForwardInitiateWebSessionRequestToBackendAsync:BACKEND_RESPONSE_PARSED_BUT_NULL",
                        $"Backend returned null response. Response: {responseContentString}"
                    );
                }

                return result.SetSuccessResult(backendResponse!);
            }
            catch (HttpRequestException httpEx)
            {
                return result.SetFailureResult(
                    "ForwardInitiateWebSessionRequestToBackendAsync:HttpRequestError",
                    $"HTTP request error: {httpEx.Message}"
                );
            }
            catch (TaskCanceledException tex)
            {
                return result.SetFailureResult(
                    "ForwardInitiateWebSessionRequestToBackendAsync:Timeout",
                    "Request to backend timed out."
                );
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ForwardInitiateWebSessionRequestToBackendAsync:GenericError",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}
