using IqraCore.Entities.Business;
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
using Microsoft.AspNetCore.Http;
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

        public async Task<FunctionReturnResult<InitiateWebSessionResultModel?>> InitiateWebSession(BusinessData businessData, IFormCollection formData)
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
                if (!formData.TryGetValue("config", out var configStringValue))
                {
                    return result.SetFailureResult(
                        "InitiateWebSession:FORM_DATA_CONFIG_NOT_FOUND",
                        "Config not found in form data."
                    );
                }
                else
                {
                    var configString = configStringValue.FirstOrDefault();
                    if (string.IsNullOrEmpty(configString))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:FORM_DATA_CONFIG_EMPTY",
                            "Config not found in form data."
                        );
                    }

                    JsonDocument? initiateWebSessionConfigElement = null;
                    try
                    {
                        initiateWebSessionConfigElement = JsonSerializer.Deserialize<JsonDocument>(configString);
                    }
                    catch (Exception ex)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_DESERIALIZATION_ERROR",
                            $"Invalid config data format: {ex.Message}"
                        );
                    }
                    if (initiateWebSessionConfigElement == null)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_DESERIALIZATION_ERROR",
                            "Invalid config data format."
                        );
                    }
                    var callRequestElement = initiateWebSessionConfigElement.RootElement;

                    // Web Campaign Id
                    if (!callRequestElement.TryGetProperty("webCampaignId", out var webCampaignIdElement)
                        || webCampaignIdElement.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(webCampaignIdElement.GetString()))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_WEB_CAMPAIGN_ID_NOT_FOUND",
                            "Web Campaign ID not found in config data."
                        );
                    }
                    else
                    {
                        var webCampaignIdValue = webCampaignIdElement.GetString()!;

                        // TODO change to a simple check if exists than getting full data
                        var campaignDataResult = await _parentBusinessManager.GetCampaignManager().GetWebCampaignById(businessData.Id, webCampaignIdValue);
                        if (!campaignDataResult.Success && campaignDataResult.Data != null)
                        {
                            return result.SetFailureResult(
                                "InitiateWebSession:CAMPAIGN_NOT_FOUND",
                                "Campaign not found in business."
                            );
                        }

                        webCampaignData = campaignDataResult.Data!;
                        newWebSessionData.WebCampaignId = webCampaignIdValue;
                    }

                    // Region Id
                    if (!callRequestElement.TryGetProperty("regionId", out var regionIdElement)
                        || regionIdElement.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(regionIdElement.GetString()))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_REGION_ID_NOT_FOUND",
                            "Region ID not found in config data."
                        );
                    }
                    else
                    {
                        var regionIdValue = regionIdElement.GetString()!;

                        var regionDataResult = await _regionManager.GetRegionById(regionIdValue);
                        if (regionDataResult == null)
                        {
                            return result.SetFailureResult(
                                "InitiateWebSession:REGION_NOT_FOUND",
                                "Region not found in business."
                            );
                        }

                        newWebSessionData.RegionId = regionIdValue;
                    }

                    // ClientIdentifier String
                    if (!callRequestElement.TryGetProperty("clientIdentifier", out var clientIdentifierElement)
                        || clientIdentifierElement.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(clientIdentifierElement.GetString()))
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_CLIENT_IDENTIFIER_NOT_FOUND",
                            "Client Identifier not found in config data."
                        );
                    }
                    else
                    {
                        var clientIdentifierValue = clientIdentifierElement.GetString()!;
                        newWebSessionData.ClientIdentifier = clientIdentifierValue;
                    }

                    // Dynamic Variables
                    if (!callRequestElement.TryGetProperty("dynamicVariables", out var dynamicVariablesElement)
                        || dynamicVariablesElement.ValueKind != JsonValueKind.Object)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_DYNAMIC_VARIABLES_NOT_FOUND",
                            "Dynamic variables not found in config data."
                        );
                    }
                    else
                    {
                        var dynamicVariablesEnumerator = dynamicVariablesElement.EnumerateObject();

                        foreach (var dynamicVariableItem in dynamicVariablesEnumerator)
                        {
                            if (string.IsNullOrWhiteSpace(dynamicVariableItem.Name))
                            {
                                return result.SetFailureResult(
                                    "InitiateWebSession:CONFIG_DYNAMIC_VARIABLES_KEY_INVALID",
                                    "One of the Dynamic variables key is empty."
                                );
                            }

                            if (newWebSessionData.DynamicVariables.ContainsKey(dynamicVariableItem.Name))
                            {
                                return result.SetFailureResult(
                                    "InitiateWebSession:CONFIG_DYNAMIC_VARIABLES_KEY_DUPLICATE",
                                    $"Duplicate Dynamic variable key found '{dynamicVariableItem.Name}'."
                                );
                            }

                            if (dynamicVariableItem.Value.ValueKind != JsonValueKind.String) {
                                return result.SetFailureResult(
                                    "InitiateWebSession:CONFIG_DYNAMIC_VARIABLES_INVALID",
                                    $"Dynamic variable key '{dynamicVariableItem.Name}' with value is not a string but {dynamicVariableItem.Value.ValueKind}."
                                );
                            }

                            newWebSessionData.DynamicVariables.Add(dynamicVariableItem.Name, dynamicVariableItem.Value.GetString()!);
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
                    }

                    // Metadata
                    if (!callRequestElement.TryGetProperty("metadata", out var metadataElement)
                        || metadataElement.ValueKind != JsonValueKind.Object)
                    {
                        return result.SetFailureResult(
                            "InitiateWebSession:CONFIG_METADATA_NOT_FOUND",
                            "Metadata not found in config data."
                        );
                    }
                    else
                    {
                        var metadataEnumerator = metadataElement.EnumerateObject();

                        foreach (var metadataItem in metadataEnumerator) {
                            if (string.IsNullOrWhiteSpace(metadataItem.Name))
                            {
                                return result.SetFailureResult(
                                    "InitiateWebSession:CONFIG_METADATA_KEY_INVALID",
                                    "One of the Metadata key is empty."
                                );
                            }

                            if (newWebSessionData.Metadata.ContainsKey(metadataItem.Name))
                            {
                                return result.SetFailureResult(
                                    "InitiateWebSession:CONFIG_METADATA_KEY_DUPLICATE",
                                    $"Duplicate Metadata key found '{metadataItem.Name}'."
                                );
                            }

                            if (metadataItem.Value.ValueKind != JsonValueKind.String) {
                                return result.SetFailureResult(
                                    "InitiateWebSession:CONFIG_METADATA_INVALID",
                                    $"Metadata key '{metadataItem.Name}' with value is not a string but {metadataItem.Value.ValueKind}."
                                );
                            }

                            newWebSessionData.Metadata.Add(metadataItem.Name, metadataItem.Value.GetString()!);
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
