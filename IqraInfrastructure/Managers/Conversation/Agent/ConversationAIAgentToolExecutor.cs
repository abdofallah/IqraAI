using IqraInfrastructure.Managers.Script;
using IqraInfrastructure.Managers.Business; // For session manager (if needed for EndAsync)
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text; // For ParseArguments
using System.Text.Json; // For JSON parsing
using System.Threading;
using System.Threading.Tasks;


namespace IqraInfrastructure.Managers.Conversation.Modules
{
    public class ConversationAIAgentToolExecutor
    {
        // Event to notify Orchestrator/LLMHandler about the result
        public event Func<string, Task>? ToolResultAvailable; // Sends result message back to LLM
                                                              // Event to request speaking (e.g., message before ending call)
        public event Func<string, CancellationToken, Task>? PlaySpeechRequested; // Requests AudioOutput to speak
                                                                                 // Event to end the conversation session
        public event Func<string, Task>? EndConversationRequested; // Requests Orchestrator/SessionManager to end


        private readonly ILogger<ConversationAIAgentToolExecutor> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly ScriptExecutionManager _scriptExecutionManager;
        // Avoid direct dependency on SessionManager if possible, use event instead
        // private readonly ConversationSessionManager _conversationSessionManager;

        public ConversationAIAgentToolExecutor(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            ScriptExecutionManager scriptExecutionManager)
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentToolExecutor>();
            _agentState = agentState;
            _scriptExecutionManager = scriptExecutionManager;
        }

        public async Task InitializeAsync(CancellationToken agentToken) // Added token
        {
            // --- Move ScriptExecutionManager loading logic here if not done elsewhere ---
            _logger.LogInformation("Agent {AgentId}: Initializing ScriptExecutionManager...", _agentState.AgentId);
            if (_agentState.BusinessApp == null || _agentState.CurrentSessionRoute == null || string.IsNullOrEmpty(_agentState.CurrentLanguageCode))
            {
                _logger.LogError("Agent {AgentId}: Cannot initialize ScriptExecutionManager - missing required state.", _agentState.AgentId);
                throw new InvalidOperationException("ScriptExecutionManager requires BusinessApp, Route, and LanguageCode.");
            }

            // Cancelled operation is not expected here unless the agentToken is cancelled during init
            await _scriptExecutionManager.LoadScriptAsync(
               _agentState.BusinessApp,
               _agentState.CurrentSessionRoute,
               _agentState.CurrentLanguageCode,
               agentToken); // Pass token if LoadScriptAsync supports it

            _logger.LogInformation("Tool Executor initialized for Agent {AgentId}.", _agentState.AgentId);
        }

        public async Task ReInitializeForLanguageAsync(CancellationToken agentToken) // Added token
        {
            _logger.LogInformation("Agent {AgentId}: Re-initializing ScriptExecutionManager for new language.", _agentState.AgentId);
            await InitializeAsync(agentToken); // Re-run full init logic
        }

        public async Task HandleSystemToolAsync(string functionContent, CancellationToken cancellationToken)
        {
            // --- Move logic from HandleLLMSystemToolResponseCompletedAsync here ---
            // Parse functionContent using ParseArguments
            // Handle "end_call", "transfer_to_agent", "receive_dtmf_input", etc.
            // Raise events like PlaySpeechRequested or EndConversationRequested

            _logger.LogInformation("Agent {AgentId}: Handling system tool: {FunctionContent}", _agentState.AgentId, functionContent);
            try
            {
                // Basic parsing assuming format "toolName: arg1, arg2, ..."
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
                    toolName = functionContent.Trim(); // Tool with no arguments
                }

                List<string> arguments = ParseArguments(argsRaw); // Parse the arguments part

                _logger.LogDebug("Agent {AgentId}: Parsed System Tool '{ToolName}' with {ArgCount} args.", _agentState.AgentId, toolName, arguments.Count);

                if (toolName.Equals("end_call", StringComparison.OrdinalIgnoreCase))
                {
                    string reasonForEnding = "";
                    string? messageToSpeak = null;

                    if (arguments.Count >= 1)
                    {
                        reasonForEnding = UnescapeArgument(arguments[0]);
                    }
                    if (arguments.Count >= 2)
                    {
                        messageToSpeak = UnescapeArgument(arguments[1]);
                        if (string.IsNullOrWhiteSpace(messageToSpeak) || messageToSpeak.Equals("null", StringComparison.OrdinalIgnoreCase))
                        {
                            messageToSpeak = null;
                        }
                    }

                    _logger.LogInformation("Agent {AgentId}: Processing end_call. Reason: '{Reason}', Message: '{Message}'", _agentState.AgentId, reasonForEnding, messageToSpeak ?? "None");

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                    {
                        _logger.LogDebug("Agent {AgentId}: Requesting to play end call message.", _agentState.AgentId);
                        await PlaySpeechRequested.Invoke(messageToSpeak, cancellationToken);
                        // Wait for speech to finish? The orchestrator might need to coordinate this.
                        // Adding a small delay might be pragmatic but not robust.
                        await Task.Delay(200, cancellationToken); // Small delay, adjust as needed
                    }

                    if (EndConversationRequested != null)
                    {
                        _logger.LogDebug("Agent {AgentId}: Requesting to end conversation.", _agentState.AgentId);
                        await EndConversationRequested.Invoke(reasonForEnding);
                    }
                }
                else if (toolName.Equals("transfer_to_agent", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Implement transfer logic
                    _logger.LogWarning("Agent {AgentId}: 'transfer_to_agent' system tool not fully implemented.", _agentState.AgentId);
                    await NotifyToolResultAsync("System tool 'transfer_to_agent' acknowledged but not fully implemented.");
                }
                else if (toolName.Equals("receive_dtmf_input", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Implement DTMF input request logic
                    _logger.LogWarning("Agent {AgentId}: 'receive_dtmf_input' system tool not fully implemented.", _agentState.AgentId);
                    await NotifyToolResultAsync("System tool 'receive_dtmf_input' acknowledged but not fully implemented.");
                }
                else if (toolName.Equals("acknowledge", StringComparison.OrdinalIgnoreCase))
                {
                    // This seems to be used internally for warmup/state sync
                    var ackMessage = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "Acknowledged";
                    _logger.LogDebug("Agent {AgentId}: System tool 'acknowledge' received: {Message}", _agentState.AgentId, ackMessage);
                    // No result needs to be sent back to LLM usually for simple ack
                }
                // Add other system tools here...
                else
                {
                    _logger.LogWarning("Agent {AgentId}: Received unknown system tool: {ToolName}", _agentState.AgentId, toolName);
                    await NotifyToolResultAsync($"Error: Unknown system tool '{toolName}'.");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error executing system tool: {FunctionContent}", _agentState.AgentId, functionContent);
                await NotifyToolResultAsync($"Error executing system tool: {ex.Message}");
            }
        }

        public async Task HandleCustomToolAsync(string functionContent, CancellationToken cancellationToken)
        {
            // --- Move logic from HandleLLMCustomToolResponseCompletedAsync here ---
            // Parse functionContent using ParseArguments
            // Extract node ID and variables
            // Call _scriptExecutionManager.ExecuteCustomToolAsync
            // Raise ToolResultAvailable event with the outcome

            _logger.LogInformation("Agent {AgentId}: Handling custom tool: {FunctionContent}", _agentState.AgentId, functionContent);

            try
            {
                List<string> arguments = ParseArguments(functionContent);

                string reasonForExecuting = ""; // Optional, maybe just for logging
                string nodeIdToExecute = "";
                string? nodeVariableValuesJson = null;

                if (arguments.Count >= 1) reasonForExecuting = UnescapeArgument(arguments[0]);
                if (arguments.Count >= 2) nodeIdToExecute = UnescapeArgument(arguments[1]);
                if (arguments.Count >= 3) nodeVariableValuesJson = arguments[2]; // Keep raw JSON string


                if (string.IsNullOrWhiteSpace(nodeIdToExecute))
                {
                    _logger.LogError("Agent {AgentId}: Invalid custom tool call - Node ID is missing. Content: {Content}", _agentState.AgentId, functionContent);
                    await NotifyToolResultAsync("Error: Invalid custom tool format. Node ID is required.");
                    return;
                }

                _logger.LogDebug("Agent {AgentId}: Parsed Custom Tool. Reason: '{Reason}', NodeID: '{NodeId}', Vars: '{Vars}'",
                    _agentState.AgentId, reasonForExecuting, nodeIdToExecute, nodeVariableValuesJson ?? "None");


                Dictionary<string, JsonElement>? nodeVariables = null;
                if (!string.IsNullOrEmpty(nodeVariableValuesJson) && nodeVariableValuesJson != "null")
                {
                    try
                    {
                        // Trim outer quotes if ParseArguments included them
                        var trimmedJson = nodeVariableValuesJson.Trim();
                        if (trimmedJson.StartsWith("\"") && trimmedJson.EndsWith("\""))
                        {
                            trimmedJson = trimmedJson.Substring(1, trimmedJson.Length - 2).Replace("\\\"", "\""); // Basic unescaping
                        }

                        if (trimmedJson.StartsWith("{") && trimmedJson.EndsWith("}"))
                        {
                            nodeVariables = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(trimmedJson);
                        }
                        else
                        {
                            _logger.LogWarning("Agent {AgentId}: Custom tool variables argument was not a valid JSON object string: {JsonString}", _agentState.AgentId, nodeVariableValuesJson);
                            // Proceed without variables? Or error out? Let's proceed without.
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Agent {AgentId}: Error parsing custom tool JSON variables: {JsonString}", _agentState.AgentId, nodeVariableValuesJson);
                        await NotifyToolResultAsync($"Error: Could not parse JSON variables for custom tool '{nodeIdToExecute}'.");
                        return;
                    }
                }

                // Execute the custom tool
                var executeResult = await _scriptExecutionManager.ExecuteCustomToolAsync(
                    nodeIdToExecute,
                    nodeVariables ?? new Dictionary<string, JsonElement>(),
                    cancellationToken
                ); // Pass cancellation token

                if (!executeResult.Success)
                {
                    _logger.LogError("Agent {AgentId}: Failed to execute custom tool '{NodeId}'. Reason: {Message}, Data: {Data}", _agentState.AgentId, nodeIdToExecute, executeResult.Message, executeResult.Data);
                    await NotifyToolResultAsync($"Failed to execute custom tool '{nodeIdToExecute}': {executeResult.Message} - {executeResult.Data}");
                }
                else
                {
                    _logger.LogInformation("Agent {AgentId}: Successfully executed custom tool '{NodeId}'. Response: {Response}", _agentState.AgentId, nodeIdToExecute, executeResult.Data);
                    await NotifyToolResultAsync($"Successfully executed custom tool '{nodeIdToExecute}'. Response: {executeResult.Data}");
                }

            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Agent {AgentId}: Custom tool execution was cancelled.", _agentState.AgentId);
                await NotifyToolResultAsync($"Custom tool execution cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error executing custom tool: {FunctionContent}", _agentState.AgentId, functionContent);
                await NotifyToolResultAsync($"Error executing custom tool: {ex.Message}");
            }
        }

        private async Task NotifyToolResultAsync(string resultMessage)
        {
            if (ToolResultAvailable != null)
            {
                await ToolResultAvailable.Invoke(resultMessage);
            }
        }

        // --- Move ParseArguments helper here (make private static?) ---
        private static List<string> ParseArguments(string input)
        {
            // ... (Copy the implementation from the original class) ...
            List<string> arguments = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return arguments;

            int currentPos = 0;
            int jsonDepth = 0; // Track nesting for JSON objects/arrays
            StringBuilder currentArg = new StringBuilder();
            bool inQuotes = false;

            while (currentPos < input.Length)
            {
                char c = input[currentPos];
                char? prevC = currentPos > 0 ? input[currentPos - 1] : null;

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
                    // Only decrement if depth > 0 to avoid issues with stray '}'
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
                    // End of argument (if not inside quotes and not inside JSON structure)
                    arguments.Add(currentArg.ToString().Trim());
                    currentArg.Clear();
                }
                else
                {
                    currentArg.Append(c);
                }

                currentPos++;
            }

            // Add the last argument
            if (currentArg.Length > 0)
            {
                arguments.Add(currentArg.ToString().Trim());
            }

            return arguments;
        }

        // Helper to remove surrounding quotes and handle "null" string literal
        private static string UnescapeArgument(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg)) return string.Empty;

            arg = arg.Trim();

            if (arg.Equals("null", StringComparison.OrdinalIgnoreCase)) return string.Empty; // Treat "null" as empty

            if (arg.Length >= 2 && arg.StartsWith("\"") && arg.EndsWith("\""))
            {
                // Basic unescaping of \" -> "
                return arg.Substring(1, arg.Length - 2).Replace("\\\"", "\"");
            }
            return arg; // Return as-is if not quoted
        }
    }
}