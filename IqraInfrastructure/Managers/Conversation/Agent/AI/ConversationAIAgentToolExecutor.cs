using IqraInfrastructure.Managers.Conversation.Agent.AI.Helpers;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;


namespace IqraInfrastructure.Managers.Conversation.Agent.AI
{
    public class ConversationAIAgentToolExecutor
    {
        public event Func<string, Task>? ToolResultAvailable;
        public event Func<string, CancellationToken, Task>? PlaySpeechRequested;
        public event Func<string, Task>? EndConversationRequested;
        public event Func<string, string?, Task>? TransferToAIAgentRequested; // (reason, nodeId)
        public event Func<string, string?, Task>? TransferToHumanAgentRequested; // (reason, nodeId)

        private readonly ILogger<ConversationAIAgentToolExecutor> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly ScriptExecutionManager _scriptAccessor;
        private readonly CustomToolExecutionHelper _customToolHelper;
        private readonly ConversationAIAgentDTMFSessionManager _dtmfSessionManager;

        public ConversationAIAgentToolExecutor(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            ScriptExecutionManager scriptAccessor,
            CustomToolExecutionHelper customToolHelper,
            ConversationAIAgentDTMFSessionManager dtmfSessionManager
        )
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentToolExecutor>();
            _agentState = agentState;
            _scriptAccessor = scriptAccessor;
            _customToolHelper = customToolHelper;
            _dtmfSessionManager = dtmfSessionManager;
        }

        public async Task InitializeAsync()
        {
            if (_agentState.BusinessApp == null || _agentState.CurrentSessionRoute == null || string.IsNullOrEmpty(_agentState.CurrentLanguageCode))
            {
                _logger.LogError("Agent {AgentId}: Cannot initialize ScriptExecutionManager - missing required state.", _agentState.AgentId);
                throw new InvalidOperationException("ScriptExecutionManager requires BusinessApp, Route, and LanguageCode.");
            }

            // ScriptAccessor loads the script data
            await _scriptAccessor.LoadScriptAsync(
               _agentState.BusinessApp,
               _agentState.CurrentSessionRoute,
               _agentState.CurrentLanguageCode
            );

            // Initialize the helper (it needs business app data)
            _customToolHelper.Initialize(_agentState.BusinessApp, _agentState.CurrentLanguageCode);
        }

        public async Task ReInitializeForLanguageAsync()
        {
            await InitializeAsync();
        }

        public async Task HandleSystemToolAsync(string functionContent, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Agent {AgentId}: Handling system tool: {FunctionContent}", _agentState.AgentId, functionContent);
            try
            {
                string toolName;
                string argsRaw = string.Empty;
                int colonIndex = functionContent.IndexOf(':');

                if (colonIndex > 0)
                {
                    toolName = functionContent.Substring(0, colonIndex).Trim();
                    argsRaw = functionContent.Substring(colonIndex + 1).Trim();
                }
                else
                {
                    toolName = functionContent.Trim();
                    await ToolResultAvailable?.Invoke($"Error: Invalid system tool format '{functionContent}'. Expected 'tool_name: args'.");
                    return;
                }

                // Use the robust parser
                List<string> arguments = ParseArguments(argsRaw);

                // Standard arguments often: reason, messageToSpeak, nodeId (optional for some)
                string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "No reason provided";
                string? messageToSpeak = arguments.Count > 1 ? UnescapeNullableArgument(arguments[1]) : null;
                string? nodeId = arguments.Count > 2 ? UnescapeNullableArgument(arguments[2]) : null; // Node ID is often the 3rd arg

                // Play message *before* executing the core logic (if provided)
                if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                {
                    // todo we need to set is responding system tool here
                    await PlaySpeechRequested.Invoke(messageToSpeak, cancellationToken);
                    // Check for cancellation after potentially long speech
                    if (cancellationToken.IsCancellationRequested)
                    {
                        await ToolResultAvailable?.Invoke($"Custom tool execution cancelled after playing message.");
                        return;
                    }
                }

                // Tool Specific Logic
                if (toolName.Equals("end_call", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: end_call: string <reason>, string | null <message>, string | null <node_id>
                    if (EndConversationRequested != null)
                    {
                        await EndConversationRequested.Invoke(reason);
                    }
                }
                else if (toolName.Equals("transfer_to_ai_agent", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: transfer_to_ai_agent: string <reason>, string | null <message>, string <node_id>
                    if (string.IsNullOrWhiteSpace(nodeId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Transfer to AI agent requires a Node ID.");
                    }
                    else if (TransferToAIAgentRequested != null)
                    {
                        await TransferToAIAgentRequested.Invoke(reason, nodeId);
                    }
                }
                else if (toolName.Equals("transfer_to_human_agent", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: transfer_to_human_agent: string <reason>, string | null <message>, string <node_id>
                    if (string.IsNullOrWhiteSpace(nodeId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Transfer to human agent requires a Node ID.");
                    }
                    else if (TransferToHumanAgentRequested != null)
                    {
                        await TransferToHumanAgentRequested.Invoke(reason, nodeId);
                    }
                }
                else if (toolName.Equals("recieve_dtmf_input", StringComparison.OrdinalIgnoreCase)) // Renamed in prompt? Check consistency. Assuming 'recieve_dtmf_input'
                {
                    // Format: recieve_dtmf_input: string <reason>, string | null <message>, string <node_id>
                    if (string.IsNullOrWhiteSpace(nodeId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Receive DTMF input requires a Node ID.");
                    }
                    else
                    {
                        // Get DTMF configuration from the script node using ScriptAccessor
                        var dtmfNodeResult = _scriptAccessor.GetDTMFNodeDetails(nodeId); // Need to add this method to ScriptExecutionManager
                        if (!dtmfNodeResult.Success || dtmfNodeResult.Data == null)
                        {
                            await ToolResultAvailable?.Invoke($"Error: Could not find node with id '{nodeId}' to request DTMF input. {dtmfNodeResult.Message}");
                        }
                        else
                        {
                            var dtmfConfig = dtmfNodeResult.Data;
                            bool started = _dtmfSessionManager.StartSession(dtmfConfig);
                            if (!started)
                            {
                                await ToolResultAvailable?.Invoke($"Error: Could not start DTMF session for node '{nodeId}', another session seems to already be active.");
                            }
                            else
                            {
                                // todo maybe do nothing here and wait for session to complete/fail/cancel etc
                                //await ToolResultAvailable?.Invoke($"System tool 'recieve_dtmf_input' for node '{nodeId}' initiated. Awaiting user input.");
                            }
                        }
                    }
                }
                else if (toolName.Equals("retrieve_product_information", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: retrieve_product_information: string <reason>, string <product_id>
                    string productId = arguments.Count > 1 ? UnescapeArgument(arguments[1]) : ""; // Product ID is 2nd arg
                    if (string.IsNullOrWhiteSpace(productId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Retrieve product information requires a Product ID.");
                    }
                    else
                    {
                        // TODO: Implement actual retrieval logic (e.g., call a business service)

                        await ToolResultAvailable?.Invoke($"System tool 'retrieve_product_information' for product '{productId}' acknowledged but backend retrieval is not implemented.");
                    }
                }
                else if (toolName.Equals("retrieve_service_information", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: retrieve_service_information: string <reason>, string <service_id>
                    string serviceId = arguments.Count > 1 ? UnescapeArgument(arguments[1]) : ""; // Service ID is 2nd arg
                    if (string.IsNullOrWhiteSpace(serviceId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Retrieve service information requires a Service ID.");
                    }
                    else
                    {
                        // TODO: Implement actual retrieval logic

                        await ToolResultAvailable?.Invoke($"System tool 'retrieve_service_information' for service '{serviceId}' acknowledged but backend retrieval is not implemented.");
                    }
                }
                // --- Other potential system tools from prompt (no format examples given) ---
                else if (toolName.Equals("change_language", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Parse language code argument? Trigger language change flow.

                    await ToolResultAvailable?.Invoke("System tool 'change_language' acknowledged but not implemented.");
                }
                else if (toolName.Equals("press_dtmf_keypad", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Parse keys to press argument? Trigger DTMF sending mechanism. Add to LLM context.

                    await ToolResultAvailable?.Invoke("System tool 'press_dtmf_keypad' acknowledged but not implemented.");
                }
                else if (toolName.Equals("add_script_to_context", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Parse script content/ID argument? Add to LLM context.

                    await ToolResultAvailable?.Invoke("System tool 'add_script_to_context' acknowledged but not implemented.");
                }
                else if (toolName.Equals("acknowledge", StringComparison.OrdinalIgnoreCase)) // Keep internal acknowledge
                {
                    var ackMessage = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "Acknowledged";
                    // todo is this even needed?
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId}: Received unknown system tool: {ToolName}", _agentState.AgentId, toolName);
                    await ToolResultAvailable?.Invoke($"Error: Unknown system tool '{toolName}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error executing system tool: {FunctionContent}", _agentState.AgentId, functionContent);
                await ToolResultAvailable?.Invoke($"Error executing system tool: {ex.Message}");
            }
        }

        public async Task HandleCustomToolAsync(string functionContent, CancellationToken cancellationToken)
        {
            try
            {
                // Format: execute_custom_function: string <reason>, string | null <message>, string <node_id>, Dictionary | null <vars>
                List<string> arguments = ParseArguments(functionContent);

                if (arguments.Count < 3) // Need at least reason, message (even if null), and node_id
                {
                    await ToolResultAvailable?.Invoke("Error: Invalid custom tool format. Expected at least reason (required), message (empty or null allowed), node_id (required).");
                    return;
                }

                string reasonForExecuting = UnescapeArgument(arguments[0]);
                string? messageToSpeak = UnescapeNullableArgument(arguments[1]); // NEW message argument
                string nodeIdToExecute = UnescapeArgument(arguments[2]);
                string? nodeVariableValuesJson = arguments.Count >= 4 ? arguments[3] : null; // Variables are 4th arg, optional

                if (string.IsNullOrWhiteSpace(nodeIdToExecute))
                {
                    await ToolResultAvailable?.Invoke("Error: Invalid custom tool format. Node ID is required and cannot be empty.");
                    return;
                }

                // Play Message Before Execution
                if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                {
                    // todo we need to set is responding custom tool here
                    await PlaySpeechRequested.Invoke(messageToSpeak, cancellationToken);
                    // Check for cancellation after potentially long speech
                    if (cancellationToken.IsCancellationRequested)
                    {
                        await ToolResultAvailable?.Invoke($"Custom tool execution cancelled after playing message.");
                        return;
                    }
                }

                // Get Tool Details from Script Node
                var nodeDetailsResult = _scriptAccessor.GetCustomToolNodeDetails(nodeIdToExecute);
                if (!nodeDetailsResult.Success || nodeDetailsResult.Data == null)
                {
                    await ToolResultAvailable?.Invoke($"Error: Could not find node with ID '{nodeIdToExecute}' for custom tool exection. {nodeDetailsResult.Message}");
                    return;
                }
                var toolData = nodeDetailsResult.Data;

                // Parse Variables
                Dictionary<string, JsonElement>? nodeVariables = null;
                if (!string.IsNullOrEmpty(nodeVariableValuesJson) && !nodeVariableValuesJson.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Trim outer quotes if ParseArguments included them (it shouldn't for valid JSON object)
                        var trimmedJson = nodeVariableValuesJson.Trim();
                        if (trimmedJson.StartsWith("\"") && trimmedJson.EndsWith("\""))
                        {
                            // Attempt to unescape JSON within a JSON string - complex, try basic first
                            try
                            {
                                trimmedJson = JsonSerializer.Deserialize<string>(trimmedJson) ?? trimmedJson;
                            }
                            catch
                            {
                                // If deserialize as string fails, use the raw unescaped version
                                trimmedJson = nodeVariableValuesJson.Substring(1, nodeVariableValuesJson.Length - 2).Replace("\\\"", "\"");
                            }
                        }

                        if (trimmedJson.StartsWith("{") && trimmedJson.EndsWith("}"))
                        {
                            nodeVariables = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(trimmedJson);
                        }
                        else
                        {
                            await ToolResultAvailable?.Invoke($"Error: Variables provided for custom tool node '{nodeIdToExecute}' were not a valid JSON object.");
                            return; // Fail if variables are expected but invalid format
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        await ToolResultAvailable?.Invoke($"Error: Could not parse JSON variables for custom tool node '{nodeIdToExecute}'. Malformed JSON.");
                        return;
                    }
                }


                // Execute the Custom Tool via Helper
                var executeResult = await _customToolHelper.ExecuteHttpRequestForToolAsync(
                    toolData,
                    nodeVariables ?? new Dictionary<string, JsonElement>(),
                    cancellationToken
                );

                // --- Notify Result ---
                if (!executeResult.Success)
                {
                    await ToolResultAvailable?.Invoke(executeResult.Message ?? $"Failed to execute custom tool with node id '{nodeIdToExecute}'.");
                }
                else
                {
                    await ToolResultAvailable?.Invoke(executeResult.Data ?? $"Successfully executed custom tool with node id '{nodeIdToExecute}'.");
                }

            }
            catch (OperationCanceledException)
            {
                await ToolResultAvailable?.Invoke($"Custom tool execution cancelled.");
            }
            catch (Exception ex)
            {
                await ToolResultAvailable?.Invoke($"Unexpected error executing custom tool: {ex.Message}");
            }
        }

        // Argument Parsing Helpers
        private static List<string> ParseArguments(string input)
        {
            // This parser seems robust enough, keep as is.
            List<string> arguments = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return arguments;

            int currentPos = 0;
            int jsonDepth = 0; // Track nesting for JSON objects/arrays
            StringBuilder currentArg = new StringBuilder();
            bool inQuotes = false;
            char? prevC = null;

            while (currentPos < input.Length)
            {
                char c = input[currentPos];

                if (c == '"' && prevC != '\\') // Handle escaped quotes correctly
                {
                    inQuotes = !inQuotes;
                    currentArg.Append(c);
                }
                else if (c == '{' && !inQuotes)
                {
                    jsonDepth++;
                    currentArg.Append(c);
                }
                else if (c == '}' && !inQuotes)
                {
                    if (jsonDepth > 0) jsonDepth--;
                    currentArg.Append(c);
                }
                else if (c == '[' && !inQuotes) // Handle JSON arrays too
                {
                    jsonDepth++;
                    currentArg.Append(c);
                }
                else if (c == ']' && !inQuotes)
                {
                    if (jsonDepth > 0) jsonDepth--;
                    currentArg.Append(c);
                }
                else if (c == ',' && !inQuotes && jsonDepth == 0)
                {
                    arguments.Add(currentArg.ToString().Trim());
                    currentArg.Clear();
                }
                else
                {
                    currentArg.Append(c);
                }

                prevC = c;
                currentPos++;
            }

            if (currentArg.Length > 0)
            {
                arguments.Add(currentArg.ToString().Trim());
            }

            return arguments;
        }
        private static string UnescapeArgument(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg)) return string.Empty;
            arg = arg.Trim();

            // Don't treat "null" as special here unless specifically needed, let UnescapeNullableArgument handle it.

            if (arg.Length >= 2 && arg.StartsWith("\"") && arg.EndsWith("\""))
            {
                // Basic unescaping: \\ -> \ and \" -> "
                return arg.Substring(1, arg.Length - 2)
                          .Replace("\\\"", "\"")
                          .Replace("\\\\", "\\");
            }
            return arg; // Return as-is if not quoted
        }
        private static string? UnescapeNullableArgument(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg)) return null;
            arg = arg.Trim();

            if (arg.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;

            // Check if it's the *quoted* string "null"
            if (arg.Equals("\"null\"", StringComparison.OrdinalIgnoreCase)) return null;


            if (arg.Length >= 2 && arg.StartsWith("\"") && arg.EndsWith("\""))
            {
                return arg.Substring(1, arg.Length - 2)
                         .Replace("\\\"", "\"")
                         .Replace("\\\\", "\\");
            }

            // If it wasn't the literal null or quoted, return the value itself
            return arg;
        }
    }
}