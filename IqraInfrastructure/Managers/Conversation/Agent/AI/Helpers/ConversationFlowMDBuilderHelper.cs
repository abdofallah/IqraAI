using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Utilities;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Agent.AI.Helpers
{
    public class ConversationFlowMDBuilderHelper
    {
        private readonly BusinessAppAgentScript _script;
        private readonly string _selectedLanguageCode;
        private readonly List<BusinessAppTool> _scriptCustomTools;

        private readonly Dictionary<string, BusinessAppAgentScriptNode> _nodesMap = new();
        private readonly Dictionary<string, List<ScriptEdge>> _edgesMap = new();
        private readonly Dictionary<string, string> _nodeContentMap = new();
        private readonly Dictionary<string, string> _nodeLabelMap = new();

        public ConversationFlowMDBuilderHelper(BusinessAppAgentScript script, string languageCode, List<BusinessAppTool> scriptCustomTools)
        {
            _script = script;
            _selectedLanguageCode = languageCode;
            _scriptCustomTools = scriptCustomTools;
        }

        public string ConvertScriptToHumanReadable()
        {
            if (_script.Nodes == null || !_script.Nodes.Any())
            {
                return "No conversation flow defined.";
            }

            // === PHASE 1: PRE-PROCESSING, CACHING, AND LABELING ===

            // 1a. Build node and edge maps for quick lookups.
            foreach (var node in _script.Nodes)
            {
                _nodesMap[node.Id] = node;
                // Ensure every node has an entry in the edges map, even if it has no outgoing edges.
                if (!_edgesMap.ContainsKey(node.Id))
                {
                    _edgesMap[node.Id] = new List<ScriptEdge>();
                }
            }
            foreach (var edge in _script.Edges)
            {
                if (_edgesMap.ContainsKey(edge.SourceNodeId))
                {
                    _edgesMap[edge.SourceNodeId].Add(new ScriptEdge
                    {
                        TargetId = edge.TargetNodeId,
                        SourcePort = edge.SourceNodePortId,
                        TargetPort = edge.TargetNodePortId
                    });
                }
            }

            // 1b. Generate content and a default reference label for every single node.
            foreach (var node in _script.Nodes)
            {
                _nodeContentMap[node.Id] = GenerateNodeContent(node);
                _nodeLabelMap[node.Id] = $"Node {(node.NodeType != BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool ? node.NodeType : (node.NodeType + " " + ((BusinessAppAgentScriptSystemToolNode)node).ToolType))} ({node.Id})";
            }

            var startNode = _script.Nodes.FirstOrDefault(n => n.NodeType == BusinessAppAgentScriptNodeTypeENUM.Start);
            if (startNode == null)
            {
                return "Script has no start node.";
            }

            // 1c. Find the main entry points (children of StartNode) and give them friendlier labels.
            var startNodeChildren = GetNodeChildren(startNode.Id);
            for (int i = 0; i < startNodeChildren.Count; i++)
            {
                var childNodeId = startNodeChildren[i].TargetId;
                if (_nodeLabelMap.ContainsKey(childNodeId))
                {
                    _nodeLabelMap[childNodeId] = $"Main Scenario {i + 1}";
                }
            }


            // === PHASE 2: TRAVERSAL AND ASSEMBLY ===

            var result = new StringBuilder();
            var processedNodes = new HashSet<string>(); // Tracks nodes whose full flow has been written to the output.

            if (startNodeChildren.Count > 1)
            {
                for (int i = 0; i < startNodeChildren.Count; i++)
                {
                    var childEdge = startNodeChildren[i];
                    result.AppendLine($"## {_nodeLabelMap[childEdge.TargetId]}");
                    ProcessNodeRecursively(childEdge.TargetId, result, processedNodes, 1); // Start at depth 1
                    result.AppendLine();
                }
            }
            else if (startNodeChildren.Count == 1)
            {
                var childEdge = startNodeChildren[0];
                result.AppendLine($"# Conversation Flow: {_nodeLabelMap[childEdge.TargetId]}");
                result.AppendLine();
                ProcessNodeRecursively(childEdge.TargetId, result, processedNodes, 0);
            }

            return result.ToString().Trim();
        }

        private void ProcessNodeRecursively(string nodeId, StringBuilder result, HashSet<string> processedNodes, int depth)
        {
            var indent = new string(' ', depth * 2);

            // --- CORE LOGIC: Handle Jumps and Loops ---
            // If the full flow for this node has already been written, just add a reference and return.
            if (processedNodes.Contains(nodeId))
            {
                result.AppendLine($"{indent}--> (Flow continues at: {_nodeLabelMap[nodeId]})");
                return;
            }

            if (!_nodesMap.ContainsKey(nodeId)) return; // Should not happen in a valid script

            // Mark this node as fully processed for this output generation.
            processedNodes.Add(nodeId);
            var node = _nodesMap[nodeId];

            // Append the pre-generated content for the current node.
            result.AppendLine($"{indent}{_nodeContentMap[nodeId]}");

            var childEdges = GetNodeChildren(nodeId);

            // --- Handle Child Nodes ---

            // Special handling for nodes with distinct, named outcomes (Tools)
            if (IsMultiOutcomeTool(node, childEdges))
            {
                result.AppendLine($"{indent}possible tool result scenarios:");
                foreach (var edge in childEdges)
                {
                    string outcomeLabel = GetOutcomeLabel(edge.SourcePort, node);
                    result.AppendLine($"{indent}  scenario ({outcomeLabel}):");
                    // Recurse for the child of this specific outcome.
                    ProcessNodeRecursively(edge.TargetId, result, processedNodes, depth + 2);
                }
                return; // Children are handled, so we stop here for this node.
            }

            // Standard handling for generic branches (1 or more children from a single output port)
            if (childEdges.Count > 1)
            {
                result.AppendLine($"{indent}possible scenarios:");
                for (int i = 0; i < childEdges.Count; i++)
                {
                    result.AppendLine($"{indent}  ### Sub-flow {i + 1}");
                    ProcessNodeRecursively(childEdges[i].TargetId, result, processedNodes, depth + 2);
                }
            }
            else if (childEdges.Count == 1)
            {
                // If there's only one path forward, continue the flow linearly without extra headers.
                ProcessNodeRecursively(childEdges[0].TargetId, result, processedNodes, depth);
            }
            // If childEdges.Count is 0, this branch of the conversation ends.
        }

        /// <summary>
        /// Generates the single-line Markdown content for a given node. This is part of Phase 1.
        /// </summary>
        private string GenerateNodeContent(BusinessAppAgentScriptNode node)
        {
            switch (node.NodeType)
            {
                case BusinessAppAgentScriptNodeTypeENUM.UserQuery:
                    var userQueryNode = (BusinessAppAgentScriptUserQueryNode)node;
                    return $"customer_query: NodeId=\"{node.Id}\" CustomerQuery=\"{GetLocalizedString(userQueryNode.Query, _selectedLanguageCode, "Customer query")}\"";

                case BusinessAppAgentScriptNodeTypeENUM.AIResponse:
                    var aiResponseNode = (BusinessAppAgentScriptAIResponseNode)node;
                    return $"response_to_customer: NodeId=\"{node.Id}\" AgentResponse=\"{GetLocalizedString(aiResponseNode.Response, _selectedLanguageCode, "AI response")}\"";

                case BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool:
                    {
                        var systemToolNode = (BusinessAppAgentScriptSystemToolNode)node;

                        if (systemToolNode.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.GoToNode)
                        {
                            var goToNode = (BusinessAppAgentScriptGoToNodeToolNode)systemToolNode;
                            return $"--> Flow continues at ({_nodeLabelMap[goToNode.GoToNodeId]})";
                        }
                        else
                        {
                            return $"execute_system_function: {GetSystemToolTypeFormat(systemToolNode.ToolType, systemToolNode, _selectedLanguageCode)}";
                        }
                    }

                case BusinessAppAgentScriptNodeTypeENUM.ExecuteCustomTool:
                    var customToolNode = (BusinessAppAgentScriptCustomToolNode)node;
                    var nodeCustomTool = _scriptCustomTools.FirstOrDefault(t => t.Id == customToolNode.ToolId);
                    if (nodeCustomTool != null)
                    {
                        var variablesSchema = BusinessAppToolArgumentsToJsonSchemea.ConvertToJsonSchema(nodeCustomTool.Configuration.InputSchemea, _selectedLanguageCode, true);
                        return $"execute_custom_function: \"reason for execution\", \"message if any to speak before execution begins\", \"{node.Id}\", {variablesSchema}";
                    }
                    return $"execute_custom_function: [Error: Tool with ID {customToolNode.ToolId} not found]";

                default:
                    // We add the label to the node content itself to make referencing easier.
                    return $"[{_nodeLabelMap.GetValueOrDefault(node.Id, "Unknown Node")}] NodeType: {node.NodeType}";
            }
        }

        private List<ScriptEdge> GetNodeChildren(string nodeId) => _edgesMap.GetValueOrDefault(nodeId, new List<ScriptEdge>());

        private bool IsMultiOutcomeTool(BusinessAppAgentScriptNode node, List<ScriptEdge> edges)
        {
            if (node is BusinessAppAgentScriptDTMFInputToolNode) return true;
            if (node is BusinessAppAgentScriptSendSMSToolNode) return true;
            if (node is BusinessAppAgentScriptCustomToolNode && edges.Count > 1)
            {
                // It's a multi-outcome tool if its edges originate from different source ports.
                return edges.Select(e => e.SourcePort).Distinct().Count() > 1;
            }
            return false;
        }

        private string GetOutcomeLabel(string sourcePort, BusinessAppAgentScriptNode node)
        {
            if (node is BusinessAppAgentScriptDTMFInputToolNode dtmfNode)
            {
                if (sourcePort == "timeout") return "timeout";
                var outcome = dtmfNode.Outcomes.FirstOrDefault(o => o.PortId == sourcePort);
                return outcome != null ? GetLocalizedString(outcome.Value, _selectedLanguageCode, sourcePort) : "default";
            }
            if (node is BusinessAppAgentScriptCustomToolNode)
            {
                if (sourcePort.StartsWith("outcome-"))
                {
                    var outcomeValue = sourcePort.Replace("outcome-", "");
                    return outcomeValue == "default" ? "default" : $"Response {outcomeValue}";
                }
            }
            return sourcePort ?? "next";
        }

        private string GetSystemToolTypeFormat(BusinessAppAgentScriptNodeSystemToolTypeENUM type, BusinessAppAgentScriptSystemToolNode systemToolNode, string currentLanguage)
        {
            string nodeId = systemToolNode.Id;

            switch (type)
            {
                case BusinessAppAgentScriptNodeSystemToolTypeENUM.EndCall:
                    {
                        var endCallNode = systemToolNode as BusinessAppAgentScriptEndCallToolNode;
                        var messageToSpeak = endCallNode.Messages?[currentLanguage] ?? null;

                        string originalFormat = $"end_call: \"reason for ending the call\", \"{((!string.IsNullOrEmpty(messageToSpeak)) ? messageToSpeak : "null")}\", \"{nodeId}\"";
                        return originalFormat;
                    }
                case BusinessAppAgentScriptNodeSystemToolTypeENUM.ChangeLanguage:
                    return "change_language: \"reason for changing language\", \"true to play all list of languages if customer does not define language and false if customer defines an available language\", \"if customer defines the language that is available in this session/conversation/call\"";
                case BusinessAppAgentScriptNodeSystemToolTypeENUM.GetDTMFKeypadInput:
                    return $"recieve_dtmf_input: \"reason for requesting dtmf input\", \"response to speak before requesting dtmf input\", \"{nodeId}\"";
                case BusinessAppAgentScriptNodeSystemToolTypeENUM.PressDTMFKeypad:
                    return "press_dtmf_keypad: \"array of keypad dtmf input you would like to press, can be one or many at once\"";
                case BusinessAppAgentScriptNodeSystemToolTypeENUM.TransferToAgent:
                    return $"transfer_to_ai_agent: \"reason for transfering the call\", \"response to speak before agent transfer execution\", \"{nodeId}\"";
                case BusinessAppAgentScriptNodeSystemToolTypeENUM.TransferToHuman:
                    return $"transfer_to_human_agent: \"reason for transfering the call\", \"response to speak before agent transfer execution\", \"{nodeId}\"";
                case BusinessAppAgentScriptNodeSystemToolTypeENUM.AddScriptToContext:
                    return "add_script_to_context";
                case BusinessAppAgentScriptNodeSystemToolTypeENUM.SendSMS:
                    {
                        var sendSMSNode = systemToolNode as BusinessAppAgentScriptSendSMSToolNode;
                        var messageToSend = sendSMSNode.Messages?[currentLanguage] ?? null;
                        if (messageToSend == null) throw new Exception("Message to send is null"); // here it should never be null tho

                        return $"send_sms: \"reason for sending the message\", \"{messageToSend}\", \"phone number in E.164 format '+[country code][phone number]' or 'current_caller' if sending to the current caller without knowing their number\" \"{nodeId}\"";
                    }
                default:
                    return $"{systemToolNode.ToolType}: \"details...\", \"{nodeId}\"";
            }
        }

        private string GetLocalizedString(Dictionary<string, string> dictionary, string languageCode, string defaultValue)
        {
            if (dictionary == null) return defaultValue;
            if (dictionary.TryGetValue(languageCode, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
            var baseLanguage = languageCode.Split('-')[0];
            foreach (var key in dictionary.Keys)
            {
                if (key.StartsWith(baseLanguage) && !string.IsNullOrWhiteSpace(dictionary[key])) return dictionary[key];
            }
            if (dictionary.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue)) return enValue;
            var firstNonEmpty = dictionary.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            return firstNonEmpty ?? defaultValue;
        }

        private class ScriptEdge
        {
            public string TargetId { get; set; }
            public string SourcePort { get; set; }
            public string TargetPort { get; set; }
        }
    }

}
