using Google.Apis.Http;
using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Models.RAG;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Managers.RAG.Extractors;
using IqraInfrastructure.Managers.RAG.Processors;
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

        private readonly KnowledgeBaseVectorRepository _documentVectorRepository;
        private readonly IndexProcessorFactory _indexProcessorFactory;
        private readonly ExtractProcessor _extractProcessor;

        public BusinessKnowledgeBaseManager(
            BusinessManager parentBusinessManager,
            BusinessAppRepository businessAppRepository,
            BusinessKnowledgeBaseDocumentRepository knowledgeBaseDocumentRepository,
            IntegrationConfigurationManager integrationConfigurationManager,
            KnowledgeBaseVectorRepository documentVectorRepository,
            IndexProcessorFactory indexProcessorFactory,
            ExtractProcessor extractProcessor
        )
        {
            _parentBusinessManager = parentBusinessManager;
            _businessAppRepository = businessAppRepository;
            _knowledgeBaseDocumentRepository = knowledgeBaseDocumentRepository;
            _integrationConfigurationManager = integrationConfigurationManager;
            _documentVectorRepository = documentVectorRepository;
            _indexProcessorFactory = indexProcessorFactory;
            _extractProcessor = extractProcessor;
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
                    if (!chunkingElement.TryGetProperty("type", out var chunkingTypeElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_MODE_NOT_FOUND",
                            "Configuration chunking mode not found."
                        );
                    }
                    if (!chunkingTypeElement.TryGetInt32(out var chunkingTypeInt))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_MODE_INVALID",
                            "Configuration chunking mode invalid."
                        );
                    }
                    if (!Enum.IsDefined(typeof(KnowledgeBaseChunkingType), chunkingTypeInt))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_MODE_ENUM_INVALID",
                            "Configuration chunking mode enum invalid."
                        );
                    }

                    var chunkingType = (KnowledgeBaseChunkingType)chunkingTypeInt;
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
                        if (!chunkingElement.TryGetProperty("delimiter", out var delimiterElement))
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
                        if (!chunkingElement.TryGetProperty("maxLength", out var maxLengthElement))
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
                        if (!chunkingElement.TryGetProperty("overlap", out var overlapElement))
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
                        if (!chunkingElement.TryGetProperty("preprocess", out var preprocessElement))
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
                        if (!chunkingElement.TryGetProperty("parent", out var parentElement))
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
                        if (!chunkingElement.TryGetProperty("child", out var childElement))
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
                        if (!chunkingElement.TryGetProperty("preprocess", out var preprocessElement))
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

                bool vectorCollectionCreated = await _documentVectorRepository.CreateCollectionAsync(
                    collectionName,
                    (int)newKnowledgeBaseData.Configuration.Embedding.FieldValues["model_vector_dimension"],
                    newKnowledgeBaseData.Configuration.Retrieval.Type == KnowledgeBaseRetrievalType.HybirdSearch
                );
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

            var businessApp = await _parentBusinessManager.GetUserBusinessAppById(businessId, "SYSTEM");
            if (!businessApp.Success)
            {
                return result.SetFailureResult(
                    "ProcessAndAddDocumentAsync:BUSINESS_APP_NOT_FOUND",
                    "Business app not found."
                );
            }

            var kb = businessApp.Data?.KnowledgeBases.FirstOrDefault(k => k.Id == knowledgeBaseId);
            if (kb == null)
            {
                return result.SetFailureResult(
                    "ProcessAndAddDocumentAsync:KNOWLEDGE_BASE_NOT_FOUND",
                    "Knowledge Base not found in business data."
                );
            }

            // TODO: Validate File

            var newDocument = new BusinessAppKnowledgeBaseDocument
            {
                Id = await _knowledgeBaseDocumentRepository.GetNextDocumentId(),
                Name = file.FileName,
                Enabled = true,
                Status = KnowledgeBaseDocumentStatus.Processing,
                Chunks = new List<BusinessAppKnowledgeBaseDocumentChunk>()
            };


            // TURN BELOW INTO MONGO SESSION
            bool docCreated = await _knowledgeBaseDocumentRepository.CreateDocumentAsync(newDocument);
            if (!docCreated)
            {
                return result.SetFailureResult(
                    "ProcessAndAddDocumentAsync:DOCUMENT_CREATION_FAILED",
                    "Failed to save document metadata."
                );
            }
            bool docAddedToKb = await _businessAppRepository.AddDocumentIdToKnowledgeBaseAsync(businessId, knowledgeBaseId, newDocument.Id);
            if (!docAddedToKb)
            {
                return result.SetFailureResult(
                    "ProcessAndAddDocumentAsync:DOCUMENT_ADDITION_FAILED",
                    "Failed to add document to knowledge base."
                );
            }

            // Run Background Processing Task
            _ = ProcessDocumentBackgroundAsync(file, businessId, kb, newDocument, businessApp.Data!);

            return result.SetSuccessResult(newDocument);
        }

        private async Task ProcessDocumentBackgroundAsync(IFormFile file, long businessId, BusinessAppKnowledgeBase kb, BusinessAppKnowledgeBaseDocument knowledgeBaseDocument, BusinessApp businessApp)
        {
            string? tempFilePath = null;

            try
            {
                var embeddingIntegrationData = businessApp.Integrations.Find(integration => integration.Id == kb.Configuration.Embedding.Id);
                if (embeddingIntegrationData == null)
                {
                    await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, "No embedding integration found in business app.");
                    return;
                }
           
                tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var extractor = _extractProcessor.GetExtractor(tempFilePath);
                List<ExtractorDocumentModel> extractedDocuments = await extractor.ExtractAsync();
                if (extractedDocuments == null || !extractedDocuments.Any())
                {
                    await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, "Extraction resulted in no content.");
                    return;
                }

                IIndexProcessor indexProcessor = _indexProcessorFactory.Create(kb);

                List<ProcessedDocumentChunkModel> processedChunks = await indexProcessor.TransformAsync(extractedDocuments, kb, knowledgeBaseDocument.Id);
                if (processedChunks == null || !processedChunks.Any())
                {
                    await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, "Transformation resulted in no processable chunks.");
                    return;
                }

                var result = await indexProcessor.LoadAsync(processedChunks, kb, knowledgeBaseDocument, embeddingIntegrationData, businessId);
                if (!result.Success)
                {
                    await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, $"[{result.Code}]: {result.Message}");
                    return;
                }

                await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Ready);
            }
            catch (Exception ex)
            {
                await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, $"Processing failed: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempFilePath) && File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        public async Task<FunctionReturnResult<List<BusinessAppKnowledgeBaseDocument>?>> GetKnowledgeBaseDocuments(long businessId, string kbId)
        {
            var result = new FunctionReturnResult<List<BusinessAppKnowledgeBaseDocument>?>();

            try
            {
                var knowledgeBaseData = await _businessAppRepository.GetBusinessAppKnowledgeBaseAsync(businessId, kbId);
                if (knowledgeBaseData == null)
                {
                    return result.SetFailureResult(
                        "GetKnowledgeBaseDocuments:KNOWLEDGE_BASE_NOT_FOUND",
                        "Knowledge base not found."
                    );
                }

                var documents = await _knowledgeBaseDocumentRepository.GetDocumentsByIdsAsync(knowledgeBaseData.Documents);
                if (documents == null)
                {
                    return result.SetFailureResult(
                        "GetKnowledgeBaseDocuments:DOCUMENTS_NOT_FOUND",
                        "Documents not found."
                    );
                }

                if (documents.Count != knowledgeBaseData.Documents.Count)
                {
                    return result.SetFailureResult(
                        "GetKnowledgeBaseDocuments:DOCUMENTS_INCOMPLETE_COUNT",
                        "Incomplete document count."
                    );
                }

                return result.SetSuccessResult(documents);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetKnowledgeBaseDocuments:EXCEPTION",
                    ex.Message
                );
            }
        }

        // TODO: Implement other public methods like UpdateChunksAsync, TestRetrievalAsync, etc.
    }
}
