using IqraInfrastructure.Managers.Conversation.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Conversation.Client;
using Microsoft.Extensions.Logging;
using PhoneNumbers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace IqraInfrastructure.Managers.Conversation.Agent.AI
{
    public class ConversationAIAgentToolExecutor
    {
        public event Func<string, Task>? ToolResultAvailable;
        public event Func<string, CancellationToken, Task>? PlaySpeechRequested;
        public event Func<string, Task>? EndConversationRequested;
        public event Func<string, string?, Task>? TransferToAIAgentRequested; // (reason, nodeId)
        public event Func<string, string?, Task>? TransferToHumanAgentRequested; // (reason, nodeId)
        public event Func<string, Task>? SendDTMFRequested;
        public event Func<string, Task>? AddContextRequested;
        public event Func<string, Task>? ChangeLanguageRequested;
        public event Func<Task>? InitiateLanguageSelectionRequested;

        private readonly ILogger<ConversationAIAgentToolExecutor> _logger;
        private readonly ConversationSession _conversationSession;
        private readonly ConversationAIAgentState _agentState;
        private readonly ScriptExecutionManager _scriptAccessor;
        private readonly CustomToolExecutionHelper _customToolHelper;
        private readonly ConversationAIAgentDTMFSessionManager _dtmfSessionManager;
        private readonly SendSMSToolExecutionHelper _sendSMSToolExecutionHelper;

        // Static Regex for basic DTMF validation (0-9, *, #, A-D, W - W for pause if needed)
        private static readonly Regex ValidDtmfCharsRegex = new Regex("^[0-9*#A-DW]+$", RegexOptions.Compiled);

        public ConversationAIAgentToolExecutor(
            ILoggerFactory loggerFactory,
            ConversationSession conversationSession,
            ConversationAIAgentState agentState,
            ScriptExecutionManager scriptAccessor,
            CustomToolExecutionHelper customToolHelper,
            ConversationAIAgentDTMFSessionManager dtmfSessionManager,
            SendSMSToolExecutionHelper sendSMSToolExecutionHelper
        )
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentToolExecutor>();

            _conversationSession = conversationSession;
            _agentState = agentState;
            _scriptAccessor = scriptAccessor;
            _customToolHelper = customToolHelper;
            _dtmfSessionManager = dtmfSessionManager;
            _sendSMSToolExecutionHelper = sendSMSToolExecutionHelper;
        }

        public async Task InitializeAsync()
        {
            if (_agentState.BusinessApp == null || _agentState.CurrentSessionContext == null || string.IsNullOrEmpty(_agentState.CurrentLanguageCode))
            {
                _logger.LogError("Agent {AgentId}: Cannot initialize ScriptExecutionManager - missing required state.", _agentState.AgentId);
                throw new InvalidOperationException("ScriptExecutionManager requires BusinessApp, Route, and LanguageCode.");
            }

            // ScriptAccessor loads the script data
            await _scriptAccessor.LoadScriptAsync(
               _agentState.BusinessApp,
               _agentState.CurrentSessionContext,
               _agentState.CurrentLanguageCode
            );

            // Initialize the helper (it needs business app data)
            _customToolHelper.Initialize(_agentState.BusinessApp, _agentState.CurrentLanguageCode);
            _sendSMSToolExecutionHelper.Initalize(_agentState.BusinessApp);
        }

        public async Task ReInitializeForLanguageAsync()
        {
            await InitializeAsync();
        }

        public async Task HandleSystemToolAsync(string functionContent, CancellationToken cancellationToken, string? clientId = null)
        {
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
                    await ToolResultAvailable?.Invoke($"Error: Invalid system tool format '{functionContent}'. Expected 'tool_name: args'.");
                    return;
                }

                List<string> arguments = ParseArguments(argsRaw);

                // Tool Specific Logic
                if (toolName.Equals("end_call", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: end_call: string <reason>, string | null <message>, string | null <node_id>
                    string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "No reason provided";
                    string? messageToSpeak = arguments.Count > 1 ? UnescapeNullableArgument(arguments[1]) : null;
                    // string? nodeId = arguments.Count > 2 ? UnescapeNullableArgument(arguments[2]) : null; // todo NodeId not used right now

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                        await PlaySpeechRequested.Invoke(messageToSpeak, cancellationToken);

                    // TODO make this based on telephony vs web For telephony higher and web low
                    // somehow know estimate accurate of how much sound delay there is?
                    await Task.Delay(2000);

                    if (EndConversationRequested != null)
                        await EndConversationRequested.Invoke(reason);
                    else
                        _logger.LogWarning("Agent {AgentId}: EndConversationRequested event has no subscribers.", _agentState.AgentId);

                }
                else if (toolName.Equals("change_language", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: change_language: string <reason>, boolean <play list>, string | null <language code>
                    // string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "Language change requested"; // todo Reason not used right now
                    bool playList = false;
                    string? targetLanguageCode = null;

                    if (arguments.Count < 2)
                    {
                        await ToolResultAvailable?.Invoke("Error: change_language requires at least reason and playList flag.");
                        return;
                    }

                    if (!bool.TryParse(UnescapeArgument(arguments[1]), out playList))
                    {
                        await ToolResultAvailable?.Invoke($"Error: change_language expects a boolean (true/false) for the second argument (play languages list), got '{arguments[1]}'.");
                        return;
                    }

                    targetLanguageCode = arguments.Count > 2 ? UnescapeNullableArgument(arguments[2]) : null;

                    if (playList)
                    {
                        // Initiate the language selection flow (e.g., DTMF prompt)
                        if (InitiateLanguageSelectionRequested != null)
                            await InitiateLanguageSelectionRequested.Invoke();
                        else
                            _logger.LogWarning("Agent {AgentId}: InitiateLanguageSelectionRequested event has no subscribers.", _agentState.AgentId);
                    }
                    else if (!string.IsNullOrWhiteSpace(targetLanguageCode))
                    {
                        // Request direct language change
                        if (ChangeLanguageRequested != null)
                            await ChangeLanguageRequested.Invoke(targetLanguageCode);
                        else
                            _logger.LogWarning("Agent {AgentId}: ChangeLanguageRequested event has no subscribers.", _agentState.AgentId);
                    }
                    else
                    {
                        await ToolResultAvailable?.Invoke("Error: change_language called with playList=false requires a target language code.");
                    }
                }
                else if (toolName.Equals("transfer_to_ai_agent", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: transfer_to_ai_agent: string <reason>, string | null <message>, string <node_id>
                    string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "AI Agent transfer requested";
                    string? messageToSpeak = arguments.Count > 1 ? UnescapeNullableArgument(arguments[1]) : null;
                    string? nodeId = arguments.Count > 2 ? UnescapeNullableArgument(arguments[2]) : null;

                    if (string.IsNullOrWhiteSpace(nodeId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Transfer to AI agent requires a Node ID.");
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                        await PlaySpeechRequested.Invoke(messageToSpeak, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;

                    if (TransferToAIAgentRequested != null)
                        await TransferToAIAgentRequested.Invoke(reason, nodeId);
                    else
                        _logger.LogWarning("Agent {AgentId}: TransferToAIAgentRequested event has no subscribers.", _agentState.AgentId);
                }
                else if (toolName.Equals("transfer_to_human_agent", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: transfer_to_human_agent: string <reason>, string | null <message>, string <node_id>
                    string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "Human Agent transfer requested";
                    string? messageToSpeak = arguments.Count > 1 ? UnescapeNullableArgument(arguments[1]) : null;
                    string? nodeId = arguments.Count > 2 ? UnescapeNullableArgument(arguments[2]) : null;

                    if (string.IsNullOrWhiteSpace(nodeId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Transfer to human agent requires a Node ID.");
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                        await PlaySpeechRequested.Invoke(messageToSpeak, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;

                    if (TransferToHumanAgentRequested != null)
                        await TransferToHumanAgentRequested.Invoke(reason, nodeId);
                    else
                        _logger.LogWarning("Agent {AgentId}: TransferToHumanAgentRequested event has no subscribers.", _agentState.AgentId);
                }
                else if (toolName.Equals("recieve_dtmf_input", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: recieve_dtmf_input: string <reason>, string | null <message>, string <node_id>
                    // string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "DTMF input requested";
                    string? messageToSpeak = arguments.Count > 1 ? UnescapeNullableArgument(arguments[1]) : null;
                    string? nodeId = arguments.Count > 2 ? UnescapeNullableArgument(arguments[2]) : null;

                    if (string.IsNullOrWhiteSpace(nodeId))
                    {
                        _logger.LogError("Agent {AgentId}: Receive DTMF input requested but Node ID is missing.", _agentState.AgentId);
                        await ToolResultAvailable?.Invoke("Error: Receive DTMF input requires a Node ID.");
                        return;
                    }

                    // Play message *before* starting the session
                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                        await PlaySpeechRequested.Invoke(messageToSpeak, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;

                    var dtmfNodeResult = _scriptAccessor.GetDTMFNodeDetails(nodeId);
                    if (!dtmfNodeResult.Success || dtmfNodeResult.Data == null)
                    {
                        await ToolResultAvailable?.Invoke($"Error: Could not find or parse DTMF configuration for node '{nodeId}'. {dtmfNodeResult.Message}");
                    }
                    else
                    {
                        var dtmfConfig = dtmfNodeResult.Data;
                        bool started = _dtmfSessionManager.StartSession(dtmfConfig, clientId);
                        if (!started)
                        {
                            _logger.LogWarning("Agent {AgentId}: Failed to start DTMF session for node {NodeId} (already active?).", _agentState.AgentId, nodeId);
                            await ToolResultAvailable?.Invoke($"Error: Could not start DTMF session for node '{nodeId}', another session seems to be active.");
                        }
                        // No immediate result sent back; result comes via DTMFSessionManager event handler
                    }
                }
                else if (toolName.Equals("press_dtmf_keypad", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: press_dtmf_keypad: string <reason>, array of char <digits to press> -> interpreted as string
                    // string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "Sending DTMF";
                    string? digitsToPress = arguments.Count > 1 ? UnescapeNullableArgument(arguments[1]) : null;

                    if (string.IsNullOrWhiteSpace(digitsToPress))
                    {
                        await ToolResultAvailable?.Invoke("Error: press_dtmf_keypad requires the digits to press as the second argument.");
                        return;
                    }

                    // Validate digits
                    if (!ValidDtmfCharsRegex.IsMatch(digitsToPress))
                    {
                        await ToolResultAvailable?.Invoke($"Error: press_dtmf_keypad received invalid characters. Only 0-9, *, #, A-D, W are allowed. Got: '{digitsToPress}'.");
                        return;
                    }

                    if (SendDTMFRequested != null)
                    {
                        await SendDTMFRequested.Invoke(digitsToPress);
                        // Acknowledge the request was sent
                        // TODO whether to acknowledge or not?
                        // maybe just add to the conversation messages history and force ai to say acknowledge
                        //await ToolResultAvailable?.Invoke($"System instruction to press DTMF digits '{digitsToPress}' was sent.");
                    }
                    else
                    {
                        await ToolResultAvailable?.Invoke($"System tool 'press_dtmf_keypad' acknowledged but no handler is configured to send DTMF.");
                    }
                }
                else if (toolName.Equals("add_script_to_context", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: add_script_to_context: string <reason>, string | null <message>, string <node id>
                    // string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "Adding context";
                    string? messageToSpeak = arguments.Count > 1 ? UnescapeNullableArgument(arguments[1]) : null;
                    string? nodeId = arguments.Count > 2 ? UnescapeNullableArgument(arguments[2]) : null;

                    if (string.IsNullOrWhiteSpace(nodeId))
                    {
                        _logger.LogError("Agent {AgentId}: Add script to context requested but Node ID is missing.", _agentState.AgentId);
                        await ToolResultAvailable?.Invoke("Error: Add script to context requires a Node ID.");
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                        await PlaySpeechRequested.Invoke(messageToSpeak, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;

                    // TODO: Need a method in ScriptAccessor to get the actual script text/content based on the node ID.
                    // var scriptContentResult = _scriptAccessor.GetScriptContextFromNode(nodeId);
                    string? scriptContent = null;// $"[Content for Script Node {nodeId} - Implementation Pending]"; // Placeholder

                    // if (!scriptContentResult.Success || string.IsNullOrWhiteSpace(scriptContentResult.Data))
                    // {
                    //     _logger.LogError("Agent {AgentId}: Failed to retrieve script content for node {NodeId}. Error: {Error}", _agentState.AgentId, nodeId, scriptContentResult.Message);
                    //     await ToolResultAvailable?.Invoke($"Error: Failed to retrieve script content for node '{nodeId}'. {scriptContentResult.Message}");
                    // }
                    // else
                    // {
                    //     string scriptContent = scriptContentResult.Data;
                    //     if (AddContextRequested != null)
                    //     {
                    //         await AddContextRequested.Invoke(scriptContent); // Pass content to Orchestrator/LLMHandler
                    //         await ToolResultAvailable?.Invoke($"Successfully added context from script node '{nodeId}'.");
                    //     } else {
                    //         _logger.LogWarning("Agent {AgentId}: AddContextRequested event has no subscribers.", _agentState.AgentId);
                    //          await ToolResultAvailable?.Invoke($"System tool 'add_script_to_context' acknowledged but no handler is configured.");
                    //     }
                    // }

                    // Using Placeholder for now:
                    if (AddContextRequested != null)
                    {
                        await AddContextRequested.Invoke(scriptContent);
                        await ToolResultAvailable?.Invoke($"Successfully added context from script node '{nodeId}'. (Placeholder Content)");
                    }
                    else
                    {
                        _logger.LogWarning("Agent {AgentId}: AddContextRequested event has no subscribers.", _agentState.AgentId);
                        await ToolResultAvailable?.Invoke($"System tool 'add_script_to_context' acknowledged but no handler is configured.");
                    }
                }
                else if (toolName.Equals("retrieve_product_information", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: retrieve_product_information: string <reason>, string <product_id>
                    // string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "Retrieving product info";
                    string productId = arguments.Count > 1 ? UnescapeArgument(arguments[1]) : "";

                    if (string.IsNullOrWhiteSpace(productId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Retrieve product information requires a Product ID.");
                        return;
                    }

                    var productData = _agentState.BusinessApp.Context.Products.Find(p => p.Id == productId);
                    if (productData == null)
                    {
                        await ToolResultAvailable?.Invoke($"Error: Product '{productId}' not found.");
                        return;
                    }

                    var builtProductMessage = $"Product: {productData.Name}\n\nShort Description: {productData.ShortDescription}\n\nLong Description: {productData.LongDescription}";
                    if (productData.AvailableAtBranches.Count > 0)
                    {
                        builtProductMessage += $"\n\nAvailable at branches with ids: {string.Join(", ", productData.AvailableAtBranches)}";
                    }
                    if (productData.OtherInformation[_agentState.CurrentLanguageCode].Count > 0)
                    {
                        builtProductMessage += $"\n\nOther Information: {string.Join(", ", productData.OtherInformation[_agentState.CurrentLanguageCode])}";
                    }

                    await ToolResultAvailable?.Invoke($"Successfully detailed information for product '{productId}'.\n\n{builtProductMessage}");

                }
                else if (toolName.Equals("retrieve_service_information", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: retrieve_service_information: string <reason>, string <service_id>
                    // string reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : "Retrieving service info";
                    string serviceId = arguments.Count > 1 ? UnescapeArgument(arguments[1]) : "";

                    if (string.IsNullOrWhiteSpace(serviceId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Retrieve service information requires a Service ID.");
                        return;
                    }

                    var serviceData = _agentState.BusinessApp.Context.Services.Find(s => s.Id == serviceId);
                    if (serviceData == null)
                    {
                        await ToolResultAvailable?.Invoke($"Error: Service '{serviceId}' not found.");
                        return;
                    }

                    var builtServiceMessage = $"Service Name: ```{serviceData.Name[_agentState.CurrentLanguageCode]}```\nService Short Description: ```{serviceData.ShortDescription[_agentState.CurrentLanguageCode]}```\nService Long Description: ```{serviceData.LongDescription[_agentState.CurrentLanguageCode]}```";
                    if (serviceData.AvailableAtBranches.Count > 0)
                    {
                        builtServiceMessage += $"\nAvailable at branches with id: ```{string.Join(", ", serviceData.AvailableAtBranches)}```";
                    }
                    if (serviceData.RelatedProducts.Count > 0)
                    {
                        builtServiceMessage += $"\nRelated products with id: ```{string.Join(", ", serviceData.RelatedProducts)}```";
                    }
                    if (serviceData.OtherInformation[_agentState.CurrentLanguageCode].Count > 0)
                    {
                        builtServiceMessage += $"\nOther Information: ```{string.Join(",\n", serviceData.OtherInformation[_agentState.CurrentLanguageCode])}\n```";
                    }

                    await ToolResultAvailable?.Invoke($"Successfully retrieved detailed information for service '{serviceId}':\n\n{builtServiceMessage}");
                }
                else if (toolName.Equals("acknowledge", StringComparison.OrdinalIgnoreCase))
                {
                    // No action needed, internal ack.
                }
                else if (toolName.Equals("send_sms", StringComparison.OrdinalIgnoreCase))
                {
                    if (arguments.Count != 4)
                    {
                        await ToolResultAvailable?.Invoke($"Error: Send SMS requires 4 arguments but you provided {arguments.Count}. Format: send_sms: string <reason for sending>, string <message to send>, string <to number or current_caller>, string <node id>");
                        return;
                    }

                    string? reason = arguments.Count > 0 ? UnescapeArgument(arguments[0]) : null;
                    string? message = arguments.Count > 1 ? UnescapeArgument(arguments[1]) : null;

                    if (string.IsNullOrEmpty(message))
                    {
                        await ToolResultAvailable?.Invoke("Error: Send SMS requires a message.");
                        return;
                    }

                    string? toNumber = arguments.Count > 2 ? UnescapeArgument(arguments[2]) : null;
                    if (string.IsNullOrEmpty(toNumber))
                    {
                        await ToolResultAvailable?.Invoke("Error: Send SMS requires a to number which can either be in format E.164 (+[country code][phone number]) or if current calling number, then use 'current_caller'.");
                        return;
                    }

                    if (toNumber == "current_caller")
                    {
                        if (_conversationSession.PrimaryClient is BaseTelephonyConversationClient telephonyClient)
                        {
                            toNumber = telephonyClient.CustomerPhoneNumber;
                        }
                        else
                        {
                            await ToolResultAvailable?.Invoke("Error: The primary client is not a telephony client so unable to figure out their phone number to send the sms to.");
                            return;
                        }
                    }

                    string? nodeId = arguments.Count > 3 ? UnescapeArgument(arguments[3]) : null;
                    if (string.IsNullOrEmpty(nodeId))
                    {
                        await ToolResultAvailable?.Invoke("Error: Send SMS requires a node ID.");
                        return;
                    }

                    var sendSmsNodeDetails = _scriptAccessor.GetSendSMSToolNodeDetails(nodeId);
                    if (!sendSmsNodeDetails.Success)
                    {
                        await ToolResultAvailable?.Invoke($"Error: {sendSmsNodeDetails.Message}.");
                        return;
                    }

                    var sendSmsResult = await _sendSMSToolExecutionHelper.SendMessageAsync(sendSmsNodeDetails.Data, message, toNumber, cancellationToken);
                    if (!sendSmsResult.Success)
                    {
                        await ToolResultAvailable?.Invoke($"Error: {sendSmsResult.Message}.");
                        return;
                    }

                    await ToolResultAvailable?.Invoke("Success: Successfully sent SMS.");
                }
                else
                {
                    await ToolResultAvailable?.Invoke($"Error: Unknown system tool '{toolName}'.");
                }
            }
            catch (OperationCanceledException)
            {
                await ToolResultAvailable?.Invoke($"System tool execution cancelled."); // Optionally notify LLM
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