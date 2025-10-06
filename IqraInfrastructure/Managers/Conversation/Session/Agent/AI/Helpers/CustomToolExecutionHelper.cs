using IqraCore.Entities.Business;
using IqraCore.Entities.Helper;
using IqraCore.Entities.Helpers;
using Jint;
using Microsoft.Extensions.Logging;
using Scriban.Functions;
using Scriban.Runtime;
using Scriban;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers
{
    public class CustomToolExecutionHelper
    {
        private readonly ILogger<CustomToolExecutionHelper> _logger;
        private BusinessApp? _businessApp;
        private string? _currentLanguageCode;

        public CustomToolExecutionHelper(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CustomToolExecutionHelper>();
        }

        public void Initialize(BusinessApp businessApp, string languageCode)
        {
            _businessApp = businessApp;
            _currentLanguageCode = languageCode;
        }

        public async Task<FunctionReturnResult<string?>> ExecuteHttpRequestForToolAsync(
            BusinessAppTool toolData,
            Dictionary<string, JsonElement> parameters,
            CancellationToken cancellationToken
        )
        {
            var result = new FunctionReturnResult<string?>();
            if (_businessApp == null || string.IsNullOrEmpty(_currentLanguageCode))
            {
                result.Code = "ExecuteHttpRequest:0";
                result.Message = "Helper not initialized with BusinessApp and LanguageCode.";
                return result;
            }
            if (toolData == null)
            {
                result.Code = "ExecuteHttpRequest:1";
                result.Message = "Tool data provided was null.";
                return result;
            }

            try
            {
                var convertedParametersResult = BuildAndValidateCustomToolVariables(toolData, parameters);
                if (!convertedParametersResult.Success)
                {
                    result.Code = "ExecuteHttpRequest:2";
                    result.Message = $"Error building and validating custom tool variables for tool {toolData.Id}:\n\n```{convertedParametersResult.Message}```";
                    return result;
                }
                var baseUriResult = await ResolveEndpointQueryStrings(toolData.Configuration.Endpoint, convertedParametersResult.Data);
                if (!baseUriResult.Success)
                {
                    result.Code = "ExecuteHttpRequest:3";
                    result.Message = $"Error resolving endpoint query strings for tool {toolData.Id}:\n\n```{baseUriResult.Message}```";
                    return result;
                }
                var baseUri = baseUriResult.Data;

                using (var toolHttpClient = new HttpClient())
                {
                    toolHttpClient.Timeout = TimeSpan.FromSeconds(5); // todo make configurable by user

                    HttpResponseMessage httpResponseMessage;
                    HttpContent? requestContent = null;

                    // Build Request Body
                    if (toolData.Configuration.RequestType != HttpMethodEnum.Get && toolData.Configuration.RequestType != HttpMethodEnum.Delete)
                    {
                        var requestContentResult = await BuildRequestBody(toolData.Id, toolData.Configuration, convertedParametersResult.Data);
                        if (!requestContentResult.Success)
                        {
                            result.Code = "ExecuteHttpRequest:4";
                            result.Message = $"Error building request body for tool {toolData.Id}:\n\n```{requestContentResult.Message}```";
                            return result;
                        }    

                        requestContent = requestContentResult.Data;
                    }

                    // Add Headers
                    foreach (var header in toolData.Configuration.Headers)
                    {
                        var headerValueResult = await RenderScribanTemplateAsync(header.Value, convertedParametersResult.Data);
                        if (!headerValueResult.Success)
                        {
                            result.Code = "ExecuteHttpRequest:4";
                            result.Message = $"Error rendering header value for tool {toolData.Id}:\n\n```{headerValueResult.Message}```";
                            return result;
                        }
                        toolHttpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, headerValueResult.Data);
                    }

                    // Execute Request
                    switch (toolData.Configuration.RequestType)
                    {
                        case HttpMethodEnum.Get:
                            httpResponseMessage = await toolHttpClient.GetAsync(baseUri, cancellationToken);
                            break;
                        case HttpMethodEnum.Post:
                            var strindata = requestContent.ReadAsStringAsync().Result;

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
                            result.Code = "ExecuteHttpRequest:3";
                            result.Message = $"Unsupported http method {toolData.Configuration.RequestType} in tool {toolData.Id}";
                            _logger.LogError(result.Message);
                            return result;
                    }

                    var responseStatusCode = httpResponseMessage.StatusCode;
                    var responseData = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

                    // Process Response
                    if (!toolData.Response.TryGetValue(responseStatusCode.ToString(), out BusinessAppToolResponse? responseToolConfiguration) || responseToolConfiguration == null)
                    {
                        result.Success = true; // Consider it success, but provide raw data
                        result.Data = $"The custom tool {toolData.General.Name[_currentLanguageCode]} ({toolData.Id}) returned status code {responseStatusCode} with data:\n\n```{responseData}```\n\nNo specific handling was defined for this status code.";
                        return result;
                    }

                    var javascriptExecutedResult = ExecuteJavaScriptProcessor(responseData, responseToolConfiguration.Javascript);
                    if (!javascriptExecutedResult.Success)
                    {
                        result.Code = "ExecuteHttpRequest:4" + javascriptExecutedResult.Code;
                        result.Message = $"Tool {toolData.General.Name[_currentLanguageCode]} ({toolData.Id}) returned status code {responseStatusCode} but failed to execute the response JavaScript processor. Error: {javascriptExecutedResult.Message}";
                        // Optionally include raw response data in error for debugging?
                        // result.Message += $"\nRaw Response: ```{responseData}```";
                        return result;
                    }

                    // Handle Static Response Override
                    if (responseToolConfiguration.HasStaticResponse && responseToolConfiguration.StaticResponse.TryGetValue(_currentLanguageCode!, out string? staticResponse) && !string.IsNullOrEmpty(staticResponse))
                    {
                        result.Success = true;
                        result.Data = $"The custom tool {toolData.General.Name[_currentLanguageCode]} ({toolData.Id}) executed successfully (Status: {responseStatusCode}). The processed data was:\n\n```{javascriptExecutedResult.Data}```\n\nThe following static response is configured:\n\n```{staticResponse}```";
                        return result;
                    }

                    result.Success = true;
                    result.Data = $"The custom tool {toolData.General.Name[_currentLanguageCode]} ({toolData.Id}) executed successfully (Status: {responseStatusCode}). The processed data is:\n\n```{javascriptExecutedResult.Data}```";
                    return result;
                }
            }
            catch (HttpRequestException httpEx)
            {
                result.Code = "ExecuteHttpRequest:-1";
                result.Message = $"Error executing tool '{toolData.General.Name[_currentLanguageCode]}': Network or HTTP error - {httpEx.Message}";
                return result;
            }
            catch (OperationCanceledException)
            {
                result.Code = "ExecuteHttpRequest:-2";
                result.Message = $"Execution of tool '{toolData.General.Name[_currentLanguageCode]}' was cancelled.";
                return result;
            }
            catch (Exception ex)
            {
                result.Code = "ExecuteHttpRequest:-3";
                result.Message = $"Error executing tool '{toolData.General.Name[_currentLanguageCode]}': {ex.Message}";
                return result;
            }
        }

        public async Task<FunctionReturnResult<string?>> ExecuteHttpRequestForToolWithObjectDictAsync(
            BusinessAppTool toolData,
            Dictionary<string, object?> parameters,
            CancellationToken cancellationToken
        )
        {
            var result = new FunctionReturnResult<string?>();
            if (_businessApp == null || string.IsNullOrEmpty(_currentLanguageCode))
            {
                result.Code = "ExecuteHttpRequest:0";
                result.Message = "Helper not initialized with BusinessApp and LanguageCode.";
                return result;
            }
            if (toolData == null)
            {
                result.Code = "ExecuteHttpRequest:1";
                result.Message = "Tool data provided was null.";
                return result;
            }

            try
            {
                var baseUriResult = await ResolveEndpointQueryStrings(toolData.Configuration.Endpoint, parameters);
                if (!baseUriResult.Success)
                {
                    result.Code = "ExecuteHttpRequest:3";
                    result.Message = $"Error resolving endpoint query strings for tool {toolData.Id}:\n\n```{baseUriResult.Message}```";
                    return result;
                }
                var baseUri = baseUriResult.Data;

                using (var toolHttpClient = new HttpClient())
                {
                    toolHttpClient.Timeout = TimeSpan.FromSeconds(5); // todo make configurable by user

                    HttpResponseMessage httpResponseMessage;
                    HttpContent? requestContent = null;

                    // Build Request Body
                    if (toolData.Configuration.RequestType != HttpMethodEnum.Get && toolData.Configuration.RequestType != HttpMethodEnum.Delete)
                    {
                        var requestContentResult = await BuildRequestBody(toolData.Id, toolData.Configuration, parameters);
                        if (!requestContentResult.Success)
                        {
                            result.Code = "ExecuteHttpRequest:4";
                            result.Message = $"Error building request body for tool {toolData.Id}:\n\n```{requestContentResult.Message}```";
                            return result;
                        }    

                        requestContent = requestContentResult.Data;
                    }

                    // Add Headers
                    foreach (var header in toolData.Configuration.Headers)
                    {
                        var headerValueResult = await RenderScribanTemplateAsync(header.Value, parameters);
                        if (!headerValueResult.Success)
                        {
                            result.Code = "ExecuteHttpRequest:4";
                            result.Message = $"Error rendering header value for tool {toolData.Id}:\n\n```{headerValueResult.Message}```";
                            return result;
                        }
                        toolHttpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, headerValueResult.Data);
                    }

                    // Execute Request
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
                            result.Code = "ExecuteHttpRequest:3";
                            result.Message = $"Unsupported http method {toolData.Configuration.RequestType} in tool {toolData.Id}";
                            _logger.LogError(result.Message);
                            return result;
                    }

                    var responseStatusCode = httpResponseMessage.StatusCode;
                    var responseData = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

                    // Process Response
                    if (!toolData.Response.TryGetValue(responseStatusCode.ToString(), out BusinessAppToolResponse? responseToolConfiguration) || responseToolConfiguration == null)
                    {
                        result.Success = true; // Consider it success, but provide raw data
                        result.Data = $"The custom tool {toolData.General.Name[_currentLanguageCode]} ({toolData.Id}) returned status code {responseStatusCode} with data:\n\n```{responseData}```\n\nNo specific handling was defined for this status code.";
                        return result;
                    }

                    var javascriptExecutedResult = ExecuteJavaScriptProcessor(responseData, responseToolConfiguration.Javascript);
                    if (!javascriptExecutedResult.Success)
                    {
                        result.Code = "ExecuteHttpRequest:4" + javascriptExecutedResult.Code;
                        result.Message = $"Tool {toolData.General.Name[_currentLanguageCode]} ({toolData.Id}) returned status code {responseStatusCode} but failed to execute the response JavaScript processor. Error: {javascriptExecutedResult.Message}";
                        // Optionally include raw response data in error for debugging?
                        // result.Message += $"\nRaw Response: ```{responseData}```";
                        return result;
                    }

                    // Handle Static Response Override
                    if (responseToolConfiguration.HasStaticResponse && responseToolConfiguration.StaticResponse.TryGetValue(_currentLanguageCode!, out string? staticResponse) && !string.IsNullOrEmpty(staticResponse))
                    {
                        result.Success = true;
                        result.Data = $"The custom tool {toolData.General.Name[_currentLanguageCode]} ({toolData.Id}) executed successfully (Status: {responseStatusCode}). The processed data was:\n\n```{javascriptExecutedResult.Data}```\n\nThe following static response is configured:\n\n```{staticResponse}```";
                        return result;
                    }

                    result.Success = true;
                    result.Data = $"The custom tool {toolData.General.Name[_currentLanguageCode]} ({toolData.Id}) executed successfully (Status: {responseStatusCode}). The processed data is:\n\n```{javascriptExecutedResult.Data}```";
                    return result;
                }
            }
            catch (HttpRequestException httpEx)
            {
                result.Code = "ExecuteHttpRequest:-1";
                result.Message = $"Error executing tool '{toolData.General.Name[_currentLanguageCode]}': Network or HTTP error - {httpEx.Message}";
                return result;
            }
            catch (OperationCanceledException)
            {
                result.Code = "ExecuteHttpRequest:-2";
                result.Message = $"Execution of tool '{toolData.General.Name[_currentLanguageCode]}' was cancelled.";
                return result;
            }
            catch (Exception ex)
            {
                result.Code = "ExecuteHttpRequest:-3";
                result.Message = $"Error executing tool '{toolData.General.Name[_currentLanguageCode]}': {ex.Message}";
                return result;
            }
        }

        private async Task<FunctionReturnResult<HttpContent?>> BuildRequestBody(string toolId, BusinessAppToolConfiguration toolConfig, Dictionary<string, object?> context)
        {
            var result = new FunctionReturnResult<HttpContent?>();
            try
            {
                if (toolConfig.BodyType == HttpBodyEnum.FormData)
                {
                    var formData = new MultipartFormDataContent();
                    if (toolConfig.BodyData is Dictionary<string, string> formBodyFields)
                    {
                        foreach (var field in formBodyFields)
                        {
                            var fieldValueResult = await RenderScribanTemplateAsync(field.Value, context);
                            if (!fieldValueResult.Success)
                            {
                                result.Code = "FillSessionInformationInPrompt:1";
                                result.Message = "Error parsing system prompt template: " + string.Join(", ", fieldValueResult.Message);
                                return result;
                            }

                            HttpContent fieldHttpContentData = new StringContent(fieldValueResult.Data);
                            formData.Add(fieldHttpContentData, field.Key);
                        }
                    }
                    return result.SetSuccessResult(formData);
                }
                else if (toolConfig.BodyType == HttpBodyEnum.XWWWFormUrlencoded)
                {
                    var formFields = new Dictionary<string, string>();
                    if (toolConfig.BodyData is Dictionary<string, string> formBodyFields)
                    {
                        foreach (var field in formBodyFields)
                        {
                            var fieldValueResult = await RenderScribanTemplateAsync(field.Value, context);
                            if (!fieldValueResult.Success)
                            {
                                result.Code = "FillSessionInformationInPrompt:2";
                                result.Message = "Error parsing system prompt template: " + string.Join(", ", fieldValueResult.Message);
                                return result;
                            }

                            formFields[field.Key] = fieldValueResult.Data;
                        }
                    }
                    return result.SetSuccessResult(new FormUrlEncodedContent(formFields));
                }
                else if (toolConfig.BodyType == HttpBodyEnum.Raw)
                {
                    string rawBody = toolConfig.BodyData?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(rawBody))
                    {
                        var rawBodyResult = await RenderScribanTemplateAsync(rawBody, context);
                        if (!rawBodyResult.Success)
                        {
                            result.Code = "FillSessionInformationInPrompt:3";
                            result.Message = "Error parsing system prompt template: " + string.Join(", ", rawBodyResult.Message);
                            return result;
                        }

                        rawBody = rawBodyResult.Data;
                    }
                    return result.SetSuccessResult(new StringContent(rawBody, Encoding.UTF8, "application/json"));// Default to JSON, could be configurable
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build request body for tool {ToolId}", toolId);
                // Maybe throw or return null, depending on desired error handling
                result.Code = "FillSessionInformationInPrompt:4";
                result.Message = "Error building request body for tool " + toolId + ": " + ex.Message;
                return result;
            }

            result.Code = "FillSessionInformationInPrompt:5";
            result.Message = "Failed to build request body for tool " + toolId;
            return result;
        }


        private FunctionReturnResult<Dictionary<string, object>> BuildAndValidateCustomToolVariables(BusinessAppTool toolData, Dictionary<string, JsonElement> parameters)
        {
            var result = new FunctionReturnResult<Dictionary<string, object>> { Data = new Dictionary<string, object>() };
            var errors = new List<string>();

            if (toolData.Configuration?.InputSchemea == null || toolData.Configuration.InputSchemea.Count == 0)
            {
                result.Success = true;
                return result;
            }


            foreach (var toolInputProperty in toolData.Configuration.InputSchemea)
            {
                bool foundValue = parameters.TryGetValue(toolInputProperty.Id, out JsonElement parameterValue);

                if (toolInputProperty.IsRequired)
                {
                    if (!foundValue)
                    {
                        errors.Add($"Required parameter '{toolInputProperty.Id}' ('{toolInputProperty.Name}') is missing.");
                        continue;
                    }
                    if (parameterValue.ValueKind == JsonValueKind.Null || parameterValue.ValueKind == JsonValueKind.Undefined)
                    {
                        errors.Add($"Required parameter '{toolInputProperty.Id}' ('{toolInputProperty.Name}') cannot be null or undefined.");
                        continue;
                    }
                }

                if (!foundValue) continue; // Optional and not provided

                if (toolInputProperty.IsArray)
                {
                    if (parameterValue.ValueKind != JsonValueKind.Array)
                    {
                        errors.Add($"Parameter '{toolInputProperty.Id}' ('{toolInputProperty.Name}') must be an array, but received {parameterValue.ValueKind}.");
                        continue;
                    }

                    var listValues = new List<object?>(); // todo Allow nulls within arrays if needed?
                    int itemIndex = 0;
                    foreach (var item in parameterValue.EnumerateArray())
                    {
                        var itemResult = ParseJsonElementAsType(toolInputProperty, item, $"item at index {itemIndex}");
                        if (!itemResult.Success)
                        {
                            errors.Add($"Error in array parameter '{toolInputProperty.Id}': {itemResult.Message}");
                            // Skip rather than breaking to make sure other values are validated
                        }
                        else
                        {
                            listValues.Add(itemResult.Data);
                        }
                        itemIndex++;
                    }
                    if (errors.Count == 0) // Only add if no errors encountered *within* the array processing for this param
                    {
                        result.Data.Add(toolInputProperty.Id, listValues);
                    }
                }
                else
                {
                    var itemResult = ParseJsonElementAsType(toolInputProperty, parameterValue);
                    if (!itemResult.Success)
                    {
                        errors.Add(itemResult.Message);
                    }
                    else
                    {
                        result.Data.Add(toolInputProperty.Id, itemResult.Data!); // Add validated data
                    }
                }
            }

            if (errors.Any())
            {
                _logger.LogWarning("Parameter validation failed for tool {ToolId}: {Errors}", toolData.Id, string.Join("; ", errors));
                result.Success = false;
                result.Message = string.Join("\n", errors);
            }
            else
            {
                _logger.LogDebug("Parameter validation successful for tool {ToolId}", toolData.Id);
                result.Success = true;
            }

            return result;
        }

        private FunctionReturnResult<object?> ParseJsonElementAsType(BusinessAppToolConfigurationInputSchemea toolInputProperty, JsonElement parameterValue, string context = "")
        {
            var result = new FunctionReturnResult<object?>();
            string paramDesc = $"Parameter '{toolInputProperty.Id}' ('{toolInputProperty.Name}'){(string.IsNullOrEmpty(context) ? "" : $" ({context})")}";

            try
            {
                // Handle explicit null - this should usually be caught by IsRequired check earlier if needed
                if (parameterValue.ValueKind == JsonValueKind.Null || parameterValue.ValueKind == JsonValueKind.Undefined)
                {
                    if (toolInputProperty.IsRequired) // Double check, though BuildAndValidate should catch it todo, the array could be required but the sub value could include null? possible?
                    {
                        result.Message = $"{paramDesc} is required but received null/undefined.";
                        return result;
                    }
                    result.Success = true;
                    result.Data = null; // Allow null for optional non-array types
                    return result;
                }


                switch (toolInputProperty.Type)
                {
                    case BusinessAppToolConfigurationInputSchemeaTypeEnum.String:
                        if (parameterValue.ValueKind == JsonValueKind.String)
                            result.Data = parameterValue.GetString();
                        else if (parameterValue.ValueKind == JsonValueKind.Number || parameterValue.ValueKind == JsonValueKind.True || parameterValue.ValueKind == JsonValueKind.False)
                            result.Data = parameterValue.ToString(); // Allow conversion from primitives
                        else
                            result.Message = $"{paramDesc} must be a string (or convertible primitive), but received {parameterValue.ValueKind}.";
                        break;

                    case BusinessAppToolConfigurationInputSchemeaTypeEnum.Number:
                        if (parameterValue.ValueKind == JsonValueKind.Number)
                        {
                            result.Data = parameterValue.GetDouble(); // Store as double, check for int later if needed
                        }
                        else if (parameterValue.ValueKind == JsonValueKind.String && double.TryParse(parameterValue.GetString(), out double parsedDouble))
                        {
                            result.Data = parsedDouble;
                        }
                        else
                        {
                            result.Message = $"{paramDesc} must be a number (or numeric string), but received {parameterValue.ValueKind}.";
                        }
                        break;

                    case BusinessAppToolConfigurationInputSchemeaTypeEnum.Boolean:
                        if (parameterValue.ValueKind == JsonValueKind.True || parameterValue.ValueKind == JsonValueKind.False)
                            result.Data = parameterValue.GetBoolean();                  
                        else if (parameterValue.ValueKind == JsonValueKind.String && bool.TryParse(parameterValue.GetString(), out bool parsedBool))
                            result.Data = parsedBool;
                        else
                            result.Message = $"{paramDesc} must be a boolean, but received {parameterValue.ValueKind}.";
                        break;

                    case BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime:
                        if (parameterValue.ValueKind == JsonValueKind.String && DateTime.TryParse(parameterValue.GetString(), out DateTime parsedDate))
                            result.Data = parameterValue.ToString();
                        else
                            result.Message = $"{paramDesc} must be a valid datetime string, but received {parameterValue.ValueKind}.";
                        break;

                    default:
                        result.Message = $"{paramDesc} has an unsupported target type: {toolInputProperty.Type}";
                        break;
                }

                result.Success = string.IsNullOrEmpty(result.Message);
                if (result.Data is double)
                {
                    bool isValueInt = Math.Abs((double)result.Data - (int)result.Data) < double.Epsilon;
                    if (isValueInt) result.Data = (int)result.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON element for {ParamDesc}", paramDesc);
                result.Message = $"Internal error parsing value for {paramDesc}.";
            }

            return result;
        }


        // JavaScript Execution (Restricted Engine)
        private FunctionReturnResult<string?> ExecuteJavaScriptProcessor(string responseData, string javascriptCode)
        {
            var result = new FunctionReturnResult<string?>();
            if (string.IsNullOrWhiteSpace(javascriptCode))
            {
                result.Success = true;
                result.Data = responseData; // Return raw data if no script
                return result;
            }

            var engine = new Engine(options => {
                options.TimeoutInterval(TimeSpan.FromSeconds(5));
                options.LimitMemory(5_000_000); // 5 MB
                options.LimitRecursion(64);
                options.MaxStatements(1000);
                options.Strict(true); // Enable strict mode
                options.AllowClrWrite(false);
                options.MaxArraySize(5000);
                options.MaxJsonParseDepth(32);
                options.RegexTimeoutInterval(TimeSpan.FromMilliseconds(500));
                options.DisableStringCompilation();
            });

            try
            {
                // Provide response data and a way to parse it if it's JSON
                engine.SetValue("responseData", responseData);

                // Execute the user's script
                var executionResult = engine.Evaluate(javascriptCode);
                var executionObject = executionResult.ToObject();

                if (executionObject == null)
                {
                    result.Data = "null";
                }
                else if (executionObject is string strResult)
                {
                    result.Data = strResult;
                }
                else
                {
                    result.Data = JsonSerializer.Serialize(executionObject, new JsonSerializerOptions { WriteIndented = false });
                }

                result.Success = true;
                return result;
            }
            catch (Jint.Runtime.JavaScriptException jsEx)
            {
                _logger.LogError(jsEx, "JavaScript execution error in processor: {JSMessage}", jsEx.Message);
                result.Code = "ExecuteJavaScriptProcessor:-1";
                result.Message = "Error executing JavaScript processor: " + jsEx.Message;
                return result;
            }
            catch (Exception ex) // Catch other potential errors (Timeout, Memory Limit, etc.)
            {
                _logger.LogError(ex, "Error during JavaScript processing (engine level)");
                result.Code = "ExecuteJavaScriptProcessor:-2";
                result.Message = "Error during JavaScript processing: " + ex.Message;
                return result;
            }
        }

        // URI and Variable Resolution
        public static async Task<FunctionReturnResult<string?>> ResolveEndpointQueryStrings(string endpoint, Dictionary<string, object> parameters)
        {
            var result = new FunctionReturnResult<string?>();

            // First resolve any variables in the endpoint string
            var fullUriResult = await RenderScribanTemplateAsync(endpoint, parameters);
            if (!fullUriResult.Success)
            {
                result.Code = "ResolveEndpointQueryStrings:" + fullUriResult.Code;
                result.Message = fullUriResult.Message;
                return result;
            }

            string fullUri = fullUriResult.Data;

            // Parse the URL manually to avoid automatic + to space conversion
            int queryStartIndex = fullUri.IndexOf('?');
            if (queryStartIndex == -1)
            {
                return result.SetSuccessResult(fullUri);
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
            return result.SetSuccessResult($"{baseUrlPart}?{string.Join("&", processedPairs)}");
        }


        // --- Scriban Integration ---
        private static readonly Func<string, DateTime> ParseDateTimeFunc = (inputString) => DateTime.Parse(inputString);
        public static async Task<FunctionReturnResult<string?>> RenderScribanTemplateAsync(string? templateText, Dictionary<string, object?> context)
        {
            var result = new FunctionReturnResult<string?>();
            if (string.IsNullOrEmpty(templateText))
            {
                result.Success = true;
                result.Data = templateText; // Return null or empty string as is
                return result;
            }

            try
            {
                var template = Template.Parse(templateText);
                if (template.HasErrors)
                {
                    var errors = string.Join("\n", template.Messages.Select(m => m.Message));
                    // _logger.LogWarning("Scriban template parsing failed: {Errors}", errors); // Need logger instance or make static
                    result.Code = "RenderScriban:ParseError";
                    result.Message = $"Template parsing errors:\n{errors}";
                    return result;
                }

                var templateContext = new TemplateContext
                {
                    LoopLimit = 1000,
                    RecursiveLimit = 64,
                    StrictVariables = false,
                    LimitToString = 16000,
                    LoopLimitQueryable = 1000,
                    ObjectRecursionLimit = 1000
                };

                var scriptObject = new ScriptObject();
                foreach (var kvp in context)
                {
                    scriptObject.Add(kvp.Key, kvp.Value);
                }

                // Import custom functions
                scriptObject.Import("datetimeparse", ParseDateTimeFunc);

                scriptObject.Import(typeof(DateTimeFunctions));
                scriptObject.Import(typeof(TimeSpanFunctions));
                scriptObject.Import(typeof(StringFunctions));
                scriptObject.Import(typeof(ObjectFunctions));
                scriptObject.Import(typeof(ArrayFunctions));
                scriptObject.Import(typeof(MathFunctions));
                scriptObject.Import(typeof(RegexFunctions));

                templateContext.PushGlobal(scriptObject);

                result.Data = await template.RenderAsync(templateContext);
                result.Success = true;
            }
            catch (Exception ex) // Catch Scriban runtime errors
            {
                result.Code = "RenderScriban:RuntimeError";
                result.Message = $"Template rendering error: {ex.Message}";
            }

            return result;
        }
    }
}