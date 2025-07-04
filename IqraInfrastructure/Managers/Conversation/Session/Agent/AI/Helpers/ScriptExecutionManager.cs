using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Context;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers
{
    public class ScriptExecutionManager
    {
        private readonly ILogger<ScriptExecutionManager> _logger;

        // Keep state needed to access script data
        private BusinessApp? _businessApp;
        private ConversationSessionContext? _currentSessionContext;
        private BusinessAppAgent? _currentRouteAgent;
        private BusinessAppAgentScript? _curentAgentActiveScript;
        private string? _currentSessionlanguageCode;
        private bool _isScriptInitialized;

        public bool IsScriptActive => _isScriptInitialized && _curentAgentActiveScript != null;
        public BusinessAppAgentScript? ActiveScript => _curentAgentActiveScript;

        public ScriptExecutionManager(
            ILogger<ScriptExecutionManager> logger
        )
        {
            _logger = logger;
        }

        // LoadScriptAsync remains largely the same
        public Task LoadScriptAsync(BusinessApp businessApp, ConversationSessionContext currentSessionContext, string languageCode)
        {
            _businessApp = businessApp;
            _currentSessionContext = currentSessionContext;
            _currentSessionlanguageCode = languageCode;
            _isScriptInitialized = false;

            var currentRouteAgentId = _currentSessionContext.Agent.SelectedAgentId;
            var routeAgentScriptId = _currentSessionContext.Agent.OpeningScriptId; // Assuming opening script is the main one for now

            try
            {
                if (businessApp?.Agents == null) throw new InvalidOperationException("BusinessApp Agents collection is null.");
                _currentRouteAgent = businessApp.Agents.FirstOrDefault(a => a.Id == currentRouteAgentId);
                if (_currentRouteAgent == null)
                {
                    throw new InvalidOperationException($"Agent not found with ID {currentRouteAgentId} in business {businessApp.Id}");
                }

                if (_currentRouteAgent.Scripts == null) throw new InvalidOperationException($"Agent {currentRouteAgentId} Scripts collection is null.");
                _curentAgentActiveScript = _currentRouteAgent.Scripts.FirstOrDefault(s => s.Id == routeAgentScriptId);
                if (_curentAgentActiveScript == null)
                {
                    // Optional: Log available scripts if needed for debugging
                    // var availableScripts = string.Join(", ", _currentRouteAgent.Scripts.Select(s => s.Id));
                    // _logger.LogWarning("Available scripts for agent {AgentId}: {Scripts}", currentRouteAgentId, availableScripts);
                    throw new InvalidOperationException($"Script not found with ID {routeAgentScriptId} for agent {currentRouteAgentId} in business {businessApp.Id}");
                }

                if (_curentAgentActiveScript.Nodes == null) _curentAgentActiveScript.Nodes = new List<BusinessAppAgentScriptNode>(); // Ensure nodes list exists
                if (_curentAgentActiveScript.Edges == null) _curentAgentActiveScript.Edges = new List<BusinessAppAgentScriptEdge>(); // Ensure edges list exists
                if (_businessApp.Tools == null) _businessApp.Tools = new List<BusinessAppTool>(); // Ensure tools list exists


                _isScriptInitialized = true;
                _logger.LogInformation("Script data loaded successfully. Script ID: {ScriptId}", _curentAgentActiveScript.Id);
                return Task.CompletedTask; // Make method async Task if future awaits needed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading agent {AgentId} script {ScriptId} for business {BusinessId}", currentRouteAgentId, routeAgentScriptId, _businessApp?.Id);
                _isScriptInitialized = false; // Ensure state reflects failure
                throw; // Re-throw to signal failure
            }
        }


        // Get DTMF Node Configuration
        public FunctionReturnResult<DTMFSessionConfig> GetDTMFNodeDetails(string nodeId)
        {
            var result = new FunctionReturnResult<DTMFSessionConfig>();
            if (!IsScriptActive || _curentAgentActiveScript?.Nodes == null)
            {
                result.Code = "GetDTMFNode:1";
                result.Message = "Script is not active or loaded.";
                _logger.LogWarning(result.Message);
                return result;
            }

            _logger.LogDebug("Searching for DTMF node with ID: {NodeId}", nodeId);
            var nodeData = _curentAgentActiveScript.Nodes.FirstOrDefault(n => n.Id == nodeId);

            if (nodeData == null)
            {
                result.Code = "GetDTMFNode:2";
                result.Message = $"Node with id {nodeId} not found in active script.";
                _logger.LogWarning(result.Message);
                return result;
            }

            if (nodeData.NodeType != BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool ||
                !(nodeData is BusinessAppAgentScriptSystemToolNode systemToolNode) ||
                systemToolNode.ToolType != BusinessAppAgentScriptNodeSystemToolTypeENUM.GetDTMFKeypadInput ||
                !(systemToolNode is BusinessAppAgentScriptDTMFInputToolNode dtmfNode))
            {
                result.Code = "GetDTMFNode:3";
                result.Message = $"Node {nodeId} is not a valid 'GetDTMFKeypadInput' system tool node. Actual type: {nodeData.NodeType}";
                _logger.LogWarning(result.Message);
                return result;
            }

            _logger.LogDebug("Found DTMF node {NodeId}. Extracting configuration.", nodeId);

            result.Data = new DTMFSessionConfig
            {
                AssociatedNodeId = dtmfNode.Id,
                MaxLength = dtmfNode.MaxLength,
                MaxSessionDurationSeconds = 10, //dtmfNode.MaxTimeSeconds, // todo make configurable
                InterDigitTimeoutSeconds = dtmfNode.Timeout,
                TerminatorChar = dtmfNode.RequireEndHash == true ? '#' : null,
                StartChar = dtmfNode.RequireStartAsterisk ? '*' : null,
                IsEncrypted = dtmfNode.EncryptInput,
                SaveEncryptedToVariable = dtmfNode.EncryptInput ? dtmfNode.VariableName : null
            };

            result.Success = true;
            return result;
        }

        // Get Send SMS Tool Node Details
        public FunctionReturnResult<BusinessAppAgentScriptSendSMSToolNode> GetSendSMSToolNodeDetails(string nodeId)
        {
            var result = new FunctionReturnResult<BusinessAppAgentScriptSendSMSToolNode>();
            if (!IsScriptActive || _curentAgentActiveScript?.Nodes == null)
            {
                result.Code = "GetSendSMSToolNode:1";
                result.Message = "Script is not active or loaded.";
                _logger.LogWarning(result.Message);
                return result;
            }

            _logger.LogDebug("Searching for Send SMS node with ID: {NodeId}", nodeId);
            var nodeData = _curentAgentActiveScript.Nodes.FirstOrDefault(n => n.Id == nodeId);

            if (nodeData == null)
            {
                result.Code = "GetSendSMSToolNode:2";
                result.Message = $"Node with id {nodeId} not found in active script.";
                _logger.LogWarning(result.Message);
                return result;
            }

            if (nodeData.NodeType != BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool ||
                !(nodeData is BusinessAppAgentScriptSystemToolNode systemToolNode) ||
                systemToolNode.ToolType != BusinessAppAgentScriptNodeSystemToolTypeENUM.SendSMS ||
                !(systemToolNode is BusinessAppAgentScriptSendSMSToolNode smsNode))
            {
                result.Code = "GetSendSMSToolNode:3";
                result.Message = $"Node {nodeId} is not a valid 'SendSMS' system tool node. Actual type: {nodeData.NodeType}";
                _logger.LogWarning(result.Message);
                return result;
            }

            _logger.LogDebug("Found Send SMS node {NodeId}. Extracting configuration.", nodeId);

            result.Data = smsNode;
            result.Success = true;
            return result;
        }

        // Get Custom Tool Node Details (resolves Node ID to Tool Definition)
        public FunctionReturnResult<BusinessAppTool> GetCustomToolNodeDetails(string nodeId)
        {
            var result = new FunctionReturnResult<BusinessAppTool>();
            if (!IsScriptActive || _curentAgentActiveScript?.Nodes == null || _businessApp?.Tools == null)
            {
                result.Code = "GetCustomToolNode:1";
                result.Message = "Script or Business Tools are not active or loaded.";
                _logger.LogWarning(result.Message);
                return result;
            }

            _logger.LogDebug("Searching for Custom Tool node with ID: {NodeId}", nodeId);
            var nodeData = _curentAgentActiveScript.Nodes.FirstOrDefault(n => n.Id == nodeId);

            if (nodeData == null)
            {
                result.Code = "GetCustomToolNode:2";
                result.Message = $"Node with id {nodeId} not found in active script.";
                _logger.LogWarning(result.Message);
                return result;
            }

            if (nodeData.NodeType != BusinessAppAgentScriptNodeTypeENUM.ExecuteCustomTool ||
                !(nodeData is BusinessAppAgentScriptCustomToolNode customToolNode))
            {
                result.Code = "GetCustomToolNode:3";
                result.Message = $"Node {nodeId} is not an 'ExecuteCustomTool' node. Actual type: {nodeData.NodeType}";
                _logger.LogWarning(result.Message);
                return result;
            }

            if (string.IsNullOrEmpty(customToolNode.ToolId))
            {
                result.Code = "GetCustomToolNode:4";
                result.Message = $"Custom tool node {nodeId} does not have a ToolId defined.";
                _logger.LogWarning(result.Message);
                return result;
            }

            _logger.LogDebug("Found Custom Tool node {NodeId}. Looking for Tool definition with ID: {ToolId}", nodeId, customToolNode.ToolId);
            BusinessAppTool? toolData = _businessApp.Tools.Find(t => t.Id == customToolNode.ToolId);
            if (toolData == null)
            {
                result.Code = "GetCustomToolNode:5";
                result.Message = $"Tool definition with id {customToolNode.ToolId} (referenced by node {nodeId}) not found in business app tools.";
                _logger.LogWarning(result.Message);
                return result;
            }

            result.Data = toolData;
            result.Success = true;
            _logger.LogDebug("Successfully resolved node {NodeId} to Tool {ToolName} ({ToolId})", nodeId, toolData.General.Name[_currentSessionlanguageCode], toolData.Id);
            return result;
        }
    }
}