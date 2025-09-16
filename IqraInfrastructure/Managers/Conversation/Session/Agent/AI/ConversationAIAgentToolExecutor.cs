using Google.Api;
using IqraCore.Entities.Conversation.Turn;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Conversation.Session.Client.Telephony;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebSocketSharp;


namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentToolExecutor
    {
        public event EventHandler<ConversationTurn>? TurnUpdate;

        public event Func<ConversationTurn, Task>? ToolResultAvailable;
        public event Func<ConversationTurn, string, CancellationToken, Task>? PlaySpeechRequested;
        public event Func<ConversationTurn, Task>? EndConversationRequested;
        public event Func<ConversationTurn, Task>? TransferToAIAgentRequested;
        public event Func<ConversationTurn, Task>? TransferToHumanAgentRequested;
        public event Func<List<char>, Task>? SendDTMFRequested;
        public event Func<string, Task>? AddContextRequested;
        public event Func<string, Task>? ChangeLanguageRequested;
        public event Func<Task>? InitiateLanguageSelectionRequested;

        private readonly ILogger<ConversationAIAgentToolExecutor> _logger;
        private readonly ConversationSessionOrchestrator _conversationSession;
        private readonly ConversationAIAgentState _agentState;
        private readonly ScriptExecutionManager _scriptAccessor;
        private readonly CustomToolExecutionHelper _customToolHelper;
        private readonly ConversationAIAgentDTMFSessionManager _dtmfSessionManager;
        private readonly SendSMSToolExecutionHelper _sendSMSToolExecutionHelper;

        // Static Regex for basic DTMF validation (0-9, *, #, A-D, W - W for pause if needed)
        private static readonly Regex ValidDtmfCharsRegex = new Regex(@"^[0-9*#A-DW\s,""\[\]]+$", RegexOptions.Compiled);
        private static readonly Regex DtmfSanitizerRegex = new Regex(@"[^0-9*#A-DW]", RegexOptions.Compiled);

        public ConversationAIAgentToolExecutor(
            ILoggerFactory loggerFactory,
            ConversationSessionOrchestrator conversationSession,
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

        public async Task HandleSystemToolAsync(ConversationTurn turn, CancellationToken cancellationToken)
        {
            var turnToolExecutionData = turn.Response.ToolExecution;
            string functionContent = turnToolExecutionData.RawLLMInput;          

            try
            {
                string toolName;
                string argsRaw = string.Empty;
                string prefix = "execute_system_function:";
                int colonIndex = functionContent.IndexOf(':');

                if (colonIndex > 0)
                {
                    toolName = functionContent.Substring(prefix.Length, colonIndex - prefix.Length).Trim();
                    argsRaw = functionContent.Substring(colonIndex + 1).Trim();
                }
                else
                {
                    await FinalizeAndReportToolResult(
                        turn,
                        false,
                        $"Error: Invalid system tool format '{functionContent}'. Expected 'tool_name: args'."
                    );
                    return;
                }
        
                List<string> arguments = ParseArguments(argsRaw);

                turnToolExecutionData.ToolName = toolName;
                TurnUpdate?.Invoke(this, turn);

                // Tool Specific Logic
                if (toolName.Equals("end_call", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: end_call: string <reason>, string | null <message>, string | null <node_id>
                    if (arguments.Count < 3)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: System Tool End Call format requires 3 arguments: string <reason>, string | null <message>, string | null <node_id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string? messageToSpeak = UnescapeNullableArgument(arguments[1]);
                    string? nodeId = UnescapeNullableArgument(arguments[2]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                    {
                        await PlaySpeechRequested.Invoke(turn, messageToSpeak, cancellationToken);
                    }

                    EndConversationRequested?.Invoke(turn);

                    // RESULT HANDLED BY ORCHESTRATOR
                }
                else if (toolName.Equals("change_language", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: change_language: string <reason>, boolean <play list>, string | null <language code>, string | null <node_id>
                    if (arguments.Count < 3)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: System Tool Change Language format requires 4 arguments: string <reason>, boolean <play list>, string | null <language code>, string | null <node_id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string playListBoolString = UnescapeArgument(arguments[1]);
                    string? targetLanguageCode = UnescapeNullableArgument(arguments[2]);
                    string? nodeId = UnescapeNullableArgument(arguments[3]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (!bool.TryParse(playListBoolString, out bool playList))
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: System Tool Change Language format requires boolean for play list argument. Unable to parse string to boolean."
                        );
                        return;
                    }

                    if (playList)
                    {
                        await InitiateLanguageSelectionRequested?.Invoke();

                        // RESULT HANDLED BY ORCHESTRATOR
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(targetLanguageCode))
                        {
                            await FinalizeAndReportToolResult(
                                turn,
                                false,
                                "Error: System Tool Change Language requires a target language code when play list argument is false. Found empty or null."
                            );
                            return;
                        }

                        await ChangeLanguageRequested?.Invoke(targetLanguageCode);

                        // RESULT HANDLED BY ORCHESTRATOR
                    }
                }
                else if (toolName.Equals("transfer_to_ai_agent", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: transfer_to_ai_agent: string <reason>, string | null <message>, string <node_id>
                    if (arguments.Count < 3)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: System Tool Transfer To AI Agent format requires 3 arguments: string <reason>, string | null <message>, string <node_id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string? messageToSpeak = UnescapeNullableArgument(arguments[1]);
                    string nodeId = UnescapeArgument(arguments[2]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                    {
                        await PlaySpeechRequested.Invoke(turn, messageToSpeak, cancellationToken);
                    }

                    await TransferToAIAgentRequested?.Invoke(turn);

                    // RESULT HANDLED BY ORCHESTRATOR
                }
                else if (toolName.Equals("transfer_to_human_agent", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: transfer_to_human_agent: string <reason>, string | null <message>, string <node_id>
                    if (arguments.Count < 3)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: System Tool Transfer To Human Agent format requires 3 arguments: string <reason>, string | null <message>, string <node_id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string? messageToSpeak = UnescapeNullableArgument(arguments[1]);
                    string nodeId = UnescapeArgument(arguments[2]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                    {
                        await PlaySpeechRequested.Invoke(turn, messageToSpeak, cancellationToken);
                    }

                    await TransferToHumanAgentRequested?.Invoke(turn);

                    // RESULT HANDLED BY ORCHESTRATOR
                }
                else if (toolName.Equals("recieve_dtmf_input", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: recieve_dtmf_input: string <reason>, string | null <message>, string <node_id>
                    if (arguments.Count < 3)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: System Tool Receive DTMF Input format requires 3 arguments: string <reason>, string | null <message>, string <node_id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string? messageToSpeak = UnescapeNullableArgument(arguments[1]);
                    string nodeId = UnescapeArgument(arguments[2]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                    {
                        await PlaySpeechRequested.Invoke(turn, messageToSpeak, cancellationToken);
                    }

                    var dtmfNodeResult = _scriptAccessor.GetDTMFNodeDetails(nodeId);
                    if (!dtmfNodeResult.Success || dtmfNodeResult.Data == null)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            $"Error: Could not find or parse DTMF configuration for node '{nodeId}'. {dtmfNodeResult.Message}"
                        );
                        return;
                    }

                    var dtmfConfig = dtmfNodeResult.Data;
                    bool started = _dtmfSessionManager.StartSession(dtmfConfig, turn);
                    if (!started)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            $"Error: Could not start DTMF session for node '{nodeId}'. {dtmfNodeResult.Message}"
                        );
                        return;
                    }

                    // RESULT HANDLED BY DTMF SESSION MANAGER
                }
                else if (toolName.Equals("press_dtmf_keypad", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: press_dtmf_keypad: string <reason>, string <digits to press>, string | null <node_id>
                    if (arguments.Count < 3)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: System Tool Press DTMF Keypad format requires 3 arguments: string <reason>, string <digits to press>, string | null <node_id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string digitsToPress = UnescapeArgument(arguments[1]);
                    string? nodeId = UnescapeNullableArgument(arguments[2]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (!ValidDtmfCharsRegex.IsMatch(digitsToPress))
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            $"Error: Invalid characters in string to press. Input must only contain 0-9, *, #, A-D, W and basic formatting (spaces, quotes, commas, brackets). Raw input was: '{digitsToPress}'."
                        );
                        return;
                    }

                    string finalDigits = DtmfSanitizerRegex.Replace(digitsToPress, "");
                    if (string.IsNullOrEmpty(finalDigits))
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            $"Error: Invalid or empty characters in string to press after sanitizing. Only 0-9, *, #, A-D, W are allowed. Raw input was: '{digitsToPress}'."
                        );
                        return;
                    }

                    await SendDTMFRequested?.Invoke(finalDigits.ToCharArray().ToList());

                    // RESULT HANDLED BY DTMF SESSION MANAGER
                }
                else if (toolName.Equals("add_script_to_context", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: add_script_to_context: string <reason>, string | null <message>, string <node id>
                    if (arguments.Count < 3)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: Add script to context requires 3 arguments: string <reason>, string | null <message>, string <node_id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string? messageToSpeak = UnescapeNullableArgument(arguments[1]);
                    string nodeId = UnescapeArgument(arguments[2]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                    {
                        await PlaySpeechRequested.Invoke(turn, messageToSpeak, cancellationToken);
                    }

                    // TODO IMPLEMENT THIS, FALL BACK AS NOT IMPLEMENTED SENT FOR NOW
                    await FinalizeAndReportToolResult(
                        turn,
                        false,
                        "Error: Add script to context is not yet implemented. Do not try to execute this tool."
                    );
                    return;
                }
                else if (toolName.Equals("retrieve_product_information", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: retrieve_product_information: string <reason>, string <product_id>, string | null <node_id>
                    if (arguments.Count < 3)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: Retrieve product information requires 3 arguments: string <reason>, string <product_id>, string | null <node_id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string productId = UnescapeArgument(arguments[1]);
                    string? nodeId = UnescapeNullableArgument(arguments[2]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (string.IsNullOrWhiteSpace(productId))
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: Retrieve product information requires a Product ID. Found null or empty."
                        );
                        return;
                    }

                    var productData = _agentState.BusinessApp.Context.Products.Find(p => p.Id == productId);
                    if (productData == null)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            $"Error: Product with id '{productId}' not found in business context products list."
                        );
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

                    await FinalizeAndReportToolResult(
                        turn,
                        true,
                        $"Successfull Result: Here is detailed information for the product with id '{productId}'.\n\n{builtProductMessage}"
                    );
                    return;
                }
                else if (toolName.Equals("retrieve_service_information", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: retrieve_service_information: string <reason>, string <service_id>, string | null <node_id>
                    if (arguments.Count < 3)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: Retrieve service information requires 3 arguments: string <reason>, string <service_id>, string | null <node_id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string serviceId = UnescapeArgument(arguments[1]);
                    string? nodeId = UnescapeNullableArgument(arguments[2]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (string.IsNullOrWhiteSpace(serviceId))
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: Retrieve service information requires a Service ID. Found null or empty."
                        );
                        return;
                    }

                    var serviceData = _agentState.BusinessApp.Context.Services.Find(s => s.Id == serviceId);
                    if (serviceData == null)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            $"Error: Service with id '{serviceId}' not found in business context services list."
                        );
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

                    await FinalizeAndReportToolResult(
                        turn,
                        true,
                        $"Successfull Result: Here is detailed information for the service with id '{serviceId}':\n\n{builtServiceMessage}"
                    );
                    return;
                }
                else if (toolName.Equals("acknowledge", StringComparison.OrdinalIgnoreCase))
                {
                    // No action needed, internal ack.
                }
                else if (toolName.Equals("send_sms", StringComparison.OrdinalIgnoreCase))
                {
                    if (arguments.Count != 4)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            "Error: Send SMS requires 4 arguments: string <reason for sending>, string <message to send>, string <to number or current_caller>, string <node id>"
                        );
                        return;
                    }

                    string reason = UnescapeArgument(arguments[0]);
                    string message = UnescapeArgument(arguments[1]);
                    string toNumber = UnescapeArgument(arguments[2]);
                    string nodeId = UnescapeArgument(arguments[3]);

                    turnToolExecutionData.ReasonForExecution = reason;
                    turnToolExecutionData.NodeId = nodeId;
                    TurnUpdate?.Invoke(this, turn);

                    if (toNumber == "current_caller")
                    {
                        if (_conversationSession.PrimaryClient is BaseTelephonyConversationClient telephonyClient && !string.IsNullOrEmpty(telephonyClient.CustomerPhoneNumber))
                        {
                            toNumber = telephonyClient.CustomerPhoneNumber;
                        }
                        else
                        {
                            await FinalizeAndReportToolResult(
                                turn,
                                false,
                                "Error: The primary client is not a telephony client so unable to figure out their phone number to send the sms to. Manually ask the user to provide the phone number."
                            );
                            return;
                        }
                    }

                    var sendSmsNodeDetails = _scriptAccessor.GetSendSMSToolNodeDetails(nodeId);
                    if (!sendSmsNodeDetails.Success)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            $"Error: Failed to find node details for node id '{nodeId}'. {sendSmsNodeDetails.Message}"
                        );
                        return;
                    }

                    var sendSmsResult = await _sendSMSToolExecutionHelper.SendMessageAsync(sendSmsNodeDetails.Data, message, toNumber, cancellationToken);
                    if (!sendSmsResult.Success)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            $"Error: Failed to send SMS. {sendSmsResult.Message}"
                        );
                        return;
                    }

                    await FinalizeAndReportToolResult(
                        turn,
                        true,
                        $"Success: Successfully sent SMS."
                    );
                    return;
                }
                else // UNKNOWN TOOL YO
                {
                    await FinalizeAndReportToolResult(
                        turn,
                        false,
                        $"Error: Unknown system tool '{toolName}'."
                    );
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                await FinalizeAndReportToolResult(
                    turn,
                    false,
                    $"System tool execution cancelled."
                );
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing system tool for turn {TurnId}", turn.Id);

                await FinalizeAndReportToolResult(
                    turn,
                    false,
                    $"Error executing system tool: {ex.Message}"
                );
                return;
            }
        }

        public async Task HandleCustomToolAsync(ConversationTurn turn, CancellationToken cancellationToken)
        {
            string functionContent = turn.Response.ToolExecution.RawLLMInput;
            var toolExecutionData = turn.Response.ToolExecution;

            try
            {
                // Format: execute_custom_function: string <reason>, string | null <message>, string <node_id>, Dictionary | null <vars>
                List<string> arguments = ParseArguments(functionContent.Substring("execute_custom_function:".Length).Trim());

                if (arguments.Count < 3) // Need at least reason, message (even if null), and node_id
                {
                    await FinalizeAndReportToolResult(
                        turn,
                        false,
                        "Error: Custom Tool requires 4 arguments: string <reason for sending>, string | null <message to play>, string <node id>, Dictionary | null <node variable values>"
                    );
                    return;
                }

                string reasonForExecuting = UnescapeArgument(arguments[0]);
                string? messageToSpeak = UnescapeNullableArgument(arguments[1]);
                string nodeIdToExecute = UnescapeArgument(arguments[2]);
                string? nodeVariableValuesJsonString = arguments[3];

                toolExecutionData.ReasonForExecution = reasonForExecuting;
                toolExecutionData.NodeId = nodeIdToExecute;
                TurnUpdate?.Invoke(this, turn);

                var nodeDetailsResult = _scriptAccessor.GetCustomToolNodeDetails(nodeIdToExecute);
                if (!nodeDetailsResult.Success || nodeDetailsResult.Data == null)
                {
                    await FinalizeAndReportToolResult(
                        turn,
                        false,
                        $"Error: Could not find node with ID '{nodeIdToExecute}' for custom tool exection. {nodeDetailsResult.Message}"
                    );
                    return;
                }

                var toolData = nodeDetailsResult.Data;
                toolExecutionData.ToolName = toolData.General.Name[_conversationSession.BusinessData.DefaultLanguage];
                TurnUpdate?.Invoke(this, turn);

                // Parse Variables
                Dictionary<string, JsonElement>? nodeVariables = null;
                if (!string.IsNullOrEmpty(nodeVariableValuesJsonString) && !nodeVariableValuesJsonString.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Trim outer quotes if ParseArguments included them (it shouldn't for valid JSON object)
                        var trimmedJson = nodeVariableValuesJsonString.Trim();
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
                                trimmedJson = nodeVariableValuesJsonString.Substring(1, nodeVariableValuesJsonString.Length - 2).Replace("\\\"", "\"");
                            }
                        }

                        if (trimmedJson.StartsWith("{") && trimmedJson.EndsWith("}"))
                        {
                            nodeVariables = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(trimmedJson);
                        }
                        else
                        {
                            await FinalizeAndReportToolResult(
                                turn,
                                false,
                                $"Error: Variables provided for custom tool node '{nodeIdToExecute}' were not a valid JSON object."
                            );
                            return;
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        await FinalizeAndReportToolResult(
                            turn,
                            false,
                            $"Error: Could not parse JSON variables for custom tool node '{nodeIdToExecute}'. Malformed JSON."
                        );
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(messageToSpeak) && PlaySpeechRequested != null)
                {
                    await PlaySpeechRequested.Invoke(turn, messageToSpeak, cancellationToken);
                }

                // Execute the Custom Tool via Helper
                var executeResult = await _customToolHelper.ExecuteHttpRequestForToolAsync(
                    toolData,
                    nodeVariables ?? new Dictionary<string, JsonElement>(),
                    cancellationToken
                );

                toolExecutionData.WasSuccessful = executeResult.Success;
                if (!executeResult.Success)
                {
                    await FinalizeAndReportToolResult(
                        turn,
                        true,
                        $"Successfull Execution of Custom Tool{(string.IsNullOrEmpty(executeResult.Message) ? "" : $": {executeResult.Message}")}"
                    );
                    return;
                }
                else
                {
                    await FinalizeAndReportToolResult(
                        turn,
                        false,
                        $"Failed Execution of Custom Tool{(string.IsNullOrEmpty(executeResult.Message) ? "" : $": {executeResult.Message}")}"
                    );
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                await FinalizeAndReportToolResult(
                    turn,
                    false,
                    "Custom tool execution cancelled."
                );
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing custom tool for turn {TurnId}", turn.Id);

                await FinalizeAndReportToolResult(
                    turn,
                    false,
                    $"Unexpected error executing custom tool: {ex.Message}"
                );
                return;
            }
        }

        public async Task FinalizeAndReportToolResult(ConversationTurn turn, bool wasSuccessful, string? result)
        {
            turn.Response.ToolExecution!.WasSuccessful = wasSuccessful;
            turn.Response.ToolExecution!.Result = result;
            turn.Response.ToolExecution!.CompletedAt = DateTime.UtcNow;

            await ToolResultAvailable?.Invoke(turn);
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