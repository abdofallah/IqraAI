using IqraCore.Entities.Business;
using IqraCore.Entities.Helper;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using Jint;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Script
{
    public class ScriptExecutionManager
    {
        private readonly ILogger<ScriptExecutionManager> _logger;

        private BusinessApp _businessApp;
        private BusinessAppRoute _currentSessionRoute;

        private BusinessAppAgent? _currentRouteAgent;
        private BusinessAppAgentScript? _curentAgentActiveScript;

        private string _currentSessionlanguageCode;

        private bool _isScriptInitialized;

        public bool IsScriptActive => _isScriptInitialized && _curentAgentActiveScript != null;

        public ScriptExecutionManager(
            ILogger<ScriptExecutionManager> logger
        )
        {
            _logger = logger;
        }

        public async Task LoadScriptAsync(BusinessApp businessApp, BusinessAppRoute currentSessionRoute, string languageCode)
        {
            _businessApp = businessApp;
            _currentSessionRoute = currentSessionRoute;

            _currentSessionlanguageCode = languageCode;

            _isScriptInitialized = false;

            var currentRouteAgentId = _currentSessionRoute.Agent.SelectedAgentId;
            var routeAgentScriptId = _currentSessionRoute.Agent.OpeningScriptId;

            try
            {
                _currentRouteAgent = _businessApp.Agents.FirstOrDefault(a => a.Id == currentRouteAgentId);
                if (_currentRouteAgent == null)
                {
                    throw new InvalidOperationException($"Agent not found with ID {currentRouteAgentId} in business {businessApp.Id}");
                }

                _curentAgentActiveScript = _currentRouteAgent.Scripts.FirstOrDefault(s => s.Id == routeAgentScriptId);
                if (_curentAgentActiveScript == null)
                {
                    throw new InvalidOperationException($"Script not found with ID {routeAgentScriptId} for agent {currentRouteAgentId} in business {businessApp.Id}");
                }

                _isScriptInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading route {RouteId} agent {AgentId} script {ScriptId} for business {BusinessId}", _currentSessionRoute.Id, currentRouteAgentId, routeAgentScriptId, _businessApp.Id);
                throw;
            }
        }

        // Handle Custom Tool Nodes Execution
        public async Task<FunctionReturnResult<string?>> ExecuteCustomToolAsync(string nodeId, Dictionary<string, JsonElement> parameters, CancellationToken cancellationToken)
        {
            var result = new FunctionReturnResult<string?>();

            try
            {
                BusinessAppAgentScriptCustomToolNode? nodeData = (BusinessAppAgentScriptCustomToolNode?)_curentAgentActiveScript!.Nodes.Find(n => n.Id == nodeId && n.NodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteCustomTool);
                if (nodeData == null)
                {
                    result.Code = "ExecuteToolAsync:1";
                    result.Message = $"Custom tool node with id {nodeId} not found in script, did you maybe use tool id instead of node id?";
                    return result;
                }

                BusinessAppTool? toolData = _businessApp.Tools.Find(t => t.Id == nodeData.ToolId);
                if (toolData == null)
                {
                    result.Code = "ExecuteToolAsync:2";
                    result.Message = $"Tool with id {nodeData.ToolId} not found in business for node {nodeId}";
                    return result;
                }

                var convertedParametersResult = BuildAndValidateCustomToolVariables(toolData, parameters);
                if (!convertedParametersResult.Success)
                {
                    result.Code = "ExecuteToolAsync:3";
                    result.Message = $"Error building and validating custom tool variables for node {nodeId}:\n\n```{convertedParametersResult.Message}```";
                    return result;
                }
                var baseUri = ResolveEndpointQueryStrings(toolData.Configuration.Endpoint, convertedParametersResult.Data);

                using (var toolHttpClient = new HttpClient())
                {
                    HttpResponseMessage httpResponseMessage;
                    HttpContent? requestContent = null;

                    if (toolData.Configuration.RequestType != HttpMethodEnum.Get && toolData.Configuration.RequestType != HttpMethodEnum.Delete)
                    {
                        if (toolData.Configuration.BodyType == HttpBodyEnum.FormData)
                        {
                            requestContent = new MultipartFormDataContent();

                            if (toolData.Configuration.BodyData != null && toolData.Configuration.BodyData is Dictionary<string, string>)
                            {
                                var formBodyFields = toolData.Configuration.BodyData as Dictionary<string, string>;

                                foreach (var field in formBodyFields)
                                {
                                    HttpContent fieldHttpContentData = new StringContent(ResolveContextVariables(field.Value, convertedParametersResult.Data));
                                    ((MultipartFormDataContent)requestContent).Add(fieldHttpContentData, field.Key);
                                }
                            }
                        }
                        else if (toolData.Configuration.BodyType == HttpBodyEnum.XWWWFormUrlencoded)
                        {
                            if (toolData.Configuration.BodyData != null && toolData.Configuration.BodyData is Dictionary<string, string>)
                            {
                                var formBodyFields = toolData.Configuration.BodyData as Dictionary<string, string>;

                                foreach (var field in formBodyFields)
                                {
                                    formBodyFields[field.Key] = ResolveContextVariables(field.Value, convertedParametersResult.Data);
                                }

                                requestContent = new FormUrlEncodedContent(formBodyFields);
                            }
                            else
                            {
                                requestContent = new FormUrlEncodedContent(new Dictionary<string, string>());
                            }
                        }
                        else if (toolData.Configuration.BodyType == HttpBodyEnum.Raw)
                        {
                            if (toolData.Configuration.BodyData != null && toolData.Configuration.BodyData is string)
                            {
                                requestContent = new StringContent(ResolveContextVariables(toolData.Configuration.BodyData.ToString(), convertedParametersResult.Data));
                                requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                            }
                            else
                            {
                                requestContent = new StringContent("");
                                requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                            }
                        }
                    }    

                    foreach (var header in toolData.Configuration.Headers)
                    {
                        toolHttpClient.DefaultRequestHeaders.Add(header.Key, ResolveContextVariables(header.Value, convertedParametersResult.Data));
                    }

                    switch (toolData.Configuration.RequestType)
                    {
                        case HttpMethodEnum.Get:
                            httpResponseMessage = await toolHttpClient.GetAsync(baseUri, cancellationToken);
                            break;

                        case HttpMethodEnum.Post:
                            httpResponseMessage = await toolHttpClient.PostAsync(baseUri, requestContent, cancellationToken);
                            break;

                        case HttpMethodEnum.Put:
                            httpResponseMessage = await toolHttpClient.PutAsync(baseUri, requestContent, cancellationToken);
                            break;

                        case HttpMethodEnum.Patch:
                            httpResponseMessage = await toolHttpClient.PatchAsync(baseUri, requestContent, cancellationToken);
                            break;

                        case HttpMethodEnum.Delete:
                            httpResponseMessage = await toolHttpClient.DeleteAsync(baseUri, cancellationToken);
                            break;

                        default:
                            result.Code = "ExecuteToolAsync:3";
                            result.Message = $"Unsupported http method {toolData.Configuration.RequestType} in tool {toolData.Id} for node {nodeId}";
                            return result;
                    }

                    var responseStatusCode = httpResponseMessage.StatusCode;
                    var responseData = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

                    if (!toolData.Response.TryGetValue(httpResponseMessage.StatusCode.ToString(), out BusinessAppToolResponse? responseToolConfiguration) || responseToolConfiguration == null)
                    {
                        result.Success = true;
                        result.Data = $"The custom tool associated with node {nodeId} returned status code {responseStatusCode} and response data:\n\n```{responseData}```\n\nWhile the business never defined how to handle this type of response.";
                        return result;
                    }

                    var javascriptExecutedResult = ExecuteJavaScriptProcessor(responseData, responseToolConfiguration.Javascript);
                    if (!javascriptExecutedResult.Success)
                    {
                        result.Code = "ExecuteToolAsync:" + javascriptExecutedResult.Code;
                        result.Message = $"The custom tool associated with node {nodeId} returned status code {responseStatusCode} but failed to execute the tool javascript code."; // todo should provide the responseData in case of failure or no? ask user
                        return result;
                    }

                    if (responseToolConfiguration.HasStaticResponse && responseToolConfiguration.StaticResponse.TryGetValue(_currentSessionlanguageCode, out string? staticResponse) && !string.IsNullOrEmpty(staticResponse))
                    {
                        result.Success = true;
                        result.Data = $"The custom tool associated with node {nodeId} returned status code {responseStatusCode} and response data:\n\n```{javascriptExecutedResult.Data}```\n\nThe business provided the following static response you must convery the result in:\n\n```{staticResponse}```";
                        return result;
                    }

                    result.Success = true;
                    result.Data = $"The custom tool associated with node {nodeId} returned status code {responseStatusCode} and response data:\n\n```{javascriptExecutedResult.Data}```";
                    return result;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", nodeId);
                result.Code = "ExecuteToolAsync:-1";
                result.Message = "Error: " + ex.Message;
                return result;
            }
        }
        private FunctionReturnResult<string?> ExecuteJavaScriptProcessor(string responseData, string javascriptCode)
        {
            var result = new FunctionReturnResult<string?>();

            // Create engine with comprehensive security restrictions
            var engine = new Engine(options => {
                options.TimeoutInterval(TimeSpan.FromSeconds(5));
                options.LimitMemory(5_000_000);
                options.LimitRecursion(64);
                options.MaxStatements(1000);
                options.AllowClrWrite(false);
                options.AllowOperatorOverloading(false);
                options.Strict();
                options.MaxArraySize(10000);
                options.MaxJsonParseDepth(100);
                options.RegexTimeoutInterval(TimeSpan.FromSeconds(1));
                options.DisableStringCompilation();
            });

            try
            {
                engine.SetValue("responseData", responseData);
                var executionResult = engine.Evaluate(javascriptCode);
                var executionObject = executionResult.ToObject();

                if (executionObject == null)
                {
                    result.Data = ""; // what to do about this??
                }
                else if (executionObject.GetType() != typeof(string))
                {
                    result.Data = JsonSerializer.Serialize(executionObject);
                }
                else
                {
                    result.Data = executionObject.ToString();
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Code = "ExecuteJavaScriptProcessor:-1";
                result.Message = "Error: " + ex.Message;
                return result;
            }
        }
        private string ResolveEndpointQueryStrings(string endpoint, Dictionary<string, object> parameters)
        {
            // First resolve any variables in the endpoint string
            string fullUri = ResolveContextVariables(endpoint, parameters);

            // Parse the URL manually to avoid automatic + to space conversion
            int queryStartIndex = fullUri.IndexOf('?');
            if (queryStartIndex == -1)
            {
                // No query parameters, return as is
                return fullUri;
            }

            string baseUrlPart = fullUri.Substring(0, queryStartIndex);
            string queryPart = fullUri.Substring(queryStartIndex + 1);

            // Split the query string into key-value pairs
            var queryPairs = queryPart.Split('&');
            var processedPairs = new List<string>();

            foreach (var pair in queryPairs)
            {
                int equalsIndex = pair.IndexOf('=');
                if (equalsIndex > 0)
                {
                    string key = pair.Substring(0, equalsIndex);
                    string value = pair.Substring(equalsIndex + 1);

                    // Decode the value first (in case it's already URL encoded)
                    string decodedValue = Uri.UnescapeDataString(value);

                    // Now encode it properly, handling + signs specially
                    // First replace + with a placeholder that won't be affected by encoding
                    string processedValue = decodedValue.Replace("+", "___PLUS___");

                    // Encode the value
                    string encodedValue = Uri.EscapeDataString(processedValue);

                    // Replace the placeholder with %2B
                    encodedValue = encodedValue.Replace("___PLUS___", "%2B");

                    // Add to our processed pairs
                    processedPairs.Add($"{Uri.EscapeDataString(key)}={encodedValue}");
                }
                else
                {
                    // Handle cases where there might be no value
                    processedPairs.Add(Uri.EscapeDataString(pair));
                }
            }

            // Build the final URI
            return $"{baseUrlPart}?{string.Join("&", processedPairs)}";
        }
        private FunctionReturnResult<Dictionary<string, object>> BuildAndValidateCustomToolVariables(BusinessAppTool toolData, Dictionary<string, JsonElement> parameters)
        {
            var result = new FunctionReturnResult<Dictionary<string, object>>();
            result.Data = new Dictionary<string, object>();

            var errors = new List<string>();

            foreach (var toolInputProperty in toolData.Configuration.InputSchemea)
            {
                bool foundValue = parameters.TryGetValue(toolInputProperty.Id, out JsonElement parameterValue);

                if (toolInputProperty.IsRequired)
                {
                    if (foundValue == false)
                    {
                        errors.Add($"Parameter with id {toolInputProperty.Id} is required but no value found.");
                        continue;
                    }
                    
                    if (parameterValue.ValueKind == JsonValueKind.Null || parameterValue.ValueKind == JsonValueKind.Undefined)
                    {
                        // todo in future allow/ask whether null is allowed
                        errors.Add($"Parameter with id {toolInputProperty.Id} cannot be null/undefined.");
                        continue;
                    }
                }

                if (foundValue == false)
                {
                    // todo if default value is defined or if keep empty or null in case of no result is defined, do it here
                    continue;
                }

                if (toolInputProperty.IsArray)
                {
                    if (parameterValue.ValueKind != JsonValueKind.Array)
                    {
                        errors.Add($"Parameter with id {toolInputProperty.Id} must be an array but found {parameterValue.ValueKind}.");
                        continue;
                    }

                    List<object> paramsValuesList = new List<object>();
                    foreach (var item in parameterValue.EnumerateArray())
                    {
                        var itemResult = ParseJsonElementAsType(toolInputProperty, item);
                        if (!itemResult.Success)
                        {
                            errors.Add(itemResult.Message);
                            continue;
                        }

                        paramsValuesList.Add(itemResult.Data);
                    }

                    result.Data.Add(toolInputProperty.Id, paramsValuesList);
                }
                else
                {
                    var itemResult = ParseJsonElementAsType(toolInputProperty, parameterValue);
                    if (!itemResult.Success)
                    {
                        errors.Add(itemResult.Message);
                        continue;
                    }

                    result.Data.Add(toolInputProperty.Id, itemResult.Data);
                }
            }

            result.Success = true;
            return result;
        }
        private FunctionReturnResult<object?> ParseJsonElementAsType(BusinessAppToolConfigurationInputSchemea toolInputProperty, JsonElement parameterValue)
        {
            var result = new FunctionReturnResult<object?>();

            if (toolInputProperty.Type == BusinessAppToolConfigurationInputSchemeaTypeEnum.String)
            {
                if (parameterValue.ValueKind == JsonValueKind.String)
                {
                    result.Success = true;
                    result.Data = parameterValue.GetString();
                    return result;
                }

                result.Message = $"Parameter with id {toolInputProperty.Id} must be a string but found {parameterValue.ValueKind}.";
                return result;
            }
            
            if (toolInputProperty.Type == BusinessAppToolConfigurationInputSchemeaTypeEnum.Number)
            {
                if (parameterValue.ValueKind == JsonValueKind.Number)
                {
                    // todo for now we get double but we need better handling to know exactly if int is needed or double

                    double doubleValue = parameterValue.GetDouble();
                    bool isValueInt = Math.Abs(doubleValue - (int)doubleValue) < double.Epsilon;

                    result.Success = true;
                    result.Data = isValueInt ? (int)doubleValue : doubleValue;
                    return result;
                }
                else if (parameterValue.ValueKind == JsonValueKind.String)
                {
                    // try parse string to double
                    if (!double.TryParse(parameterValue.GetString(), out double doubleValue))
                    {
                        result.Message = $"Parameter with id {toolInputProperty.Id} must be a number but found {parameterValue.GetString()}.";
                        return result;
                    }

                    bool isValueInt = Math.Abs(doubleValue - (int)doubleValue) < double.Epsilon;

                    result.Success = true;
                    result.Data = isValueInt ? (int)doubleValue : doubleValue;
                    return result;
                }

                result.Message = $"Parameter with id {toolInputProperty.Id} must be a number but found {parameterValue.ValueKind}.";
                return result;
            }
            
            if (toolInputProperty.Type == BusinessAppToolConfigurationInputSchemeaTypeEnum.Boolean)
            {
                if (parameterValue.ValueKind == JsonValueKind.True || parameterValue.ValueKind == JsonValueKind.False)
                {
                    result.Success = true;
                    result.Data = parameterValue.GetBoolean();
                    return result;
                }

                result.Message = $"Parameter with id {toolInputProperty.Id} must be a boolean but found {parameterValue.ValueKind}.";
                return result;
            }
            
            if (toolInputProperty.Type == BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime)
            {
                if (parameterValue.ValueKind == JsonValueKind.String)
                {
                    string stringDateTime = parameterValue.GetString();
                    if (DateTime.TryParse(stringDateTime, out _))
                    {
                        result.Success = true;
                        result.Data = stringDateTime;
                        return result;
                    }

                    result.Message = $"Parameter with id {toolInputProperty.Id} is datetime and found string but unable to parse {parameterValue.GetString()} as datetime.";
                    return result;
                }

                result.Message = $"Parameter with id {toolInputProperty.Id} must be a datetime string but found {parameterValue.ValueKind}.";
                return result;
            }

            result.Message = $"Parameter with id {toolInputProperty.Id} Unsupported input type: {toolInputProperty.Type}";
            return result;
        }

        public async Task ProcessDTMFInputAsync(string nodeId, string digits)
        {
            if (!IsScriptActive)
            {
                _logger.LogWarning("Cannot process DTMF - no active script");
                return;
            }

            var currentNode = _curentAgentActiveScript.Nodes.Find(n => n.Id == nodeId);

            try
            {
                // Find the current node
                if (currentNode?.NodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool)
                {
                    var systemTool = currentNode as BusinessAppAgentScriptSystemToolNode;
                    if (systemTool?.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.GetDTMFKeypadInput)
                    {
                        var dtmfNode = systemTool as BusinessAppAgentScriptDTMFInputToolNode;
                        if (dtmfNode != null)
                        {
                            // Store DTMF input in context if variable name is provided
                            if (!string.IsNullOrEmpty(dtmfNode.VariableName))
                            {
                                //_scriptContext[dtmfNode.VariableName] = digits;
                                // TODO NOT YET IMPLEMENTED
                            }

                            // Find matching outcome
                            var outcome = dtmfNode.Outcomes.FirstOrDefault(o => o.Value.ContainsValue(digits));
                            if (outcome != null && !string.IsNullOrEmpty(outcome.PortId))
                            {
                                // Find the next node
                                var edge = _curentAgentActiveScript!.Edges.FirstOrDefault(e => 
                                    e.SourceNodeId == currentNode!.Id && 
                                    e.SourceNodePortId == outcome.PortId);

                                if (edge != null)
                                {
                                    var nextNode = _curentAgentActiveScript.Nodes.FirstOrDefault(n => n.Id == edge.TargetNodeId);
                                    if (nextNode != null)
                                    {
                                        currentNode = nextNode;
                                        //await ExecuteNodeAsync(nextNode);
                                        return;
                                    }
                                }
                            }

                            // No matching outcome found - default behavior
                            _logger.LogWarning("No matching outcome found for DTMF input: {Digits}", digits);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DTMF input: {Digits}", digits);
            }
        }

        private string ResolveContextVariables(string text, Dictionary<string, object> context)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Replace {{variableName}} with value from context
            foreach (var key in context.Keys)
            {
                text = text.Replace($"{{{{{key}}}}}", context[key]?.ToString() ?? "");
            }

            return text;
        }
    }
}