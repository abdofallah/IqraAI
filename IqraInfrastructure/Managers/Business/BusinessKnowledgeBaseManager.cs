using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraCore.Models.KnowledgeBase;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Managers.File;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessKnowledgeBaseManager
    {
        private readonly BusinessManager _parentBusinessManager;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessKnowledgeBaseDocumentRepository _knowledgeBaseDocumentRepository;
        private readonly IntegrationConfigurationManager _integrationConfigurationManager;

        // --- Services to be Injected ---
        private readonly KnowledgeBaseVectorRepository _documentVectorRepository;
        private readonly UnstructuredManager _unstructuredManager;


        public BusinessKnowledgeBaseManager(
            BusinessManager parentBusinessManager,
            BusinessAppRepository businessAppRepository,
            BusinessKnowledgeBaseDocumentRepository knowledgeBaseDocumentRepository,
            IntegrationConfigurationManager integrationConfigurationManager,
            KnowledgeBaseVectorRepository documentVectorRepository,
            UnstructuredManager unstructuredManager)
        {
            _parentBusinessManager = parentBusinessManager;
            _businessAppRepository = businessAppRepository;
            _knowledgeBaseDocumentRepository = knowledgeBaseDocumentRepository;
            _integrationConfigurationManager = integrationConfigurationManager;
            _documentVectorRepository = documentVectorRepository;
            _unstructuredManager = unstructuredManager;
        }

        public async Task<FunctionReturnResult<BusinessAppKnowledgeBase?>> GetKnowledgeBaseById(long businessId, string existingKbId)
        {
            var result = new FunctionReturnResult<BusinessAppKnowledgeBase?>();

            var knowledgeBase = await _businessAppRepository.GetBusinessAppKnowledgeBaseAsync(businessId, existingKbId);
            if (knowledgeBase == null)
            {
                result.Code = "GetKnowledgeBaseById:NOT_FOUND";
                result.Message = "Knowledge base not found.";
            }
            
            return result.SetSuccessResult(knowledgeBase);
        }

        public async Task<FunctionReturnResult<BusinessAppKnowledgeBase?>> AddOrUpdateKnowledgeBaseAsync(long businessId, IFormCollection formData, string postType, BusinessAppKnowledgeBase? existingKbData)
        {
            var result = new FunctionReturnResult<BusinessAppKnowledgeBase?>();

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                return result.SetFailureResult(
                    "AddOrUpdateKnowledgeBaseAsync:MISSING_CHANGES_DATA",
                    "Changes not found in form data."
                );
            }

            JsonElement changesRootElement;
            try
            {
                changesRootElement = JsonSerializer.Deserialize<JsonElement>(changesJsonString.ToString());
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddOrUpdateKnowledgeBaseAsync:CHANGES_DESERIALIZATION_ERROR",
                    $"Invalid changes data format: {ex.Message}"
                );
            }

            var newKnowledgeBaseData = new BusinessAppKnowledgeBase();

            // General Section
            if (!changesRootElement.TryGetProperty("general", out var generalTabElement))
            {
                return result.SetFailureResult(
                    "AddOrUpdateKnowledgeBaseAsync:GENERAL_SECTION_NOT_FOUND",
                    "General section not found."
                );
            }
            else
            {
                if (generalTabElement.TryGetProperty("emoji", out var emojiElement))
                {
                    newKnowledgeBaseData.General.Emoji = emojiElement.GetString();
                }

                if (!generalTabElement.TryGetProperty("name", out var nameElement) || string.IsNullOrWhiteSpace(nameElement.GetString()))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:GENERAL_NAME_NOT_FOUND",
                        "General name not found or empty."
                    );
                }
                else
                {
                    newKnowledgeBaseData.General.Name = nameElement.GetString();
                }

                if (!generalTabElement.TryGetProperty("description", out var descriptionElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:GENERAL_DESCRIPTION_NOT_FOUND",
                        "General description not found."
                    );
                }
                else
                {
                    newKnowledgeBaseData.General.Description = descriptionElement.GetString();
                }
            }

            // Configuration Section
            bool isEmbeddingModelForEditChanged = false;
            if (!changesRootElement.TryGetProperty("configuration", out var configurationTabElement))
            {
                return result.SetFailureResult(
                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_SECTION_NOT_FOUND",
                    "Configuration section not found."
                );
            }
            else
            {
                // Chunking
                if (!configurationTabElement.TryGetProperty("chunking", out var chunkingElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_NOT_FOUND",
                        "Configuration chunking not found."
                    );
                }
                else
                {
                    if (!chunkingElement.TryGetProperty("mode", out var modeElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_MODE_NOT_FOUND",
                            "Configuration chunking mode not found."
                        );
                    }
                    if (!modeElement.TryGetInt32(out var modeInt))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_MODE_INVALID",
                            "Configuration chunking mode invalid."
                        );
                    }
                    if (!Enum.IsDefined(typeof(KnowledgeBaseChunkingType), modeInt))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_MODE_ENUM_INVALID",
                            "Configuration chunking mode enum invalid."
                        );
                    }

                    var chunkingType = (KnowledgeBaseChunkingType)modeInt;
                    if (postType == "edit" && existingKbData!.Configuration.Chunking.Type != chunkingType)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_MODE_CHANGE_NOT_ALLOWED",
                            "Configuration chunking mode change not allowed."
                        );
                    }

                    newKnowledgeBaseData.Configuration.Chunking.Type = chunkingType;
                    if (chunkingType == KnowledgeBaseChunkingType.General)
                    {
                        var generalChunking = new BusinessAppKnowledgeBaseConfigurationGeneralChunking();

                        // Delimiter
                        if (!configurationTabElement.TryGetProperty("delimiter", out var delimiterElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_DELIMITER_NOT_FOUND",
                                "Configuration chunking general delimiter not found."
                            );
                        }
                        string? delimiter = delimiterElement.GetString();
                        if (string.IsNullOrWhiteSpace(delimiter))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_DELIMITER_EMPTY",
                                "Configuration chunking general delimiter empty."
                            );
                        }
                        generalChunking.Delimiter = delimiter;

                        // Max Length
                        if (!configurationTabElement.TryGetProperty("maxLength", out var maxLengthElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_MAX_LENGTH_NOT_FOUND",
                                "Configuration chunking general max length not found."
                            );
                        }
                        if (!maxLengthElement.TryGetInt32(out var maxLengthInt) || maxLengthInt <= 1)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_MAX_LENGTH_INVALID",
                                "Configuration chunking general max length invalid."
                            );
                        }
                        generalChunking.MaxLength = maxLengthInt;

                        // Overlap
                        if (!configurationTabElement.TryGetProperty("overlap", out var overlapElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_OVERLAP_NOT_FOUND",
                                "Configuration chunking general overlap not found."
                            );
                        }
                        if (!overlapElement.TryGetInt32(out var overlapInt) || overlapInt < 0)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_OVERLAP_INVALID",
                                "Configuration chunking general overlap invalid."
                            );
                        }
                        generalChunking.Overlap = overlapInt;

                        // Prerpocess
                        if (!configurationTabElement.TryGetProperty("preprocess", out var preprocessElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_PREPROCESS_NOT_FOUND",
                                "Configuration chunking general preprocess not found."
                            );
                        }
                        else
                        {
                            // replaceConsecutive
                            if (!preprocessElement.TryGetProperty("replaceConsecutive", out var replaceConsecutiveElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_PREPROCESS_REPLACE_CONSECUTIVE_NOT_FOUND",
                                    "Configuration chunking general preprocess replace consecutive not found."
                                );
                            }
                            if (replaceConsecutiveElement.ValueKind != JsonValueKind.True && replaceConsecutiveElement.ValueKind != JsonValueKind.False)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_PREPROCESS_REPLACE_CONSECUTIVE_INVALID",
                                    "Configuration chunking general preprocess replace consecutive invalid."
                                );
                            }
                            generalChunking.Preprocess.ReplaceConsecutive = replaceConsecutiveElement.GetBoolean();

                            // DeleteUrls
                            if (!preprocessElement.TryGetProperty("deleteUrls", out var deleteUrlsElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_PREPROCESS_DELETE_URLS_NOT_FOUND",
                                    "Configuration chunking general preprocess delete urls not found."
                                );
                            }
                            if (deleteUrlsElement.ValueKind != JsonValueKind.True && deleteUrlsElement.ValueKind != JsonValueKind.False)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_PREPROCESS_DELETE_URLS_INVALID",
                                    "Configuration chunking general preprocess delete urls invalid."
                                );
                            }
                            generalChunking.Preprocess.DeleteUrls = deleteUrlsElement.GetBoolean();
                        }

                        newKnowledgeBaseData.Configuration.Chunking = generalChunking;
                    }
                    else if (chunkingType == KnowledgeBaseChunkingType.ParentChild)
                    {
                        var parentChildChunking = new BusinessAppKnowledgeBaseConfigurationParentChildChunking();

                        // parent
                        if (!configurationTabElement.TryGetProperty("parent", out var parentElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_NOT_FOUND",
                                "Configuration chunking parent not found."
                            );
                        }
                        else
                        {
                            // type enum - KnowledgeBaseChunkingParentChunkType
                            if (!parentElement.TryGetProperty("type", out var parentTypeElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_TYPE_NOT_FOUND",
                                    "Configuration chunking parent type not found."
                                );
                            }
                            if (!parentTypeElement.TryGetInt32(out var parentTypeInt))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_TYPE_INVALID",
                                    "Configuration chunking parent type invalid."
                                );
                            }
                            if (!Enum.IsDefined(typeof(KnowledgeBaseChunkingParentChunkType), parentTypeInt))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_TYPE_INVALID_TYPE",
                                    "Configuration chunking parent type invalid type."
                                );
                            }

                            var parentType = (KnowledgeBaseChunkingParentChunkType)parentTypeInt;
                            parentChildChunking.Parent.Type = parentType;

                            if (parentType == KnowledgeBaseChunkingParentChunkType.Paragraph)
                            {
                                // delimiter
                                if (!parentElement.TryGetProperty("delimiter", out var delimiterElement))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_DELIMITER_NOT_FOUND",
                                        "Configuration chunking parent delimiter not found."
                                    );
                                }
                                string? paragraphDelimiter = delimiterElement.GetString();
                                if (string.IsNullOrEmpty(parentChildChunking.Parent.Delimiter))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_DELIMITER_INVALID",
                                        "Configuration chunking parent delimiter invalid."
                                    );
                                }
                                parentChildChunking.Parent.Delimiter = paragraphDelimiter;

                                // maxlength
                                if (!parentElement.TryGetProperty("maxLength", out var maxLengthElement))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_MAX_LENGTH_NOT_FOUND",
                                        "Configuration chunking parent max length not found."
                                    );
                                }
                                if (!maxLengthElement.TryGetInt32(out var maxLengthInt))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_MAX_LENGTH_INVALID",
                                        "Configuration chunking parent max length invalid."
                                    );
                                }
                                parentChildChunking.Parent.MaxLength = maxLengthInt;
                            }
                            else if (parentType != KnowledgeBaseChunkingParentChunkType.FullDoc)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_INVALID_TYPE",
                                    "Configuration chunking parent invalid type."
                                );
                            }
                        }

                        // child
                        if (!configurationTabElement.TryGetProperty("child", out var childElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_CHILD_NOT_FOUND",
                                "Configuration chunking child not found."
                            );
                        }
                        else
                        {
                            // delimiter
                            if (!childElement.TryGetProperty("delimiter", out var delimiterElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_CHILD_DELIMITER_NOT_FOUND",
                                    "Configuration chunking child delimiter not found."
                                );
                            }
                            string? paragraphDelimiter = delimiterElement.GetString();
                            if (string.IsNullOrEmpty(parentChildChunking.Child.Delimiter))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_CHILD_DELIMITER_INVALID",
                                    "Configuration chunking child delimiter invalid."
                                );
                            }
                            parentChildChunking.Child.Delimiter = paragraphDelimiter;

                            // maxlength
                            if (!childElement.TryGetProperty("maxLength", out var maxLengthElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_CHILD_MAX_LENGTH_NOT_FOUND",
                                    "Configuration chunking child max length not found."
                                );
                            }
                            if (!maxLengthElement.TryGetInt32(out var maxLengthInt))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_CHILD_MAX_LENGTH_INVALID",
                                    "Configuration chunking child max length invalid."
                                );
                            }
                            parentChildChunking.Child.MaxLength = maxLengthInt;
                        }

                        // preprocess
                        if (!configurationTabElement.TryGetProperty("preprocess", out var preprocessElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PREPROCESS_NOT_FOUND",
                                "Configuration chunking preprocess not found."
                            );
                        }
                        else
                        {
                            // replaceConsecutive
                            if (!preprocessElement.TryGetProperty("replaceConsecutive", out var replaceConsecutiveElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PREPROCESS_REPLACE_CONSECUTIVE_NOT_FOUND",
                                    "Configuration chunking preprocess replace consecutive not found."
                                );
                            }
                            if (replaceConsecutiveElement.ValueKind != JsonValueKind.True && replaceConsecutiveElement.ValueKind != JsonValueKind.False)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PREPROCESS_REPLACE_CONSECUTIVE_INVALID",
                                    "Configuration chunking preprocess replace consecutive invalid."
                                );
                            }
                            parentChildChunking.Preprocess.ReplaceConsecutive = replaceConsecutiveElement.GetBoolean();

                            // deleteUrls
                            if (!preprocessElement.TryGetProperty("deleteUrls", out var deleteUrlsElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PREPROCESS_DELETE_URLS_NOT_FOUND",
                                    "Configuration chunking preprocess delete urls not found."
                                );
                            }
                            if (deleteUrlsElement.ValueKind != JsonValueKind.True && deleteUrlsElement.ValueKind != JsonValueKind.False)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PREPROCESS_DELETE_URLS_INVALID",
                                    "Configuration chunking preprocess delete urls invalid."
                                );
                            }
                            parentChildChunking.Preprocess.DeleteUrls = deleteUrlsElement.GetBoolean();
                        }

                        newKnowledgeBaseData.Configuration.Chunking = parentChildChunking;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_MODE_INVALID_TYPE",
                            "Configuration chunking mode invalid type."
                        );
                    }
                }

                // Embedding
                if (!configurationTabElement.TryGetProperty("embedding", out var embeddingIntegrationElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_EMBEDDING_NOT_FOUND",
                        "Configuration embedding not found."
                    );
                }
                else
                {
                    var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                        businessId,
                        embeddingIntegrationElement,
                        "Embedding"
                    );
                    if (!validationBuildResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_EMBEDDING_INVALID",
                            "Configuration embedding invalid."
                        );
                    }

                    newKnowledgeBaseData.Configuration.Embedding = validationBuildResult.Data!;

                    if (postType == "edit")
                    {
                        if (newKnowledgeBaseData.Configuration.Embedding.Id != existingKbData!.Configuration.Embedding.Id)
                        {
                            isEmbeddingModelForEditChanged = true;
                        }
                        else
                        {
                            if (!newKnowledgeBaseData.Configuration.Embedding.FieldValues.OrderBy(x => x.Key).SequenceEqual(existingKbData.Configuration.Embedding.FieldValues.OrderBy(x => x.Key)))
                            {
                                isEmbeddingModelForEditChanged = true;
                            }
                        }

                    }
                }
            }

            // Retrival
            if (!configurationTabElement.TryGetProperty("retrieval", out var retrievalElement))
            {
                return result.SetFailureResult(
                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_NOT_FOUND",
                    "Configuration retrieval not found."
                );
            }
            else
            {
                // type enum KnowledgeBaseRetrievalType
                if (!retrievalElement.TryGetProperty("type", out var typeElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TYPE_NOT_FOUND",
                        "Configuration retrieval type not found."
                    );
                }
                if (!typeElement.TryGetInt32(out var typeValue))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TYPE_INVALID",
                        "Configuration retrieval type invalid."
                    );
                }
                if (!Enum.IsDefined(typeof(KnowledgeBaseRetrievalType), typeValue))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TYPE_INVALID_TYPE",
                        "Configuration retrieval type invalid type."
                    );
                }
                var retrievalType = (KnowledgeBaseRetrievalType)typeValue;

                if (retrievalType == KnowledgeBaseRetrievalType.VectorSearch)
                {
                    var vectorRetrival = new BusinessAppKnowledgeBaseConfigurationVectorRetrieval();

                    // TopK
                    if (!retrievalElement.TryGetProperty("topK", out var topKElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TOPK_NOT_FOUND",
                            "Configuration retrieval topK not found."
                        );
                    }
                    if (!topKElement.TryGetInt32(out var topKValue) || topKValue < 1 || topKValue > 10)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TOPK_INVALID",
                            "Configuration retrieval topK invalid. Minimum value is 1 and maximum value is 10."
                        );
                    }
                    vectorRetrival.TopK = topKValue;

                    // UseScoreThreshold
                    if (!retrievalElement.TryGetProperty("useScoreThreshold", out var useScoreThresholdElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_USESCORETHRESHOLD_NOT_FOUND",
                            "Configuration retrieval useScoreThreshold not found."
                        );
                    }
                    if (useScoreThresholdElement.ValueKind != JsonValueKind.True && useScoreThresholdElement.ValueKind != JsonValueKind.False)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_USESCORETHRESHOLD_INVALID",
                            "Configuration retrieval useScoreThreshold invalid."
                        );
                    }
                    vectorRetrival.UseScoreThreshold = useScoreThresholdElement.GetBoolean();

                    if (vectorRetrival.UseScoreThreshold)
                    {
                        // ScoreThreshold
                        if (!retrievalElement.TryGetProperty("scoreThreshold", out var scoreThresholdElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_SCORETHRESHOLD_NOT_FOUND",
                                "Configuration retrieval scoreThreshold not found."
                            );
                        }
                        if (!scoreThresholdElement.TryGetDouble(out var scoreThresholdValue) || scoreThresholdValue <= 0 || scoreThresholdValue > 1)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_SCORETHRESHOLD_INVALID",
                                "Configuration retrieval scoreThreshold invalid. Minimum value is 0.X and maximum value is 1."
                            );
                        }
                        vectorRetrival.ScoreThreshold = scoreThresholdValue;
                    }

                    // Rerank
                    if (!retrievalElement.TryGetProperty("rerank", out var rerankElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_NOT_FOUND",
                            "Configuration retrieval rerank not found."
                        );
                    }
                    else
                    {
                        // enabled
                        if (!rerankElement.TryGetProperty("enabled", out var rerankEnabledElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_ENABLED_NOT_FOUND",
                                "Configuration retrieval rerank enabled not found."
                            );
                        }
                        if (rerankEnabledElement.ValueKind != JsonValueKind.True && rerankEnabledElement.ValueKind != JsonValueKind.False)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_ENABLED_INVALID",
                                "Configuration retrieval rerank enabled invalid."
                            );
                        }
                        vectorRetrival.Rerank.Enabled = rerankEnabledElement.GetBoolean();

                        if (vectorRetrival.Rerank.Enabled)
                        {
                            // Integration
                            if (!rerankElement.TryGetProperty("integration", out var rerankIntegrationElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_INTEGRATION_NOT_FOUND",
                                    "Configuration retrieval rerank integration not found."
                                );
                            }

                            var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                                businessId,
                                rerankIntegrationElement,
                                "Rerank"
                            );
                            if (!validationBuildResult.Success)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_INTEGRATION_INVALID",
                                    "Configuration retrieval rerank integration invalid."
                                );
                            }

                            vectorRetrival.Rerank.Integration = validationBuildResult.Data;
                        }
                    }

                    newKnowledgeBaseData.Configuration.Retrieval = vectorRetrival;
                }
                else if (retrievalType == KnowledgeBaseRetrievalType.FullTextSearch)
                {
                    var fullTextRetrival = new BusinessAppKnowledgeBaseConfigurationFullTextRetrieval();

                    // topk
                    if (!retrievalElement.TryGetProperty("topK", out var topKElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TOPK_NOT_FOUND",
                            "Configuration retrieval topK not found."
                        );
                    }
                    if (!topKElement.TryGetInt32(out var topKValue) || topKValue < 1 || topKValue > 10)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TOPK_INVALID",
                            "Configuration retrieval topK invalid. Minimum value is 1 and maximum value is 10."
                        );
                    }
                    fullTextRetrival.TopK = topKValue;

                    // rerank
                    if (!retrievalElement.TryGetProperty("rerank", out var rerankElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_NOT_FOUND",
                            "Configuration retrieval rerank not found."
                        );
                    }
                    else
                    {
                        // enabled
                        if (!rerankElement.TryGetProperty("enabled", out var rerankEnabledElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_ENABLED_NOT_FOUND",
                                "Configuration retrieval rerank enabled not found."
                            );
                        }
                        fullTextRetrival.Rerank.Enabled = rerankEnabledElement.GetBoolean();

                        if (fullTextRetrival.Rerank.Enabled)
                        {
                            // Integration
                            if (!rerankElement.TryGetProperty("integration", out var rerankIntegrationElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_INTEGRATION_NOT_FOUND",
                                    "Configuration retrieval rerank integration not found."
                                );
                            }

                            var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                                businessId,
                                rerankIntegrationElement,
                                "Rerank"
                            );
                            if (!validationBuildResult.Success)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_INTEGRATION_INVALID",
                                    "Configuration retrieval rerank integration invalid."
                                );
                            }

                            fullTextRetrival.Rerank.Integration = validationBuildResult.Data;
                        }
                    }

                    newKnowledgeBaseData.Configuration.Retrieval = fullTextRetrival;
                }
                else if (retrievalType == KnowledgeBaseRetrievalType.HybirdSearch)
                {
                    var hybirdRetrival = new BusinessAppKnowledgeBaseConfigurationHybridRetrieval();

                    // Mode enum - KnowledgeBaseHybridRetrievalMode
                    if (!retrievalElement.TryGetProperty("mode", out var modeElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_MODE_MISSING",
                            "Configuration retrieval mode missing."
                        );
                    }
                    if (!modeElement.TryGetInt32(out var modeValue))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_MODE_INVALID",
                            "Configuration retrieval mode invalid."
                        );
                    }
                    if (!Enum.IsDefined(typeof(KnowledgeBaseHybridRetrievalMode), modeValue))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_MODE_INVALID_TYPE",
                            "Configuration retrieval mode invalid type."
                        );
                    }
                    var mode = (KnowledgeBaseHybridRetrievalMode)modeValue;

                    if (mode == KnowledgeBaseHybridRetrievalMode.WeightedScore)
                    {
                        // double Weight
                        if (!retrievalElement.TryGetProperty("weight", out var weightElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_WEIGHT_MISSING",
                                "Configuration retrieval weight missing."
                            );
                        }
                        if (!weightElement.TryGetDouble(out var weightValue) || weightValue < 0 || weightValue > 1)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_WEIGHT_INVALID",
                                "Configuration retrieval weight invalid. Minimum value is 0 and maximum value is 1."
                            );
                        }
                        hybirdRetrival.Weight = weightValue;
                    }
                    else if (mode == KnowledgeBaseHybridRetrievalMode.RerankModel)
                    {
                        // RerankIntegration
                        if (!retrievalElement.TryGetProperty("rerankIntegration", out var rerankElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_INTEGRATION_NOT_FOUND",
                                "Configuration retrieval rerank integration not found."
                            );
                        }

                        var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                            businessId,
                            rerankElement,
                            "Rerank"
                        );
                        if (!validationBuildResult.Success)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_RERANK_INTEGRATION_INVALID",
                                "Configuration retrieval rerank integration invalid."
                            );
                        }

                        hybirdRetrival.RerankIntegration = validationBuildResult.Data;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_MODE_INVALID_TYPE",
                            "Configuration retrieval mode invalid type."
                        );
                    }

                    // topk
                    if (!retrievalElement.TryGetProperty("topK", out var topKElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TOPK_MISSING",
                            "Configuration retrieval topk missing."
                        );
                    }
                    if (!topKElement.TryGetInt32(out var topkValue) || topkValue < 1 || topkValue > 10)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TOPK_INVALID",
                            "Configuration retrieval topk invalid. Minimum value is 1 and maximum value is 10."
                        );
                    }
                    hybirdRetrival.TopK = topkValue;

                    // UseScoreThreshold
                    if (!retrievalElement.TryGetProperty("useScoreThreshold", out var useScoreThresholdElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_USE_SCORE_THRESHOLD_MISSING",
                            "Configuration retrieval use score threshold missing."
                        );
                    }
                    if (useScoreThresholdElement.ValueKind != JsonValueKind.True && useScoreThresholdElement.ValueKind != JsonValueKind.False)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_USE_SCORE_THRESHOLD_INVALID",
                            "Configuration retrieval use score threshold invalid."
                        );
                    }
                    hybirdRetrival.UseScoreThreshold = useScoreThresholdElement.GetBoolean();

                    if (hybirdRetrival.UseScoreThreshold)
                    {
                        // scoreThreshold
                        if (!retrievalElement.TryGetProperty("scoreThreshold", out var scoreThresholdElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_SCORE_THRESHOLD_MISSING",
                                "Configuration retrieval score threshold missing."
                            );
                        }
                        if (!scoreThresholdElement.TryGetDouble(out var scoreThresholdValue) || scoreThresholdValue < 0 || scoreThresholdValue > 1)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_SCORE_THRESHOLD_INVALID",
                                "Configuration retrieval score threshold invalid. Minimum value is 0 and maximum value is 1."
                            );
                        }
                        hybirdRetrival.ScoreThreshold = scoreThresholdValue;
                    }

                    newKnowledgeBaseData.Configuration.Retrieval = hybirdRetrival;
                }
                else
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_TYPE_INVALID_TYPE",
                        "Configuration retrieval type invalid type."
                    );
                }
            }

            // Database Changes
            if (postType == "new")
            {
                newKnowledgeBaseData.Id = ObjectId.GenerateNewId().ToString();
                string collectionName = $"b{businessId}_kb{newKnowledgeBaseData.Id}";

                bool vectorCollectionCreated = await _documentVectorRepository.CreateCollectionAsync(collectionName, newKnowledgeBaseData.Configuration.Embedding.VectorSize, newKnowledgeBaseData.Configuration.Retrieval.Mode == KnowledgeBaseRetrievalType.HybirdSearch);
                if (!vectorCollectionCreated)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:VECTOR_COLLECTION_CREATION_FAILED",
                        "Failed to create vector database collection."
                    );
                }

                // Add the KB to the business app
                bool dbResult = await _businessAppRepository.AddKnowledgeBaseToArrayAsync(businessId, newKnowledgeBaseData);
                if (!dbResult)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:DATABASE_SAVE_FAILED",
                        "Failed to save new knowledge base to the database."
                    );
                }
            }
            else if (postType == "edit")
            {
                newKnowledgeBaseData.Id = existingKbData!.Id;
                newKnowledgeBaseData.Documents = existingKbData.Documents;

                bool dbResult = await _businessAppRepository.UpdateKnowledgeBaseInArrayAsync(businessId, newKnowledgeBaseData);
                if (!dbResult)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:DATABASE_UPDATE_FAILED",
                        "Failed to update knowledge base in the database."
                    );
                }

                if (isEmbeddingModelForEditChanged)
                {
                    // TODO RUN A BACKGROUND TASK TO UPDATE ALL DOCUMENTS
                }
            }

            return result.SetSuccessResult(newKnowledgeBaseData);
        }  

        public async Task<FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>> ProcessAndAddDocumentAsync(long businessId, string knowledgeBaseId, IFormCollection formData, IFormFile file)
        {
            var result = new FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>();

            // 1. Get KB Settings
            var businessApp = await _parentBusinessManager.GetUserBusinessAppById(businessId, "SYSTEM"); // Assuming a way to get the app
            var kb = businessApp.Data?.KnowledgeBases.FirstOrDefault(k => k.Id == knowledgeBaseId);
            if (kb == null)
                return result.SetFailureResult("ProcessDoc:1", "Knowledge Base not found.");

            // 2. Extract Text from File
            var textContent = await _unstructuredManager.ExtractTextAsync(file);
            if (string.IsNullOrWhiteSpace(textContent))
                return result.SetFailureResult("ProcessDoc:2", "Could not extract any text from the uploaded file.");

            // 3. Chunk the Text
            var chunks = ChunkText(textContent, kb.Configuration.Chunking);

            // 4. Get Embeddings for each chunk
            var vectorizedChunks = new List<KnowledgeBaseChunkModel>(); // Model for vector DB
            foreach (var chunk in chunks)
            {
                var embeddingVector = await _embeddingService.GetEmbeddingAsync(chunk.Text, kb.Configuration.Embedding);
                vectorizedChunks.Add(new KnowledgeBaseChunkModel { Id = chunk.Id, Vector = embeddingVector });
            }

            // 5. Add vectors to the Vector Database
            string collectionName = $"b{businessId}_kb{knowledgeBaseId}";
            await _documentVectorRepository.AddChunksAsync(collectionName, vectorizedChunks);

            // 6. Create and Save Document Metadata
            var newDocument = new BusinessAppKnowledgeBaseDocument
            {
                Id = await _knowledgeBaseDocumentRepository.GetNextDocumentId(),
                Name = file.FileName,
                Enabled = true,
                Status = KnowledgeBaseDocumentStatus.Ready,
                Chunks = chunks
            };

            bool docCreated = await _knowledgeBaseDocumentRepository.CreateDocumentAsync(newDocument);
            if (!docCreated)
                return result.SetFailureResult("ProcessDoc:3", "Failed to save document metadata.");

            // 7. Link Document ID to Knowledge Base
            await _businessAppRepository.AddDocumentIdToKnowledgeBaseAsync(businessId, knowledgeBaseId, newDocument.Id);

            result.Success = true;
            result.Data = newDocument;
            return result;
        }

        // This is a simplified helper. A real implementation would be more complex.
        private List<BusinessAppKnowledgeBaseDocumentChunk> ChunkText(string text, BusinessAppKnowledgeBaseChunking config)
        {
            var chunks = new List<BusinessAppKnowledgeBaseDocumentChunk>();
            // TODO: Implement the actual chunking logic based on config.Mode (General vs ParentChild)
            // For now, a simple split:
            var textChunks = text.Split(new[] { config.General.Delimiter }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var textChunk in textChunks)
            {
                chunks.Add(new BusinessAppKnowledgeBaseDocumentGeneralChunk { Id = ObjectId.GenerateNewId().ToString(), Text = textChunk });
            }
            return chunks;
        }

        // TODO: Implement other public methods like UpdateChunksAsync, TestRetrievalAsync, etc.
    }
}
