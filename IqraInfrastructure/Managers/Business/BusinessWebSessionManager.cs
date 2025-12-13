using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.WebSession;
using IqraCore.Entities.WebSession.Enum;
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

        public async Task<FunctionReturnResult<InitiateWebSessionResultModel?>> InitiateWebSession(BusinessData businessData, InitiateWebSessionRequestModel modelData, bool isUserAdmin)
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

                // Transport Type
                if (!Enum.IsDefined(typeof(WebSessionTransportTypeEnum), modelData.TransportType))
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:CONFIG_TRANSPORT_TYPE_INVALID",
                        "Transport type not found or invalid in config data."
                    );
                }
                newWebSessionData.TransportType = modelData.TransportType;

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
                    // Validation is done later
                    newWebSessionData.AudioInputConfiguration.SampleRate = modelData.AudioInputConfiguration.SampleRate;
                    newWebSessionData.AudioInputConfiguration.BitsPerSample = modelData.AudioInputConfiguration.BitsPerSample;

                    if (!Enum.IsDefined(typeof(AudioEncodingTypeEnum), modelData.AudioInputConfiguration.AudioEncodingType))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_INPUT_CONFIGURATION_AUDIO_ENCODING_TYPE_INVALID",
                            "Audio Input Configuration audio encoding type is invalid."
                        );
                    }
                    newWebSessionData.AudioInputConfiguration.AudioEncodingType = modelData.AudioInputConfiguration.AudioEncodingType;
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
                    // Validation is done later
                    newWebSessionData.AudioOutputConfiguration.SampleRate = modelData.AudioOutputConfiguration.SampleRate;
                    newWebSessionData.AudioOutputConfiguration.BitsPerSample = modelData.AudioOutputConfiguration.BitsPerSample;

                    if (!Enum.IsDefined(typeof(AudioEncodingTypeEnum), modelData.AudioOutputConfiguration.AudioEncodingType))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_AUDIO_ENCODING_TYPE_INVALID",
                            "Audio Output Configuration audio encoding type is invalid."
                        );
                    }
                    newWebSessionData.AudioOutputConfiguration.AudioEncodingType = modelData.AudioOutputConfiguration.AudioEncodingType;

                    // int FrameDurationMs - always required?
                    if (modelData.AudioOutputConfiguration.FrameDurationMs < 20 || modelData.AudioOutputConfiguration.FrameDurationMs > 150)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_AUDIO_OUTPUT_CONFIGURATION_FRAME_DURATION_MS_INVALID",
                            "Audio Output Configuration frame duration is invalid. Allowed values: 20~150."
                        );
                    }
                    newWebSessionData.AudioOutputConfiguration.FrameDurationMs = modelData.AudioOutputConfiguration.FrameDurationMs;
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

                // Perform General Audio Format and Transport Validations
                var validationResult = ValidateSessionAudioConfiguration(modelData);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(validationResult.Code, validationResult.Message);
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

                var serverSelectionResult = await _serverSelectionManager.SelectOptimalServerAsync(newWebSessionData.RegionId, isUserAdmin);
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
                    RegionServerData? backendServerDetails = regionDetails.Servers.FirstOrDefault(s => s.Id == optimalServer.ServerId && s.Type == ServerTypeEnum.Backend);
                    if (backendServerDetails == null)
                    {
                        await _webSessionRepoistory.UpdateStatusAndAddLogAsync(
                            newWebSessionData.Id,
                            WebSessionStatusEnum.Failed,
                            new WebSessionLog {
                                Message = $"[InitiateWebSession:BACKEND_SERVER_NOT_FOUND] Backend server details for {optimalServer.ServerId} not found.",
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

        private FunctionReturnResult ValidateSessionAudioConfiguration(InitiateWebSessionRequestModel model)
        {
            var result = new FunctionReturnResult();

            var input = model.AudioInputConfiguration;
            var output = model.AudioOutputConfiguration;
            var transport = model.TransportType;

            // PCM must be 8khz ~ 48khz and 8, 16, 32 bits per sample
            if (
                (input.AudioEncodingType == AudioEncodingTypeEnum.PCM || input.AudioEncodingType == AudioEncodingTypeEnum.WAV) &&
                (
                    (input.BitsPerSample != 8 && input.BitsPerSample != 16 && input.BitsPerSample != 32) ||
                    (input.SampleRate < 8000 || input.SampleRate > 48000)
                )
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Input: PCM/WAV must have 8, 16, or 32 bits per sample and a sample rate between 8000 and 48000."
                );
            }

            if (
                (output.AudioEncodingType == AudioEncodingTypeEnum.PCM || output.AudioEncodingType == AudioEncodingTypeEnum.WAV) &&
                (
                    (output.BitsPerSample != 8 && output.BitsPerSample != 16 && output.BitsPerSample != 32) ||
                    (output.SampleRate < 8000 || output.SampleRate > 48000)
                )
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Output: PCM/WAV must have 8, 16, or 32 bits per sample and a sample rate between 8000 and 48000."
                );
            }

            // Opus must be 8khz ~ 48khz and 16 bits per sample
            if (
                (input.AudioEncodingType == AudioEncodingTypeEnum.OPUS) &&
                (input.SampleRate < 8000 || input.SampleRate > 48000) &&
                input.BitsPerSample != 16
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Input: Opus must have a sample rate between 8000 and 48000 and 16 bits per sample."
                );
            }

            if (
                (output.AudioEncodingType == AudioEncodingTypeEnum.OPUS) &&
                (output.SampleRate < 8000 || output.SampleRate > 48000) &&
                output.BitsPerSample != 16
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Output: Opus must have a sample rate between 8000 and 48000 and 16 bits per sample."
                );
            }


            // G.711 (MuLaw/ALaw) must be 8000Hz and 8 bits per sample
            if (
                (input.AudioEncodingType == AudioEncodingTypeEnum.MULAW || input.AudioEncodingType == AudioEncodingTypeEnum.ALAW) &&
                input.SampleRate != 8000 &&
                input.BitsPerSample != 8
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Input: G.711 (MuLaw/ALaw) requires 8000Hz sample rate and 8 bits per sample."
                );
            }

            if (
                (output.AudioEncodingType == AudioEncodingTypeEnum.MULAW || output.AudioEncodingType == AudioEncodingTypeEnum.ALAW) &&
                output.SampleRate != 8000 &&
                output.BitsPerSample != 8
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Output: G.711 (MuLaw/ALaw) requires 8000Hz sample rate and 8 bits per sample."
                );
            }

            // G.722 must be 16000Hz and 14 bits per sample
            if (
                input.AudioEncodingType == AudioEncodingTypeEnum.G722 &&
                input.SampleRate != 16000 &&
                input.BitsPerSample != 14
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Input: G.722 requires 16000Hz sample rate and 14 bits per sample."
                );
            }

            if (
                output.AudioEncodingType == AudioEncodingTypeEnum.G722 &&
                output.SampleRate != 16000 &&
                output.BitsPerSample != 14
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Output: G.722 requires 16000Hz sample rate."
                );
            }

            // G.729 must be 8000Hz and 8 bits per sample
            if (
                input.AudioEncodingType == AudioEncodingTypeEnum.G729 &&
                input.SampleRate != 8000 &&
                input.BitsPerSample != 8
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Input: G.729 requires 8000Hz sample rate and 8 bits per sample."
                );
            }

            if (
                output.AudioEncodingType == AudioEncodingTypeEnum.G729 &&
                output.SampleRate != 8000 &&
                output.BitsPerSample != 8
            ) {
                return result.SetFailureResult(
                    "VALIDATION:INVALID_SAMPLE_RATE",
                    "Output: G.729 requires 8000Hz sample rate and 8 bits per sample."
                );
            }

            // WebRTC Specific Rules
            if (transport == WebSessionTransportTypeEnum.WebRTC)
            {
                var allowedWebRtcEncodings = new[] { AudioEncodingTypeEnum.OPUS, AudioEncodingTypeEnum.MULAW, AudioEncodingTypeEnum.ALAW, AudioEncodingTypeEnum.G722 };

                if (!allowedWebRtcEncodings.Contains(input.AudioEncodingType))
                {
                    return result.SetFailureResult(
                        "VALIDATION:WEBRTC_UNSUPPORTED_FORMAT",
                        $"Input format {input.AudioEncodingType} is not supported over WebRTC. Use OPUS, MULAW, ALAW, or G722."
                    );
                }

                if (!allowedWebRtcEncodings.Contains(output.AudioEncodingType))
                {
                    return result.SetFailureResult(
                        "VALIDATION:WEBRTC_UNSUPPORTED_FORMAT",
                        $"Output format {output.AudioEncodingType} is not supported over WebRTC. Use OPUS, MULAW, ALAW, or G722."
                    );
                }

                if (input.AudioEncodingType != output.AudioEncodingType)
                {
                    return result.SetFailureResult(
                        "VALIDATION:WEBRTC_ASYMMETRY",
                        $"WebRTC requires the Input Codec and Output Codec to be the same (e.g., both OPUS). Current: Input={input.AudioEncodingType}, Output={output.AudioEncodingType}."
                    );
                }
            }

            return result.SetSuccessResult();
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
