using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.Agent.Script.Node.StartNode;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessScriptsManager
    {
        private readonly IMongoClient _mongoClient;
        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;

        public BusinessScriptsManager(
            BusinessManager businessManager,
            IMongoClient mongoClient,
            BusinessAppRepository businessAppRepository,
            BusinessRepository businessRepository
        )
        {
            _mongoClient = mongoClient;
            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
        }

        public async Task<bool> CheckScriptExists(long businessId, string scriptId)
        {
            return await _businessAppRepository.CheckScriptExists(businessId, scriptId);
        }

        public async Task<BusinessAppScript?> GetScriptById(long businessId, string scriptId)
        {
            return await _businessAppRepository.GetScriptById(businessId, scriptId);
        }

        // Deleting Script
        public async Task<FunctionReturnResult> DeleteScript(long businessId, BusinessAppScript scriptData)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (scriptData.InboundRoutingReferences.Count > 0)
                {
                    return result.SetFailureResult(
                        "DeleteScript:INBOUND_ROUTING_REFERENCES",
                        "Cannot delete script with inbound routing references."
                    );
                }

                // TODO: check any ongoing inbound routing queues or its conversations
                // too complex, would rather let the queue or conversation fail

                if (scriptData.TelephonyCampaignReferences.Count > 0)
                {
                    return result.SetFailureResult(
                        "DeleteScript:TELEPHONY_CAMPAIGN_REFERENCES",
                        "Cannot delete script with telephony campaign references."
                    );
                }

                // TODO: check any ongoing telephony campaigns queues or its conversations
                // too complex, would rather let the queue or conversation fail

                if (scriptData.WebCampaignReferences.Count > 0)
                {
                    return result.SetFailureResult(
                        "DeleteScript:WEB_CAMPAIGN_REFERENCES",
                        "Cannot delete script with web campaign references."
                    );
                }

                // TODO: check any ongoing web campaigns queues or its conversations
                // too complex, would rather let the queue or conversation fail

                if (scriptData.ScriptAddScriptNodeReferences.Count > 0)
                {
                    return result.SetFailureResult(
                        "DeleteScript:SCRIPT_ADD_SCRIPT_NODE_REFERENCES",
                        "Cannot delete script with script add script node references."
                    );
                }

                var deleteResult = await _businessAppRepository.DeleteScript(businessId, scriptData.Id);
                if (!deleteResult)
                {
                    return result.SetFailureResult(
                        "DeleteScript:DELETE_SCRIPT",
                        "Failed to delete script in db."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteScript:EXCEPTION",
                    $"An error occurred: {ex.Message}"
                );
            }
        }

        // SAVING/ADDING SCRIPT
        public async Task<FunctionReturnResult<BusinessAppScript?>> AddOrUpdateScript(
            long businessId,
            string postType,
            IFormCollection formData,
            BusinessAppScript? existingScriptData
        )
        {
            var result = new FunctionReturnResult<BusinessAppScript?>();

            // Get business languages
            var businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

            // Parse changes data
            formData.TryGetValue("changes", out StringValues changesJsonString);
            if (string.IsNullOrWhiteSpace(changesJsonString))
            {
                result.Code = "AddOrUpdateScript:1";
                result.Message = "Changes data is required.";
                return result;
            }

            JsonElement changesRootElement;
            try
            {
                changesRootElement = JsonSerializer.Deserialize<JsonElement>(changesJsonString.ToString());
            }
            catch (Exception ex)
            {
                result.Code = "AddOrUpdateScript:2";
                result.Message = "Invalid changes data format: " + ex.Message;
                return result;
            }

            // Create new script instance
            var newScriptData = new BusinessAppScript()
            {
                Id = postType == "new" ? ObjectId.GenerateNewId().ToString() : existingScriptData!.Id
            };

            // General Section
            if (!changesRootElement.TryGetProperty("general", out var generalTabElement))
            {
                result.Code = "AddOrUpdateScript:3";
                result.Message = "General section not found.";
                return result;
            }
            else
            {
                var nameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    generalTabElement,
                    "name",
                    newScriptData.General.Name
                );
                if (!nameValidationResult.Success)
                {
                    result.Code = "AddOrUpdateScript:" + nameValidationResult.Code;
                    result.Message = nameValidationResult.Message;
                    return result;
                }

                var descriptionValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    generalTabElement,
                    "description",
                    newScriptData.General.Description
                );
                if (!descriptionValidationResult.Success)
                {
                    result.Code = "AddOrUpdateScript:" + descriptionValidationResult.Code;
                    result.Message = descriptionValidationResult.Message;
                    return result;
                }
            }

            // Nodes Section
            if (!changesRootElement.TryGetProperty("nodes", out var nodesElement))
            {
                result.Code = "AddOrUpdateScript:4";
                result.Message = "Nodes section not found.";
                return result;
            }

            var validateNodesResult = await ValidateAndCreateNodes(businessId, existingScriptData!.Id, nodesElement, businessLanguages);
            if (!validateNodesResult.Success)
            {
                result.Code = "AddOrUpdateScript:" + validateNodesResult.Code;
                result.Message = validateNodesResult.Message;
                return result;
            }
            newScriptData.Nodes = validateNodesResult.Data!;

            // Edges Section
            if (!changesRootElement.TryGetProperty("edges", out var edgesElement))
            {
                result.Code = "AddOrUpdateScript:5";
                result.Message = "Edges section not found.";
                return result;
            }

            var validateEdgesResult = ValidateAndCreateEdges(edgesElement, newScriptData.Nodes);
            if (!validateEdgesResult.Success)
            {
                result.Code = "AddOrUpdateScript:" + validateEdgesResult.Code;
                result.Message = validateEdgesResult.Message;
                return result;
            }
            newScriptData.Edges = validateEdgesResult.Data!;

            // Additional Validations
            if (newScriptData.Nodes.Count == 0)
            {
                result.Code = "AddOrUpdateScript:6";
                result.Message = "Script must contain at least one node.";
                return result;
            }

            if (newScriptData.Edges.Count == 0)
            {
                result.Code = "AddOrUpdateScript:7";
                result.Message = "Script must contain at least one connection.";
                return result;
            }

            // References
            List<(string, BusinessNumberScriptSMSNodeReference)> newSmsNodeBusinessNumberReferences = new List<(string, BusinessNumberScriptSMSNodeReference)>();
            List<(string, BusinessAppScriptAddScriptToContextNodeReference)> newAddScriptToContextNodeScriptReferences = new List<(string, BusinessAppScriptAddScriptToContextNodeReference)>();
            List<(string, BusinessAppAgentScriptTransferToAgentNodeReference)> newTransferToAgentNodeAgentReferences = new List<(string, BusinessAppAgentScriptTransferToAgentNodeReference)>();
            foreach (var node in newScriptData.Nodes)
            {
                if (node.NodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool)
                {
                    if (node is BusinessAppScriptSystemToolNode systemToolNode)
                    {
                        if (systemToolNode.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.SendSMS)
                        {
                            var smsNode = node as BusinessAppScriptSendSMSToolNode;
                            if (smsNode != null)
                            {
                                var phoneNumberId = smsNode.PhoneNumberId;

                                newSmsNodeBusinessNumberReferences.Add((
                                    phoneNumberId,
                                    new BusinessNumberScriptSMSNodeReference()
                                    {
                                        ScriptId = newScriptData.Id,
                                        NodeReference = node.Id
                                    }
                                ));
                            }
                        }
                        else if (systemToolNode.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.AddScriptToContext)
                        {
                            var addContextNode = node as BusinessAppScriptAddScriptToContextToolNode;
                            if (addContextNode != null)
                            {
                                var scriptId = addContextNode.ScriptId;

                                newAddScriptToContextNodeScriptReferences.Add((
                                    scriptId,
                                    new BusinessAppScriptAddScriptToContextNodeReference()
                                    {
                                        ScriptId = newScriptData.Id,
                                        NodeId = node.Id
                                    }
                                ));
                            }
                        }
                        else if (systemToolNode.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.TransferToAgent)
                        {
                            var transferToAgentNode = node as BusinessAppScriptTransferToAgentToolNode;
                            if (transferToAgentNode != null)
                            {
                                var agentId = transferToAgentNode.AgentId;

                                newTransferToAgentNodeAgentReferences.Add((
                                    agentId,
                                    new BusinessAppAgentScriptTransferToAgentNodeReference()
                                    {
                                        ScriptId = newScriptData.Id,
                                        NodeId = node.Id
                                    }
                                ));
                            }
                        }
                    }
                }
            }

            List<(string, BusinessNumberScriptSMSNodeReference)> deletedSmsNodeBusinessNumberReferences = new List<(string, BusinessNumberScriptSMSNodeReference)>();
            List<(string, BusinessAppScriptAddScriptToContextNodeReference)> deletedAddNodeScriptReferences = new List<(string, BusinessAppScriptAddScriptToContextNodeReference)>();
            List<(string, BusinessAppAgentScriptTransferToAgentNodeReference)> deletedTransferToAgentNodeAgentReferences = new List<(string, BusinessAppAgentScriptTransferToAgentNodeReference)>();
            if (postType != "new" && existingScriptData != null)
            {
                var oldSmsNodes = existingScriptData.Nodes
                    .OfType<BusinessAppScriptSendSMSToolNode>();

                foreach (var oldSmsNode in oldSmsNodes)
                {
                    var newCorrespondingNode = newScriptData.Nodes.FirstOrDefault(n => n.Id == oldSmsNode.Id);

                    if (newCorrespondingNode is not BusinessAppScriptSendSMSToolNode newSmsNode)
                    {
                        deletedSmsNodeBusinessNumberReferences.Add((
                            oldSmsNode.PhoneNumberId,
                            new BusinessNumberScriptSMSNodeReference()
                            {
                                ScriptId = existingScriptData.Id,
                                NodeReference = oldSmsNode.Id
                            }
                        ));
                    }
                    else if (oldSmsNode.PhoneNumberId != newSmsNode.PhoneNumberId)
                    {
                        deletedSmsNodeBusinessNumberReferences.Add((
                            oldSmsNode.PhoneNumberId,
                            new BusinessNumberScriptSMSNodeReference()
                            {
                                ScriptId = existingScriptData.Id,
                                NodeReference = oldSmsNode.Id
                            }
                        ));
                    }
                }

                var oldAddNodes = existingScriptData.Nodes
                    .OfType<BusinessAppScriptAddScriptToContextToolNode>();

                foreach (var oldAddNode in oldAddNodes)
                {
                    var newCorrespondingNode = newScriptData.Nodes.FirstOrDefault(n => n.Id == oldAddNode.Id);

                    if (newCorrespondingNode is not BusinessAppScriptAddScriptToContextToolNode newAddNode)
                    {
                        deletedAddNodeScriptReferences.Add((
                            oldAddNode.ScriptId,
                            new BusinessAppScriptAddScriptToContextNodeReference()
                            {
                                ScriptId = existingScriptData.Id,
                                NodeId = oldAddNode.Id
                            }
                        ));
                    }
                    else if (oldAddNode.ScriptId != newAddNode.ScriptId)
                    {
                        deletedAddNodeScriptReferences.Add((
                            oldAddNode.ScriptId,
                            new BusinessAppScriptAddScriptToContextNodeReference()
                            {
                                ScriptId = existingScriptData.Id,
                                NodeId = oldAddNode.Id
                            }
                        ));
                    }
                }

                var oldTransferToAgentNodes = existingScriptData.Nodes
                    .OfType<BusinessAppScriptTransferToAgentToolNode>();

                foreach (var oldTransferToAgentNode in oldTransferToAgentNodes)
                {
                    var newCorrespondingNode = newScriptData.Nodes.FirstOrDefault(n => n.Id == oldTransferToAgentNode.Id);

                    if (newCorrespondingNode is not BusinessAppScriptTransferToAgentToolNode newTransferToAgentNode)
                    {
                        deletedTransferToAgentNodeAgentReferences.Add((
                            oldTransferToAgentNode.AgentId,
                            new BusinessAppAgentScriptTransferToAgentNodeReference()
                            {
                                ScriptId = existingScriptData.Id,
                                NodeId = oldTransferToAgentNode.Id
                            }
                        ));
                    }
                    else if (oldTransferToAgentNode.AgentId != newTransferToAgentNode.AgentId)
                    {
                        deletedTransferToAgentNodeAgentReferences.Add((
                            oldTransferToAgentNode.AgentId,
                            new BusinessAppAgentScriptTransferToAgentNodeReference()
                            {
                                ScriptId = existingScriptData.Id,
                                NodeId = oldTransferToAgentNode.Id
                            }
                        ));
                    }
                }
            }

            try
            {
                using (var session = await _mongoClient.StartSessionAsync())
                {
                    session.StartTransaction();
                    try
                    {
                        if (postType == "new")
                        {
                            var addResult = await _businessAppRepository.AddScript(businessId, newScriptData, session);
                            if (!addResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateScript:ADD_FAILED",
                                    "Failed to add new script to agent."
                                );
                            }
                        }
                        else
                        {
                            var updateResult = await _businessAppRepository.UpdateScript(businessId, newScriptData, session);
                            if (!updateResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateScript:UPDATE_FAILED",
                                    "Failed to update existing script."
                                );
                            }
                        }

                        // SmsNode Reference to Business Number
                        foreach (var (phoneNumberId, reference) in newSmsNodeBusinessNumberReferences)
                        {
                            var addReferenceResult = await _businessAppRepository.AddAgentScriptSMSNodeReferenceToBusinessNumber(
                                businessId,
                                phoneNumberId,
                                reference,
                                session
                            );
                            if (!addReferenceResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateScript:SMS_NODE_REFERENCE_ADD_FAILED",
                                    "Failed to add sms node reference to business phone number."
                                );
                            }
                        }
                        foreach (var (phoneNumberId, reference) in deletedSmsNodeBusinessNumberReferences)
                        {
                            var deleteReferenceResult = await _businessAppRepository.RemoveAgentScriptSMSNodeReferenceFromBusinessNumber(
                                businessId,
                                phoneNumberId,
                                reference,
                                session
                            );
                            if (!deleteReferenceResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateScript:SMS_NODE_REFERENCE_DELETE_FAILED",
                                    "Failed to delete sms node reference from business phone number."
                                );
                            }
                        }
                        // Add Script To Context Node Reference
                        foreach (var (refScriptId, reference) in newAddScriptToContextNodeScriptReferences)
                        {
                            var addReferenceResult = await _businessAppRepository.AddScriptToContextNodeReferenceToScript(
                                businessId,
                                refScriptId,
                                reference,
                                session
                            );
                            if (!addReferenceResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateScript:SCRIPT_ADD_NODE_TO_CONTEXT_REFERENCE_ADD_FAILED",
                                    "Failed to add add node reference to script."
                                );
                            }
                        }
                        foreach (var (refScriptId, reference) in deletedAddNodeScriptReferences)
                        {
                            var deleteReferenceResult = await _businessAppRepository.RemoveAddScriptToContextReferenceFromScript(
                                businessId,
                                refScriptId,
                                reference,
                                session
                            );
                            if (!deleteReferenceResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateScript:SCRIPT_ADD_NODE_TO_CONTEXT_REFERENCE_DELETE_FAILED",
                                    "Failed to delete add node reference from script."
                                );
                            }
                        }

                        // Transfer To Agent Node Reference
                        foreach (var (agentId, reference) in newTransferToAgentNodeAgentReferences)
                        {
                            var addReferenceResult = await _businessAppRepository.AddScriptTransferToAgentNodeReferenceToAgent(
                                businessId,
                                agentId,
                                reference,
                                session
                            );
                            if (!addReferenceResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateScript:TRANSFER_TO_AGENT_NODE_REFERENCE_ADD_FAILED",
                                    "Failed to add transfer to agent node reference to agent."
                                );
                            }
                        }
                        foreach (var (agentId, reference) in deletedTransferToAgentNodeAgentReferences)
                        {
                            var deleteReferenceResult = await _businessAppRepository.RemoveScriptTransferToAgentNodeReferenceFromAgent(
                                businessId,
                                agentId,
                                reference,
                                session
                            );
                            if (!deleteReferenceResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateScript:TRANSFER_TO_AGENT_NODE_REFERENCE_DELETE_FAILED",
                                    "Failed to delete transfer to agent node reference from agent."
                                );
                            }
                        }

                        await session.CommitTransactionAsync();
                    }
                    catch (Exception ex)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "AddOrUpdateScript:DB_EXCEPTION",
                            ex.Message
                        );
                    }
                }

                return result.SetSuccessResult(newScriptData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddOrUpdateScript:EXCEPTION",
                    ex.Message
                );
            }
        }

        private async Task<FunctionReturnResult<List<BusinessAppScriptNode>>> ValidateAndCreateNodes(
            long businessId,
            string? existingScriptId,
            JsonElement nodesElement,
            IEnumerable<string> businessLanguages
        )
        {
            var result = new FunctionReturnResult<List<BusinessAppScriptNode>>();
            var nodes = new List<BusinessAppScriptNode>();

            bool hasStartNode = false;

            foreach (JsonElement nodeElement in nodesElement.EnumerateArray())
            {
                // Validate required properties
                if (!nodeElement.TryGetProperty("id", out var nodeIdElement))
                {
                    result.Code = "ValidateAndCreateNodes:1";
                    result.Message = "Node id not found.";
                    return result;
                }

                if (!nodeElement.TryGetProperty("type", out var nodeTypeElement))
                {
                    result.Code = "ValidateAndCreateNodes:2";
                    result.Message = "Node type not found.";
                    return result;
                }

                if (!nodeTypeElement.TryGetInt32(out var nodeTypeInt))
                {
                    result.Code = "ValidateAndCreateNodes:3";
                    result.Message = "Invalid node type.";
                    return result;
                }

                if (!Enum.IsDefined(typeof(BusinessAppAgentScriptNodeTypeENUM), nodeTypeInt))
                {
                    result.Code = "ValidateAndCreateNodes:4";
                    result.Message = "Invalid node type.";
                    return result;
                }

                if (!nodeElement.TryGetProperty("position", out var positionElement))
                {
                    result.Code = "ValidateAndCreateNodes:5";
                    result.Message = "Node position not found.";
                    return result;
                }

                if (!positionElement.TryGetProperty("x", out var positionXElement) ||
                    !positionElement.TryGetProperty("y", out var positionYElement))
                {
                    result.Code = "ValidateAndCreateNodes:5";
                    result.Message = "Invalid node position data.";
                    return result;
                }

                var nodeId = nodeIdElement.GetString();
                BusinessAppAgentScriptNodeTypeENUM nodeType = (BusinessAppAgentScriptNodeTypeENUM)nodeTypeInt;
                var position = new BusinessAppAgentScriptNodePosition
                {
                    X = positionXElement.GetDouble(),
                    Y = positionYElement.GetDouble()
                };

                // Handle different node types
                if (nodeType == BusinessAppAgentScriptNodeTypeENUM.Start)
                {
                    if (hasStartNode)
                    {
                        result.Code = "ValidateAndCreateNodes:5";
                        result.Message = "Multiple start nodes found.";
                        return result;
                    }

                    hasStartNode = true;
                    nodes.Add(new BusinessAppScriptStartNode
                    {
                        Id = nodeId,
                        Position = position
                    });
                }
                else if (nodeType == BusinessAppAgentScriptNodeTypeENUM.UserQuery)
                {
                    if (!nodeElement.TryGetProperty("query", out var queryElement))
                    {
                        result.Code = "ValidateAndCreateNodes:6";
                        result.Message = "User query data not found.";
                        return result;
                    }

                    var userQueryNode = new BusinessAppScriptUserQueryNode
                    {
                        Id = nodeId,
                        Position = position
                    };

                    var queryValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                        businessLanguages,
                        nodeElement,
                        "query",
                        userQueryNode.Query
                    );
                    if (!queryValidationResult.Success)
                    {
                        result.Code = "ValidateAndCreateNodes:" + queryValidationResult.Code;
                        result.Message = queryValidationResult.Message;
                        return result;
                    }

                    var examplesValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageListProperty(
                        businessLanguages,
                        nodeElement,
                        "examples",
                        userQueryNode.Examples,
                        true
                    );
                    if (!examplesValidationResult.Success)
                    {
                        result.Code = "ValidateAndCreateNodes:" + examplesValidationResult.Code;
                        result.Message = examplesValidationResult.Message;
                        return result;
                    }

                    nodes.Add(userQueryNode);
                }
                else if (nodeType == BusinessAppAgentScriptNodeTypeENUM.AIResponse)
                {
                    if (!nodeElement.TryGetProperty("response", out var responseElement))
                    {
                        result.Code = "ValidateAndCreateNodes:7";
                        result.Message = "AI response data not found.";
                        return result;
                    }

                    var aiResponseNode = new BusinessAppScriptAIResponseNode
                    {
                        Id = nodeId,
                        Position = position
                    };

                    var responseValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                        businessLanguages,
                        nodeElement,
                        "response",
                        aiResponseNode.Response
                    );
                    if (!responseValidationResult.Success)
                    {
                        result.Code = "ValidateAndCreateNodes:" + responseValidationResult.Code;
                        result.Message = responseValidationResult.Message;
                        return result;
                    }

                    var examplesValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageListProperty(
                        businessLanguages,
                        nodeElement,
                        "examples",
                        aiResponseNode.Examples,
                        true
                    );
                    if (!examplesValidationResult.Success)
                    {
                        result.Code = "ValidateAndCreateNodes:" + examplesValidationResult.Code;
                        result.Message = examplesValidationResult.Message;
                        return result;
                    }

                    nodes.Add(aiResponseNode);
                }
                else if (nodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool)
                {
                    if (!nodeElement.TryGetProperty("toolType", out var toolTypeElement))
                    {
                        result.Code = "ValidateAndCreateNodes:10";
                        result.Message = "System tool type not found.";
                        return result;
                    }

                    if (!toolTypeElement.TryGetInt32(out var toolTypeInt))
                    {
                        result.Code = "ValidateAndCreateNodes:11";
                        result.Message = "Invalid system tool type.";
                        return result;
                    }

                    if (!Enum.IsDefined(typeof(BusinessAppAgentScriptNodeSystemToolTypeENUM), toolTypeInt))
                    {
                        result.Code = "ValidateAndCreateNodes:12";
                        result.Message = "Invalid system tool type.";
                        return result;
                    }

                    BusinessAppAgentScriptNodeSystemToolTypeENUM toolType = (BusinessAppAgentScriptNodeSystemToolTypeENUM)toolTypeInt;

                    if (!nodeElement.TryGetProperty("config", out var toolConfigElement))
                    {
                        result.Code = "ValidateAndCreateNodes:12";
                        result.Message = "System tool config not found.";
                        return result;
                    }

                    // End Call Tool
                    if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.EndCall)
                    {
                        var endCallNode = new BusinessAppScriptEndCallToolNode
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("type", out var endCallTypeElement))
                        {
                            result.Code = "ValidateAndCreateNodes:12";
                            result.Message = "End call type not found.";
                            return result;
                        }

                        if (!endCallTypeElement.TryGetInt32(out var endCallTypeInt))
                        {
                            result.Code = "ValidateAndCreateNodes:13";
                            result.Message = "Invalid end call type.";
                            return result;
                        }

                        if (!Enum.IsDefined(typeof(BusinessAppAgentScriptEndCallTypeENUM), endCallTypeInt))
                        {
                            result.Code = "ValidateAndCreateNodes:14";
                            result.Message = "Invalid end call type.";
                            return result;
                        }

                        endCallNode.Type = (BusinessAppAgentScriptEndCallTypeENUM)endCallTypeInt;
                        if (endCallNode.Type == BusinessAppAgentScriptEndCallTypeENUM.WithMessage)
                        {
                            endCallNode.Messages = new Dictionary<string, string>();

                            var messagesValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                                businessLanguages,
                                toolConfigElement,
                                "messages",
                                endCallNode.Messages
                            );
                            if (!messagesValidationResult.Success)
                            {
                                result.Code = "ValidateAndCreateNodes:" + messagesValidationResult.Code;
                                result.Message = messagesValidationResult.Message;
                                return result;
                            }
                        }

                        nodes.Add(endCallNode);
                    }
                    // DTMF Input Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.GetDTMFKeypadInput)
                    {
                        var dtmfNode = new BusinessAppScriptDTMFInputToolNode
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("timeout", out var timeoutElement))
                        {
                            result.Code = "ValidateAndCreateNodes:15";
                            result.Message = "DTMF timeout not found.";
                            return result;
                        }
                        dtmfNode.Timeout = timeoutElement.GetInt32();

                        if (!toolConfigElement.TryGetProperty("requireStartAsterisk", out var requireStartElement))
                        {
                            result.Code = "ValidateAndCreateNodes:16";
                            result.Message = "DTMF require start asterisk not found.";
                            return result;
                        }
                        dtmfNode.RequireStartAsterisk = requireStartElement.GetBoolean();

                        if (!toolConfigElement.TryGetProperty("requireEndHash", out var requireEndElement))
                        {
                            result.Code = "ValidateAndCreateNodes:17";
                            result.Message = "DTMF require end hash not found.";
                            return result;
                        }
                        dtmfNode.RequireEndHash = requireEndElement.GetBoolean();

                        if (!toolConfigElement.TryGetProperty("maxLength", out var maxLengthElement))
                        {
                            result.Code = "ValidateAndCreateNodes:18";
                            result.Message = "DTMF max length not found.";
                            return result;
                        }
                        dtmfNode.MaxLength = maxLengthElement.GetInt32();

                        if (!toolConfigElement.TryGetProperty("encryptInput", out var encryptElement))
                        {
                            result.Code = "ValidateAndCreateNodes:19";
                            result.Message = "DTMF encrypt input not found.";
                            return result;
                        }
                        dtmfNode.EncryptInput = encryptElement.GetBoolean();

                        if (dtmfNode.EncryptInput)
                        {
                            if (!toolConfigElement.TryGetProperty("variableName", out var variableNameElement))
                            {
                                result.Code = "ValidateAndCreateNodes:20";
                                result.Message = "DTMF variable name not found.";
                                return result;
                            }
                            dtmfNode.VariableName = variableNameElement.GetString();
                        }

                        if (!toolConfigElement.TryGetProperty("outcomes", out var outcomesElement))
                        {
                            result.Code = "ValidateAndCreateNodes:21";
                            result.Message = "DTMF outcomes not found.";
                            return result;
                        }

                        foreach (var outcomeElement in outcomesElement.EnumerateArray())
                        {
                            var newOutcomeData = new BusinessAppAgentScriptDTMFOutcome();

                            var valueValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                                businessLanguages,
                                outcomeElement,
                                "value",
                                newOutcomeData.Value
                            );
                            if (!valueValidationResult.Success)
                            {
                                result.Code = "ValidateAndCreateNodes:" + valueValidationResult.Code;
                                result.Message = valueValidationResult.Message;
                                return result;
                            }

                            if (!outcomeElement.TryGetProperty("portId", out var portIdElement))
                            {
                                result.Code = "ValidateAndCreateNodes:22";
                                result.Message = "DTMF port ID not found.";
                                return result;
                            }

                            newOutcomeData.PortId = portIdElement.GetString();

                            dtmfNode.Outcomes.Add(newOutcomeData);
                        }

                        nodes.Add(dtmfNode);
                    }
                    // Transfer To Agent Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.TransferToAgent)
                    {
                        var transferNode = new BusinessAppScriptTransferToAgentToolNode
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("agentId", out var agentIdElement))
                        {
                            result.Code = "ValidateAndCreateNodes:23";
                            result.Message = "Transfer agent ID not found.";
                            return result;
                        }
                        var transferAgentId = agentIdElement.GetString();
                        if (!string.IsNullOrWhiteSpace(transferAgentId))
                        {
                            var transferAgent = await _parentBusinessManager.GetAgentsManager().GetAgentById(businessId, transferAgentId);
                            if (transferAgent == null)
                            {
                                result.Code = "ValidateAndCreateNodes:24";
                                result.Message = "Transfer agent not found.";
                                return result;
                            }
                            transferNode.AgentId = transferAgentId;
                        }

                        if (!toolConfigElement.TryGetProperty("transferContext", out var transferContextElement))
                        {
                            result.Code = "ValidateAndCreateNodes:25";
                            result.Message = "Transfer context flag not found.";
                            return result;
                        }
                        transferNode.TransferConversation = transferContextElement.GetBoolean();

                        if (!toolConfigElement.TryGetProperty("summarizeContext", out var summarizeContextElement))
                        {
                            result.Code = "ValidateAndCreateNodes:26";
                            result.Message = "Summarize context flag not found.";
                            return result;
                        }
                        transferNode.SummarizeConversation = summarizeContextElement.GetBoolean();

                        nodes.Add(transferNode);
                    }
                    // Add Script To Context Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.AddScriptToContext)
                    {
                        var addScriptNode = new BusinessAppScriptAddScriptToContextToolNode
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("scriptId", out var scriptIdElement))
                        {
                            result.Code = "ValidateAndCreateNodes:27";
                            result.Message = "Script ID not found.";
                            return result;
                        }

                        var scriptId = scriptIdElement.GetString();
                        if (string.IsNullOrWhiteSpace(scriptId))
                        {
                            result.Code = "ValidateAndCreateNodes:28";
                            result.Message = "Script ID invalid for add script to context node.";
                            return result;
                        }

                        if (existingScriptId != null && !string.IsNullOrWhiteSpace(existingScriptId) && scriptId == existingScriptId)
                        {
                            result.Code = "ValidateAndCreateNodes:29";
                            result.Message = "Script ID can not point to current script for add script to context node.";
                            return result;
                        }

                        bool scriptExists = await _businessAppRepository.CheckScriptExists(businessId, scriptId);
                        if (!scriptExists)
                        {
                            result.Code = "ValidateAndCreateNodes:29";
                            result.Message = "Script not found for add script to context node.";
                            return result;
                        }

                        addScriptNode.ScriptId = scriptId;

                        nodes.Add(addScriptNode);
                    }
                    // Change Language Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.ChangeLanguage)
                    {
                        var changeLanguageNode = new BusinessAppScriptSystemToolNode()
                        {
                            Id = nodeId,
                            Position = position,
                            ToolType = BusinessAppAgentScriptNodeSystemToolTypeENUM.ChangeLanguage,
                        };

                        nodes.Add(changeLanguageNode);
                    }
                    // Press DTMF Keypad Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.PressDTMFKeypad)
                    {
                        var pressDtmfKeypadNode = new BusinessAppScriptSystemToolNode()
                        {
                            Id = nodeId,
                            Position = position,
                            ToolType = BusinessAppAgentScriptNodeSystemToolTypeENUM.PressDTMFKeypad
                        };

                        nodes.Add(pressDtmfKeypadNode);
                    }
                    // Send SMS Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.SendSMS)
                    {
                        var sendSmsNode = new BusinessAppScriptSendSMSToolNode()
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("phoneNumberId", out var phoneNumberIdElement))
                        {
                            result.Code = "ValidateAndCreateNodes:SEND_SMS_TOOL_PHONE_NUMBER_ID_NOT_FOUND";
                            result.Message = "Phone number ID not found for send SMS node.";
                            return result;
                        }

                        var phoneNumberId = phoneNumberIdElement.GetString();
                        if (string.IsNullOrWhiteSpace(phoneNumberId))
                        {
                            result.Code = "ValidateAndCreateNodes:SEND_SMS_TOOL_PHONE_NUMBER_ID_INVALID";
                            result.Message = "Phone number ID invalid for send SMS node.";
                            return result;
                        }

                        var businessNumberData = await _parentBusinessManager.GetNumberManager().GetBusinessNumberById(businessId, phoneNumberId);
                        if (businessNumberData == null)
                        {
                            result.Code = "ValidateAndCreateNodes:SEND_SMS_TOOL_PHONE_NUMBER_NOT_FOUND";
                            result.Message = "Phone number not found for send SMS node.";
                            return result;
                        }

                        if (!businessNumberData.SmsEnabled)
                        {
                            result.Code = "ValidateAndCreateNodes:SEND_SMS_TOOL_PHONE_NUMBER_SMS_NOT_ENABLED";
                            result.Message = "Phone number SMS not enabled.";
                            return result;
                        }

                        sendSmsNode.PhoneNumberId = phoneNumberId;
                        sendSmsNode.Messages = new Dictionary<string, string>();

                        var queryValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                            businessLanguages,
                            toolConfigElement,
                            "messages",
                            sendSmsNode.Messages
                        );
                        if (!queryValidationResult.Success)
                        {
                            result.Code = "ValidateAndCreateNodes:" + queryValidationResult.Code;
                            result.Message = queryValidationResult.Message;
                            return result;
                        }

                        nodes.Add(sendSmsNode);
                    }
                    // Go To Node
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.GoToNode)
                    {
                        var goToNode = new BusinessAppScriptGoToNodeToolNode()
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("goToNodeId", out var goToNodeIdElement))
                        {
                            result.Code = "ValidateAndCreateNodes:GO_TO_NODE_GO_TO_NODE_ID_NOT_FOUND";
                            result.Message = "Go to node ID not found for go to node node.";
                            return result;
                        }

                        var goToNodeId = goToNodeIdElement.GetString();
                        if (string.IsNullOrWhiteSpace(goToNodeId))
                        {
                            result.Code = "ValidateAndCreateNodes:GO_TO_NODE_GO_TO_NODE_ID_INVALID";
                            result.Message = "Go to node ID invalid for go to node node.";
                            return result;
                        }

                        goToNode.GoToNodeId = goToNodeId;

                        nodes.Add(goToNode);
                    }
                    // Retrieve KnowledgeBase Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.RetrieveKnowledgeBase)
                    {
                        var retrieveKnowledgeBaseNode = new BusinessAppScriptRetrieveKnowledgeBaseNode()
                        {
                            Id = nodeId,
                            Position = position
                        };

                        var messagesValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                            businessLanguages,
                            toolConfigElement,
                            "responseBeforeExecution",
                            retrieveKnowledgeBaseNode.ResponseBeforeExecution
                        );
                        if (!messagesValidationResult.Success)
                        {
                            result.Code = "ValidateAndCreateNodes:" + messagesValidationResult.Code;
                            result.Message = messagesValidationResult.Message;
                            return result;
                        }

                        nodes.Add(retrieveKnowledgeBaseNode);
                    }
                    // Unknown System Tool
                    else
                    {
                        result.Code = "ValidateAndCreateNodes:28";
                        result.Message = $"Unknown system tool type: {toolType}";
                        return result;
                    }
                }
                else if (nodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteCustomTool)
                {
                    if (!nodeElement.TryGetProperty("toolId", out var toolIdElement))
                    {
                        result.Code = "ValidateAndCreateNodes:29";
                        result.Message = "Custom tool ID not found.";
                        return result;
                    }

                    var toolId = toolIdElement.GetString();
                    if (string.IsNullOrWhiteSpace(toolId))
                    {
                        result.Code = "ValidateAndCreateNodes:30";
                        result.Message = "Invalid custom tool ID.";
                        return result;
                    }

                    // Validate tool exists
                    var tool = await _parentBusinessManager.GetToolsManager().CheckBusinessToolExists(businessId, toolId);
                    if (!tool)
                    {
                        result.Code = "ValidateAndCreateNodes:31";
                        result.Message = "Custom tool not found.";
                        return result;
                    }

                    var customToolNode = new BusinessAppScriptCustomToolNode
                    {
                        Id = nodeId,
                        Position = position,
                        ToolId = toolId
                    };

                    // Validate and assign tool configuration
                    if (!nodeElement.TryGetProperty("config", out var configElement))
                    {
                        result.Code = "ValidateAndCreateNodes:32";
                        result.Message = "Custom tool configuration not found.";
                        return result;
                    }

                    foreach (var configProperty in configElement.EnumerateObject())
                    {
                        customToolNode.ToolConfiguration[configProperty.Name] = configProperty.Value.GetString() ?? "";
                    }

                    nodes.Add(customToolNode);
                }
                else
                {
                    result.Code = "ValidateAndCreateNodes:35";
                    result.Message = $"Unknown node type: {nodeType}";
                    return result;
                }
            }

            // Nodes that mention other nodes (required complilation of other nodes first)
            foreach (var node in nodes)
            {
                if (node is BusinessAppScriptGoToNodeToolNode goToNode)
                {
                    var linkedGoToNode = nodes.FirstOrDefault(x => x.Id == goToNode.GoToNodeId);
                    if (linkedGoToNode == null || linkedGoToNode.Id == goToNode.Id || linkedGoToNode is BusinessAppScriptGoToNodeToolNode)
                    {
                        result.Code = "ValidateAndCreateNodes:GO_TO_NODE_GO_TO_NODE_INVALID_SELECTION";
                        result.Message = $"Go to node ({goToNode.Id}) invalid node selection.";
                        return result;
                    }
                }
            }

            // Final validations
            if (!hasStartNode)
            {
                result.Code = "ValidateAndCreateNodes:8";
                result.Message = "Start node is required.";
                return result;
            }

            result.Success = true;
            result.Data = nodes;
            return result;
        }

        private FunctionReturnResult<List<BusinessAppScriptEdge>> ValidateAndCreateEdges(
            JsonElement edgesElement,
            List<BusinessAppScriptNode> nodes)
        {
            var result = new FunctionReturnResult<List<BusinessAppScriptEdge>>();
            var edges = new List<BusinessAppScriptEdge>();

            foreach (JsonElement edgeElement in edgesElement.EnumerateArray())
            {
                // Validate required properties
                if (!edgeElement.TryGetProperty("id", out var edgeIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:1";
                    result.Message = "Edge ID not found.";
                    return result;
                }

                if (!edgeElement.TryGetProperty("sourceNodeId", out var sourceNodeIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:2";
                    result.Message = "Source node ID not found.";
                    return result;
                }

                if (!edgeElement.TryGetProperty("sourceNodePortId", out var sourcePortIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:3";
                    result.Message = "Source port ID not found.";
                    return result;
                }

                if (!edgeElement.TryGetProperty("targetNodeId", out var targetNodeIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:4";
                    result.Message = "Target node ID not found.";
                    return result;
                }

                if (!edgeElement.TryGetProperty("targetNodePortId", out var targetPortIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:5";
                    result.Message = "Target port ID not found.";
                    return result;
                }

                string? edgeId = edgeIdElement.GetString();
                string? sourceNodeId = sourceNodeIdElement.GetString();
                string? sourcePortId = sourcePortIdElement.GetString();
                string? targetNodeId = targetNodeIdElement.GetString();
                string? targetPortId = targetPortIdElement.GetString();

                if (string.IsNullOrWhiteSpace(edgeId) || string.IsNullOrWhiteSpace(sourceNodeId) || string.IsNullOrWhiteSpace(sourcePortId))
                {
                    result.Code = "ValidateAndCreateEdges:6";
                    result.Message = "Invalid edge data.";
                    return result;
                }

                // Validate source node exists
                var sourceNode = nodes.FirstOrDefault(n => n.Id == sourceNodeId);
                if (sourceNode == null)
                {
                    result.Code = "ValidateAndCreateEdges:7";
                    result.Message = $"Source node not found: {sourceNodeId}";
                    return result;
                }

                if (!string.IsNullOrEmpty(targetNodeId))
                {
                    // Validate target node exists
                    var targetNode = nodes.FirstOrDefault(n => n.Id == targetNodeId);
                    if (targetNode == null)
                    {
                        result.Code = "ValidateAndCreateEdges:8";
                        result.Message = $"Target node not found: {targetNodeId}";
                        return result;
                    }

                    // Validate connection rules
                    var connectionValidation = ValidateNodeConnection(sourceNode, targetNode, sourcePortId, targetPortId);
                    if (!connectionValidation.Success)
                    {
                        result.Code = "ValidateAndCreateEdges:" + connectionValidation.Code;
                        result.Message = connectionValidation.Message;
                        return result;
                    }
                }

                // Create edge
                var edge = new BusinessAppScriptEdge
                {
                    Id = edgeId,
                    SourceNodeId = sourceNodeId,
                    SourceNodePortId = sourcePortId,
                    TargetNodeId = targetNodeId ?? "",
                    TargetNodePortId = targetPortId ?? ""
                };

                edges.Add(edge);
            }

            // Validate start node is connected
            var startNode = nodes.FirstOrDefault(n => n.NodeType == BusinessAppAgentScriptNodeTypeENUM.Start);
            if (startNode != null && !edges.Any(e => e.SourceNodeId == startNode.Id))
            {
                result.Code = "ValidateAndCreateEdges:8";
                result.Message = "Start node must be connected to at least one node.";
                return result;
            }

            result.Success = true;
            result.Data = edges;
            return result;
        }

        private FunctionReturnResult ValidateNodeConnection(
            BusinessAppScriptNode sourceNode,
            BusinessAppScriptNode targetNode,
            string? sourcePortId,
            string? targetPortId)
        {
            var result = new FunctionReturnResult();

            // Start node cannot connect to AI response node
            if (sourceNode.NodeType == BusinessAppAgentScriptNodeTypeENUM.Start &&
                targetNode.NodeType == BusinessAppAgentScriptNodeTypeENUM.AIResponse)
            {
                result.Code = "1";
                result.Message = $"Start node cannot connect to AI Response node {targetNode.Id}.";
                return result;
            }

            // AI response node can only connect to user query node
            if (sourceNode.NodeType == BusinessAppAgentScriptNodeTypeENUM.AIResponse &&
                targetNode.NodeType != BusinessAppAgentScriptNodeTypeENUM.UserQuery)
            {
                result.Code = "2";
                result.Message = $"AI Response node {sourceNode.Id} can only connect to User Query node, but connected to {targetNode.NodeType} {targetNode.Id} node.";
                return result;
            }

            result.Success = true;
            return result;
        }
    }
}
