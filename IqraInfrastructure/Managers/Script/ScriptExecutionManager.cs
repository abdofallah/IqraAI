using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using IqraInfrastructure.Managers.Business;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Script
{
    public class ScriptExecutionManager
    {
        private readonly ILogger<ScriptExecutionManager> _logger;
        private readonly BusinessManager _businessManager;
        private readonly Dictionary<string, Func<Dictionary<string, string>, Task<string>>> _toolHandlers;

        private long _businessId;
        private string _scriptId;
        private string _languageCode;
        private BusinessAppAgentScript? _currentScript;
        private BusinessAppAgentScriptNode? _currentNode;
        private Dictionary<string, object> _scriptContext = new();
        private bool _isScriptInitialized;

        public bool IsScriptActive => _isScriptInitialized && _currentScript != null && _currentNode != null;

        public ScriptExecutionManager(
            BusinessManager businessManager, 
            ILogger<ScriptExecutionManager> logger)
        {
            _businessManager = businessManager;
            _logger = logger;
            _toolHandlers = new Dictionary<string, Func<Dictionary<string, string>, Task<string>>>();
            
            // Register built-in tools
            RegisterDefaultTools();
        }

        public async Task LoadScriptAsync(long businessId, string scriptId, string languageCode)
        {
            _businessId = businessId;
            _scriptId = scriptId;
            _languageCode = languageCode;
            _scriptContext.Clear();
            _isScriptInitialized = false;

            try
            {
                // Get the business app
                var businessApp = await _businessManager.GetUserBusinessAppById(businessId, "LoadScriptAsync");
                if (!businessApp.Success || businessApp.Data == null)
                {
                    throw new InvalidOperationException($"Business app not found for ID {businessId}");
                }

                // Find the script
                _currentScript = businessApp.Data.Agents.SelectMany(a => a.Scripts).FirstOrDefault(s => s.Id == scriptId);
                if (_currentScript == null)
                {
                    throw new InvalidOperationException($"Script not found with ID {scriptId} in business {businessId}");
                }

                // Find the start node
                _currentNode = _currentScript.Nodes.FirstOrDefault(n => n.NodeType == BusinessAppAgentScriptNodeTypeENUM.Start);
                if (_currentNode == null)
                {
                    throw new InvalidOperationException($"Start node not found in script {scriptId}");
                }

                _isScriptInitialized = true;
                _logger.LogInformation("Loaded script {ScriptId} for business {BusinessId}", scriptId, businessId);

                // Execute the start node
                await ExecuteNodeAsync(_currentNode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading script {ScriptId} for business {BusinessId}", scriptId, businessId);
                throw;
            }
        }

        public async Task<string> ExecuteToolAsync(string toolName, Dictionary<string, string> parameters)
        {
            try
            {
                if (_toolHandlers.TryGetValue(toolName, out var handler))
                {
                    var result = await handler(parameters);
                    _logger.LogInformation("Executed tool {ToolName} with result: {Result}", toolName, result);
                    return result;
                }
                else
                {
                    var errorMessage = $"Tool {toolName} not found";
                    _logger.LogWarning(errorMessage);
                    return $"Error: {errorMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
                return $"Error: {ex.Message}";
            }
        }

        public async Task ProcessDTMFInputAsync(string digits)
        {
            if (!IsScriptActive)
            {
                _logger.LogWarning("Cannot process DTMF - no active script");
                return;
            }

            try
            {
                // Find the current node
                if (_currentNode?.NodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool)
                {
                    var systemTool = _currentNode as BusinessAppAgentScriptSystemToolNode;
                    if (systemTool?.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.GetDTMFKeypadInput)
                    {
                        var dtmfNode = systemTool as BusinessAppAgentScriptDTMFInputToolNode;
                        if (dtmfNode != null)
                        {
                            // Store DTMF input in context if variable name is provided
                            if (!string.IsNullOrEmpty(dtmfNode.VariableName))
                            {
                                _scriptContext[dtmfNode.VariableName] = digits;
                            }

                            // Find matching outcome
                            var outcome = dtmfNode.Outcomes.FirstOrDefault(o => o.Value.ContainsValue(digits));
                            if (outcome != null && !string.IsNullOrEmpty(outcome.PortId))
                            {
                                // Find the next node
                                var edge = _currentScript!.Edges.FirstOrDefault(e => 
                                    e.SourceNodeId == _currentNode!.Id && 
                                    e.SourceNodePortId == outcome.PortId);

                                if (edge != null)
                                {
                                    var nextNode = _currentScript.Nodes.FirstOrDefault(n => n.Id == edge.TargetNodeId);
                                    if (nextNode != null)
                                    {
                                        _currentNode = nextNode;
                                        await ExecuteNodeAsync(nextNode);
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

        public async Task AdvanceToNextNodeAsync(string portId)
        {
            if (!IsScriptActive)
            {
                _logger.LogWarning("Cannot advance script - no active script");
                return;
            }

            try
            {
                // Find the edge from the current node with the given port ID
                var edge = _currentScript!.Edges.FirstOrDefault(e => 
                    e.SourceNodeId == _currentNode!.Id && 
                    e.SourceNodePortId == portId);

                if (edge != null)
                {
                    // Find the target node
                    var nextNode = _currentScript.Nodes.FirstOrDefault(n => n.Id == edge.TargetNodeId);
                    if (nextNode != null)
                    {
                        _currentNode = nextNode;
                        await ExecuteNodeAsync(nextNode);
                    }
                    else
                    {
                        _logger.LogWarning("Target node {NodeId} not found in script", edge.TargetNodeId);
                    }
                }
                else
                {
                    _logger.LogWarning("No edge found from node {NodeId} with port {PortId}", _currentNode!.Id, portId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error advancing to next node with port {PortId}", portId);
            }
        }

        private async Task ExecuteNodeAsync(BusinessAppAgentScriptNode node)
        {
            try
            {
                _logger.LogInformation("Executing script node {NodeId} of type {NodeType}", node.Id, node.NodeType);

                switch (node.NodeType)
                {
                    case BusinessAppAgentScriptNodeTypeENUM.Start:
                        // Find the first connected node
                        var startEdge = _currentScript!.Edges.FirstOrDefault(e => e.SourceNodeId == node.Id);
                        if (startEdge != null)
                        {
                            var nextNode = _currentScript.Nodes.FirstOrDefault(n => n.Id == startEdge.TargetNodeId);
                            if (nextNode != null)
                            {
                                _currentNode = nextNode;
                                await ExecuteNodeAsync(nextNode);
                            }
                        }
                        break;

                    case BusinessAppAgentScriptNodeTypeENUM.AIResponse:
                        var aiNode = node as BusinessAppAgentScriptAIResponseNode;
                        if (aiNode != null)
                        {
                            // Get localized response
                            var response = GetLocalizedString(aiNode.Response, _languageCode, "");
                            
                            // TODO: Emit this response through event mechanism
                            _logger.LogInformation("AI Response: {Response}", response);
                            
                            // Auto-advance to the next node if there's only one connection
                            AutoAdvanceToNextNode(node);
                        }
                        break;

                    case BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool:
                        var systemToolNode = node as BusinessAppAgentScriptSystemToolNode;
                        if (systemToolNode != null)
                        {
                            await ExecuteSystemToolNodeAsync(systemToolNode);
                            // Note: System tool nodes handle their own navigation
                        }
                        break;

                    case BusinessAppAgentScriptNodeTypeENUM.ExecuteCustomTool:
                        var customToolNode = node as BusinessAppAgentScriptCustomToolNode;
                        if (customToolNode != null)
                        {
                            // Convert tool configuration to parameters
                            var parameters = customToolNode.ToolConfiguration.ToDictionary(
                                kvp => kvp.Key,
                                kvp => ResolveContextVariables(kvp.Value));
                            
                            // Execute the tool
                            var result = await ExecuteToolAsync(customToolNode.ToolId, parameters);
                            
                            // Store result in context
                            _scriptContext["lastToolResult"] = result;
                            
                            // Auto-advance to the next node if there's only one connection
                            AutoAdvanceToNextNode(node);
                        }
                        break;

                    case BusinessAppAgentScriptNodeTypeENUM.UserQuery:
                        // User query nodes don't execute automatically - they wait for user input
                        _logger.LogInformation("Waiting for user input at node {NodeId}", node.Id);
                        break;

                    default:
                        _logger.LogWarning("Unsupported node type: {NodeType}", node.NodeType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing node {NodeId}", node.Id);
            }
        }

        private async Task ExecuteSystemToolNodeAsync(BusinessAppAgentScriptSystemToolNode node)
        {
            try
            {
                switch (node.ToolType)
                {
                    case BusinessAppAgentScriptNodeSystemToolTypeENUM.EndCall:
                        var endCallNode = node as BusinessAppAgentScriptEndCallToolNode;
                        if (endCallNode != null)
                        {
                            var message = GetLocalizedString(endCallNode.Messages, _languageCode, "Thank you for calling.");
                            
                            // TODO: Emit an event to end the call
                            _logger.LogInformation("End call triggered with message: {Message}", message);
                        }
                        break;

                    case BusinessAppAgentScriptNodeSystemToolTypeENUM.GetDTMFKeypadInput:
                        // DTMF input nodes don't execute automatically - they wait for DTMF input
                        _logger.LogInformation("Waiting for DTMF input at node {NodeId}", node.Id);
                        break;

                    case BusinessAppAgentScriptNodeSystemToolTypeENUM.TransferToAgent:
                        var transferNode = node as BusinessAppAgentScriptTransferToAgentToolNode;
                        if (transferNode != null)
                        {
                            // TODO: Emit an event to transfer the call
                            _logger.LogInformation("Transfer to agent: {AgentId}", transferNode.AgentId);
                        }
                        break;

                    case BusinessAppAgentScriptNodeSystemToolTypeENUM.AddScriptToContext:
                        var scriptNode = node as BusinessAppAgentScriptAddScriptToContextToolNode;
                        if (scriptNode != null)
                        {
                            // Load the referenced script
                            await LoadScriptAsync(_businessId, scriptNode.ScriptId, _languageCode);
                        }
                        break;

                    default:
                        _logger.LogWarning("Unsupported system tool type: {ToolType}", node.ToolType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing system tool node {NodeId}", node.Id);
            }
        }

        private void AutoAdvanceToNextNode(BusinessAppAgentScriptNode node)
        {
            // Count outgoing edges
            var edges = _currentScript!.Edges.Where(e => e.SourceNodeId == node.Id).ToList();
            
            // If there's exactly one edge, auto-advance
            if (edges.Count == 1)
            {
                AdvanceToNextNodeAsync(edges[0].SourceNodePortId).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Error auto-advancing to next node");
                    }
                });
            }
        }

        private string ResolveContextVariables(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Replace {{variableName}} with value from context
            foreach (var key in _scriptContext.Keys)
            {
                text = text.Replace($"{{{{{key}}}}}", _scriptContext[key]?.ToString() ?? "");
            }

            return text;
        }

        private void RegisterDefaultTools()
        {
            // Example built-in tool handlers
            _toolHandlers["getCurrentTime"] = async (parameters) =>
            {
                var format = parameters.GetValueOrDefault("format", "g");
                return DateTime.Now.ToString(format);
            };

            _toolHandlers["calculateValue"] = async (parameters) =>
            {
                if (!parameters.TryGetValue("expression", out var expression))
                {
                    return "Error: Missing expression parameter";
                }

                try
                {
                    // Simple evaluation using DataTable (for production use a proper expression evaluator)
                    var table = new System.Data.DataTable();
                    var result = table.Compute(expression, "");
                    return result.ToString() ?? "Error";
                }
                catch (Exception ex)
                {
                    return $"Error calculating: {ex.Message}";
                }
            };

            // Additional tools would be registered here or via a separate method
        }

        private string GetLocalizedString(Dictionary<string, string>? dictionary, string languageCode, string defaultValue)
        {
            if (dictionary == null || !dictionary.Any()) return defaultValue;

            // Try exact match
            if (dictionary.TryGetValue(languageCode, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;

            // Try language without region
            var baseLanguage = languageCode.Split('-')[0];
            foreach (var key in dictionary.Keys)
            {
                if (key.StartsWith(baseLanguage) && !string.IsNullOrWhiteSpace(dictionary[key]))
                    return dictionary[key];
            }

            // Try English as fallback
            if (dictionary.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue))
                return enValue;

            // Return any available value
            var firstNonEmpty = dictionary.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            return firstNonEmpty ?? defaultValue;
        }
    }
}