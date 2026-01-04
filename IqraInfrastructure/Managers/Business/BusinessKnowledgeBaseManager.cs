using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.RAG;
using IqraCore.Models.KnowledgeBase;
using IqraCore.Models.RAG;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.RAG.Extractors;
using IqraInfrastructure.Managers.RAG.Keywords;
using IqraInfrastructure.Managers.RAG.Processors;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.RAG;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessKnowledgeBaseManager
    {
        private readonly BusinessManager _parentBusinessManager;
        private readonly IMongoClient _mongoClient;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessKnowledgeBaseDocumentRepository _knowledgeBaseDocumentRepository;
        private readonly IntegrationConfigurationManager _integrationConfigurationManager;

        private readonly KnowledgeBaseVectorRepository _documentVectorRepository;
        private readonly IndexProcessorFactory _indexProcessorFactory;
        private readonly ExtractProcessor _extractProcessor;
        private readonly KeywordExtractor _keywordExtractor;

        private readonly EmbeddingProviderManager _embeddingProviderManager;
        private readonly RAGKeywordStore _ragKeywordStore;

        public BusinessKnowledgeBaseManager(
            BusinessManager parentBusinessManager,
            IMongoClient mongoClient,
            BusinessAppRepository businessAppRepository,
            BusinessKnowledgeBaseDocumentRepository knowledgeBaseDocumentRepository,
            IntegrationConfigurationManager integrationConfigurationManager,
            KnowledgeBaseVectorRepository documentVectorRepository,
            IndexProcessorFactory indexProcessorFactory,
            ExtractProcessor extractProcessor,
            KeywordExtractor keywordExtractor,
            EmbeddingProviderManager embeddingProviderManager,
            RAGKeywordStore ragKeywordStore
        )
        {
            _parentBusinessManager = parentBusinessManager;
            _mongoClient = mongoClient;
            _businessAppRepository = businessAppRepository;
            _knowledgeBaseDocumentRepository = knowledgeBaseDocumentRepository;
            _integrationConfigurationManager = integrationConfigurationManager;
            _documentVectorRepository = documentVectorRepository;
            _indexProcessorFactory = indexProcessorFactory;
            _extractProcessor = extractProcessor;
            _keywordExtractor = keywordExtractor;
            _embeddingProviderManager = embeddingProviderManager;
            _ragKeywordStore = ragKeywordStore;
        }

        // CURD
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
        public async Task<bool> CheckKnowledgeBaseGroupExistsById(long businessId, string linkedGroupId)
        {
            return await _businessAppRepository.CheckKnowledgeBaseGroupExistsById(businessId, linkedGroupId);
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

                var documents = await _knowledgeBaseDocumentRepository.GetDocumentsForKnowledgeBase(businessId, knowledgeBaseData.Id);
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
                        if (!maxLengthElement.TryGetInt32(out var maxLengthInt) || maxLengthInt < 1 || maxLengthInt > 4000)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_MAX_LENGTH_INVALID",
                                "Configuration chunking general max length invalid. Must be between 1 and 4000."
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
                        if (!overlapElement.TryGetInt32(out var overlapInt) || overlapInt < 0 || overlapInt > maxLengthInt)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_GENERAL_OVERLAP_INVALID",
                                "Configuration chunking general overlap invalid. Must be between 0 and max length."
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
                                if (string.IsNullOrEmpty(paragraphDelimiter))
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
                                if (!maxLengthElement.TryGetInt32(out var maxLengthInt) || maxLengthInt < 1 || maxLengthInt > 4000)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_PARENT_MAX_LENGTH_INVALID",
                                        "Configuration chunking parent max length invalid. Must be between 1 and 4000."
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
                            if (!maxLengthElement.TryGetInt32(out var maxLengthInt) || maxLengthInt < 1 || maxLengthInt > 4000)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_CHUNKING_CHILD_MAX_LENGTH_INVALID",
                                    "Configuration chunking child max length invalid. Must be between 1 and 4000."
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
            string? currentRerankIntegrationId = null;
            if (!configurationTabElement.TryGetProperty("retrieval", out var retrievalElement))
            {
                return result.SetFailureResult(
                    "AddOrUpdateKnowledgeBaseAsync:CONFIGURATION_RETRIEVAL_NOT_FOUND",
                    "Configuration retrieval not found."
                );
            }
            else
            {
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

                            vectorRetrival.Rerank.Integration = validationBuildResult.Data!;
                            currentRerankIntegrationId = vectorRetrival.Rerank.Integration.Id;
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

                            fullTextRetrival.Rerank.Integration = validationBuildResult.Data!;
                            currentRerankIntegrationId = fullTextRetrival.Rerank.Integration.Id;
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

                        hybirdRetrival.RerankIntegration = validationBuildResult.Data!;
                        currentRerankIntegrationId = hybirdRetrival.RerankIntegration.Id;
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
            using (var session = await _mongoClient.StartSessionAsync())
            {
                session.StartTransaction();

                try
                {
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
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:VECTOR_COLLECTION_CREATION_FAILED",
                                "Failed to create vector database collection."
                            );
                        }

                        // Add the KB to the business app
                        bool dbResult = await _businessAppRepository.AddKnowledgeBase(businessId, newKnowledgeBaseData, session);
                        if (!dbResult)
                        {
                            // may want to delete vectorCollectionCreated
                            var deleteResult = await _documentVectorRepository.DeleteKnowledgeBaseAsync(collectionName);
                            if (!deleteResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:DB_FAILED_AND_FALLBACK_VECTOR_COLLECTION_DELETION_FAILED",
                                    "Failed to delete vector database collection beause of database failure."
                                );
                            }

                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:DATABASE_SAVE_FAILED",
                                "Failed to save new knowledge base to the database."
                            );
                        }
                    }
                    else if (postType == "edit")
                    {
                        newKnowledgeBaseData.Id = existingKbData!.Id;

                        bool dbResult = await _businessAppRepository.UpdateKnowledgeBaseExceptDocumentsAndReferences(businessId, newKnowledgeBaseData, session);
                        if (!dbResult)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:DATABASE_UPDATE_FAILED",
                                "Failed to update knowledge base in the database."
                            );
                        }

                        if (existingKbData.Configuration.Embedding.Id != newKnowledgeBaseData.Configuration.Embedding.Id)
                        {
                            bool removeEmbeddingModelReferenceFromIntegration = await _businessAppRepository.RemoveKBEmbeddingModelReferenceFromIntegration(businessId, existingKbData.Configuration.Embedding.Id, newKnowledgeBaseData.Id, session);
                            if (!removeEmbeddingModelReferenceFromIntegration)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "AddOrUpdateKnowledgeBaseAsync:DATABASE_UPDATE_FAILED",
                                    "Failed to update knowledge base in the database."
                                );
                            }
                        }

                        if (existingKbData.Configuration.Retrieval is BusinessAppKnowledgeBaseConfigurationVectorRetrieval vectorSearchData)
                        {
                            if (vectorSearchData.Rerank.Enabled && vectorSearchData.Rerank.Integration!.Id != currentRerankIntegrationId) {
                                var removeRerankReferenceToIntegration = await _businessAppRepository.RemoveKBRerankReferenceFromIntegration(businessId, vectorSearchData.Rerank.Integration!.Id, newKnowledgeBaseData.Id, session);
                                if (!removeRerankReferenceToIntegration)
                                {
                                    await session.AbortTransactionAsync();
                                    return result.SetFailureResult(
                                        "AddOrUpdateKnowledgeBaseAsync:FAILED_TO_REMOVE_RERANK_REFERENCE_TO_INTEGRATION",
                                        "Failed to remove rerank reference to integration."
                                    );
                                }
                            }
                        }
                        else if (existingKbData.Configuration.Retrieval is BusinessAppKnowledgeBaseConfigurationFullTextRetrieval fullTextSearchData)
                        {
                            if (fullTextSearchData.Rerank.Enabled && fullTextSearchData.Rerank.Integration!.Id != currentRerankIntegrationId)
                            {
                                var removeRerankReferenceToIntegration = await _businessAppRepository.RemoveKBRerankReferenceFromIntegration(businessId, fullTextSearchData.Rerank.Integration!.Id, newKnowledgeBaseData.Id, session);
                                if (!removeRerankReferenceToIntegration)
                                {
                                    await session.AbortTransactionAsync();
                                    return result.SetFailureResult(
                                        "AddOrUpdateKnowledgeBaseAsync:FAILED_TO_REMOVE_RERANK_REFERENCE_TO_INTEGRATION",
                                        "Failed to remove rerank reference to integration."
                                    );
                                }
                            }
                        }
                        else if (existingKbData.Configuration.Retrieval is BusinessAppKnowledgeBaseConfigurationHybridRetrieval hybirdSearchData)
                        {
                            if (hybirdSearchData.Mode == KnowledgeBaseHybridRetrievalMode.RerankModel && hybirdSearchData.RerankIntegration!.Id != currentRerankIntegrationId)
                            {
                                var removeRerankReferenceToIntegration = await _businessAppRepository.RemoveKBRerankReferenceFromIntegration(businessId, hybirdSearchData.RerankIntegration!.Id, newKnowledgeBaseData.Id, session);
                                if (!removeRerankReferenceToIntegration)
                                {
                                    await session.AbortTransactionAsync();
                                    return result.SetFailureResult(
                                        "AddOrUpdateKnowledgeBaseAsync:FAILED_TO_REMOVE_RERANK_REFERENCE_TO_INTEGRATION",
                                        "Failed to remove rerank reference to integration."
                                    );
                                }
                            }
                        }
                    }

                    var addEmbeddingModelReferenceToIntegration = await _businessAppRepository.AddKBEmbeddingModelReferenceToIntegration(businessId, newKnowledgeBaseData.Configuration.Embedding.Id, newKnowledgeBaseData.Id, session);
                    if (!addEmbeddingModelReferenceToIntegration)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "AddOrUpdateKnowledgeBaseAsync:FAILED_TO_ADD_EMBEDDING_MODEL_REFERENCE_TO_INTEGRATION",
                            "Failed to add embedding model reference to integration."
                        );
                    }

                    if (!string.IsNullOrEmpty(currentRerankIntegrationId))
                    {
                        var addRerankReferenceToIntegration = await _businessAppRepository.AddKBRerankReferenceToIntegration(businessId, currentRerankIntegrationId, newKnowledgeBaseData.Id, session);
                        if (!addRerankReferenceToIntegration)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateKnowledgeBaseAsync:FAILED_TO_ADD_RERANK_REFERENCE_TO_INTEGRATION",
                                "Failed to add rerank reference to integration."
                            );
                        }
                    }

                    if (postType == "edit")
                    {
                        if (isEmbeddingModelForEditChanged)
                        {
                            // TODO RUN A BACKGROUND TASK TO UPDATE ALL DOCUMENTS
                        }
                    }

                    await session.CommitTransactionAsync();
                    return result.SetSuccessResult(newKnowledgeBaseData);
                }
                catch (Exception ex) {
                    await session.AbortTransactionAsync();
                    return result.SetFailureResult(
                        "AddOrUpdateKnowledgeBaseAsync:DB_EXCEPTION",
                        $"An error occurred in the database: {ex.Message}"
                    );
                }
            }
        }
        public async Task<FunctionReturnResult> DeleteKnowledgeBase(long businessId, BusinessAppKnowledgeBase knowledgeBaseData)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (knowledgeBaseData.AgentReferences.Count > 0)
                {
                    return result.SetFailureResult(
                        "DeleteKnowlegeBase:AGENT_REFERENCE_EXISTS",
                        "Cannot delete knowledge base with agent references."
                    );
                }

                var documentsInProgressCount = await _knowledgeBaseDocumentRepository.GetProcessingDocumentsCountsForKnowledegeBaseAsync(businessId, knowledgeBaseData.Id);
                if (documentsInProgressCount > 0)
                {
                    return result.SetFailureResult(
                        "DeleteKnowlegeBase:DOCUMENTS_IN_PROGRESS",
                        "Cannot delete knowledge base with documents in progress."
                    );
                }

                using (var session = await _mongoClient.StartSessionAsync())
                {
                    session.StartTransaction();

                    try
                    {
                        // REMOVE RERANK INTEGRATION REFERENCE
                        if (knowledgeBaseData.Configuration.Retrieval is BusinessAppKnowledgeBaseConfigurationVectorRetrieval vectorRetrieval)
                        {
                            if (vectorRetrieval.Rerank.Enabled)
                            {
                                var removeRerankReference = await _businessAppRepository.RemoveKBRerankReferenceFromIntegration(businessId, vectorRetrieval.Rerank.Integration!.Id, knowledgeBaseData.Id, session);
                                if (!removeRerankReference)
                                {
                                    await session.AbortTransactionAsync();
                                    return result.SetFailureResult(
                                        "DeleteKnowlegeBase:FAILED_TO_REMOVE_RERANK_INTEGRATION_REFERENCE",
                                        "Failed to remove rerank integration reference."
                                    );
                                }
                            }
                        }
                        else if (knowledgeBaseData.Configuration.Retrieval is BusinessAppKnowledgeBaseConfigurationFullTextRetrieval fullTextSearchData)
                        {
                            if (fullTextSearchData.Rerank.Enabled)
                            {
                                var removeRerankReference = await _businessAppRepository.RemoveKBRerankReferenceFromIntegration(businessId, fullTextSearchData.Rerank.Integration!.Id, knowledgeBaseData.Id, session);
                                if (!removeRerankReference)
                                {
                                    await session.AbortTransactionAsync();
                                    return result.SetFailureResult(
                                        "DeleteKnowlegeBase:FAILED_TO_REMOVE_RERANK_INTEGRATION_REFERENCE",
                                        "Failed to remove rerank integration reference."
                                    );
                                }
                            }
                        }
                        else if (knowledgeBaseData.Configuration.Retrieval is BusinessAppKnowledgeBaseConfigurationHybridRetrieval hybirdSearchData)
                        {
                            if (hybirdSearchData.Mode == KnowledgeBaseHybridRetrievalMode.RerankModel)
                            {
                                var removeRerankReference = await _businessAppRepository.RemoveKBRerankReferenceFromIntegration(businessId, hybirdSearchData.RerankIntegration!.Id, knowledgeBaseData.Id, session);
                                if (!removeRerankReference)
                                {
                                    await session.AbortTransactionAsync();
                                    return result.SetFailureResult(
                                        "DeleteKnowlegeBase:FAILED_TO_REMOVE_RERANK_INTEGRATION_REFERENCE",
                                        "Failed to remove rerank integration reference."
                                    );
                                }
                            }
                        }

                        // REMOVE EMBEDDING MODEL REFERENCE
                        var removeLLMIntegrationReference = await _businessAppRepository.RemoveKBEmbeddingModelReferenceFromIntegration(businessId, knowledgeBaseData.Configuration.Embedding.Id, knowledgeBaseData.Id, session);
                        if (!removeLLMIntegrationReference)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "DeleteKnowlegeBase:FAILED_TO_REMOVE_EMBEDDING_MODEL_REFERENCE_TO_INTEGRATION",
                                "Failed to remove embedding model reference to integration."
                            );
                        }

                        // REMOVE KNOWLEDGE BASE
                        var removeKb = await _businessAppRepository.RemoveKnowledgeBase(businessId, knowledgeBaseData.Id, session);
                        if (!removeKb)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "DeleteKnowlegeBase:FAILED_TO_REMOVE_KNOWLEDGE_BASE",
                                "Failed to remove knowledge base."
                            );
                        }

                        // REMOVE KNOWLEDGE BASE DOCUMENTS
                        var removeKbDocuments = await _knowledgeBaseDocumentRepository.RemoveDocumentsForKnowledgeBase(businessId, knowledgeBaseData.Id, session);
                        if (!removeKbDocuments)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "DeleteKnowlegeBase:FAILED_TO_REMOVE_KNOWLEDGE_BASE_DOCUMENTS",
                                "Failed to remove knowledge base documents."
                            );
                        }

                        // REMOVE RAG KEYWORDS FOR KNOWLEDGE BASE
                        var removeRagKeywords = await _ragKeywordStore.RemoveKeywordsForKnowledgeBaseAsync(knowledgeBaseData.Id, session);
                        if (!removeRagKeywords)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "DeleteKnowlegeBase:FAILED_TO_REMOVE_RAG_KEYWORDS",
                                "Failed to remove rag keywords."
                            );
                        }

                        // DELETE VECTOR DB
                        string collectionName = $"b{businessId}_kb{knowledgeBaseData.Id}";
                        var deleteVectorDBCollection = await _documentVectorRepository.DeleteKnowledgeBaseAsync(collectionName);
                        if (!deleteVectorDBCollection)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "DeleteKnowlegeBase:FAILED_TO_DELETE_VECTOR_DB_COLLECTION",
                                "Failed to delete vector db collection."
                            );
                        }

                        await session.CommitTransactionAsync();
                        return result.SetSuccessResult();
                    }
                    catch (Exception ex)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "DeleteKnowlegeBase:DB_EXCEPTION",
                            $"An error occurred in the database: {ex.Message}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteKnowlegeBase:DB_EXCEPTION",
                    $"An error occurred in the database: {ex.Message}"
                );
            }
        }

        // PROCESSING MANAGEMENT
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
                BusinessId = businessId,
                KnowledgeBaseId = knowledgeBaseId,
                Enabled = true,
                Status = KnowledgeBaseDocumentStatus.Processing,
                Chunks = new List<BusinessAppKnowledgeBaseDocumentChunk>()
            };


            // TURN BELOW INTO MONGO SESSION
            using (var session = await _mongoClient.StartSessionAsync())
            {
                session.StartTransaction();

                try
                {
                    bool docCreated = await _knowledgeBaseDocumentRepository.CreateDocumentAsync(newDocument, session);
                    if (!docCreated)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "ProcessAndAddDocumentAsync:DOCUMENT_CREATION_FAILED",
                            "Failed to save document metadata."
                        );
                    }

                    bool docAddedToKb = await _businessAppRepository.AddDocumentIdToKnowledgeBaseAsync(businessId, knowledgeBaseId, newDocument.Id, session);
                    if (!docAddedToKb)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "ProcessAndAddDocumentAsync:DOCUMENT_ADDITION_FAILED",
                            "Failed to add document to knowledge base."
                        );
                    }

                    await session.CommitTransactionAsync();
                }
                catch (Exception ex) {
                    await session.AbortTransactionAsync();
                    return result.SetFailureResult(
                        "ProcessAndAddDocumentAsync:DB_EXCEPTION",
                        $"An error occurred in the database: {ex.Message}"
                    );
                }
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
                    await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(businessId, kb.Id, knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, "No embedding integration found in business app.");
                    return;
                }

                tempFilePath = Path.Combine(Path.GetTempPath(), ObjectId.GenerateNewId().ToString() + Path.GetExtension(file.FileName));
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var extractor = _extractProcessor.GetExtractor(tempFilePath);
                List<ExtractorDocumentModel> extractedDocuments = await extractor.ExtractAsync();
                if (extractedDocuments == null || !extractedDocuments.Any())
                {
                    await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(businessId, kb.Id, knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, "Extraction resulted in no content.");
                    return;
                }

                IIndexProcessor indexProcessor = _indexProcessorFactory.Create(kb);

                List<ProcessedDocumentChunkModel> processedChunks = await indexProcessor.TransformAsync(extractedDocuments, kb, knowledgeBaseDocument.Id);
                if (processedChunks == null || !processedChunks.Any())
                {
                    await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(businessId, kb.Id, knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, "Transformation resulted in no processable chunks.");
                    return;
                }

                var result = await indexProcessor.LoadAsync(processedChunks, kb, knowledgeBaseDocument, embeddingIntegrationData, businessId);
                if (!result.Success)
                {
                    await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(businessId, kb.Id, knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, $"[{result.Code}]: {result.Message}");
                    return;
                }

                await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(businessId, kb.Id, knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Ready);
            }
            catch (Exception ex)
            {
                await _knowledgeBaseDocumentRepository.UpdateDocumentStatusAsync(businessId, kb.Id, knowledgeBaseDocument.Id, KnowledgeBaseDocumentStatus.Failed, $"Processing failed: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempFilePath) && File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        public async Task<FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>> UpdateKnowledgeBaseDocumentChunksAsync(long businessId, string knowledgeBaseId, long documentId, IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppKnowledgeBaseDocument?>();

            try
            {
                if (!formData.TryGetValue("changes", out var changesJsonString))
                {
                    return result.SetFailureResult(
                        "UpdateKnowledgeBaseDocumentChunksAsync:MISSING_CHANGES_DATA",
                        "Changes not found in form data."
                    );
                }

                KnowledgeBaseDocumentUpdateChunksModel documentUpdateChunksModel;
                try
                {
                    documentUpdateChunksModel = JsonSerializer.Deserialize<KnowledgeBaseDocumentUpdateChunksModel>(changesJsonString.ToString(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                }
                catch (Exception ex)
                {
                    return result.SetFailureResult(
                        "UpdateKnowledgeBaseDocumentChunksAsync:CHANGES_DESERIALIZATION_ERROR",
                        $"Invalid changes data format: {ex.Message}"
                    );
                }

                if (documentUpdateChunksModel.Added.Count == 0 && documentUpdateChunksModel.Edited.Count == 0 && documentUpdateChunksModel.Deleted.Count == 0)
                {
                    return result.SetFailureResult(
                        "UpdateKnowledgeBaseDocumentChunksAsync:CHANGES_EMPTY",
                        "Changes data is empty. No added, edited or deleted chunks found."
                    );
                }

                var knowledgeBaseData = await _businessAppRepository.GetBusinessAppKnowledgeBaseAsync(businessId, knowledgeBaseId);
                if (knowledgeBaseData == null)
                {
                    return result.SetFailureResult(
                        "UpdateKnowledgeBaseDocumentChunksAsync:KNOWLEDGE_BASE_NOT_FOUND",
                        "Knowledge base not found."
                    );
                }

                if (!knowledgeBaseData.Documents.Contains(documentId))
                {
                    return result.SetFailureResult(
                        "UpdateKnowledgeBaseDocumentChunksAsync:DOCUMENT_NOT_FOUND",
                        "Document not found in knowledge base."
                    );
                }

                var knowledgeBaseDocumentData = await _knowledgeBaseDocumentRepository.GetDocumentByIdAsync(businessId, knowledgeBaseId, documentId);
                if (knowledgeBaseDocumentData == null)
                {
                    return result.SetFailureResult(
                        "UpdateKnowledgeBaseDocumentChunksAsync:DOCUMENT_NOT_FOUND",
                        "Document not found."
                    );
                }
                var originalDocumentChunks = knowledgeBaseDocumentData.Chunks.ToList();

                // Build In-Memory Lookups for fast validation.
                var existingChunksMap = originalDocumentChunks.ToDictionary(c => c.Id);
                var deletedChunkIds = new HashSet<string>(documentUpdateChunksModel.Deleted);
                var editedChunksMap = documentUpdateChunksModel.Edited.ToDictionary(c => c.Id);
                var addedChunksMap = documentUpdateChunksModel.Added.ToDictionary(c => c.Id);

                // Check for overlapping operations
                foreach (var id in deletedChunkIds)
                {
                    if (editedChunksMap.ContainsKey(id))
                    {
                        return result.SetFailureResult(
                            "UpdateKnowledgeBaseDocumentChunksAsync:CONFLICT",
                            $"Chunk {id} cannot be both deleted and edited."
                        );
                    }
                }

                // Validate that edited/deleted chunks actually exist
                foreach (var id in editedChunksMap.Keys.Concat(deletedChunkIds))
                {
                    if (!existingChunksMap.ContainsKey(id))
                    {
                        return result.SetFailureResult(
                            "UpdateKnowledgeBaseDocumentChunksAsync:CHUNK_NOT_FOUND",
                            $"Chunk {id} not found. The data might be stale."
                        );
                    }
                }

                // Validate Parent-Child integrity
                foreach (var deletedId in deletedChunkIds)
                {
                    if (existingChunksMap[deletedId] is BusinessAppKnowledgeBaseDocumentParentChunk parentChunk)
                    {
                        foreach (var childId in parentChunk.ChildrenIds)
                        {
                            if (editedChunksMap.ContainsKey(childId))
                            {
                                return result.SetFailureResult(
                                    "UpdateKnowledgeBaseDocumentChunksAsync:PARENT_CHILD_CONFLICT",
                                    $"Cannot edit child chunk {childId} because its parent {deletedId} is being deleted."
                                );
                            }
                        }
                    }
                }

                foreach (var addedChunk in documentUpdateChunksModel.Added)
                {
                    if (addedChunk.Type == KnowledgeBaseDocumentType.Child)
                    {
                        if (string.IsNullOrEmpty(addedChunk.ParentId))
                        {
                            return result.SetFailureResult(
                                "UpdateKnowledgeBaseDocumentChunksAsync:CHILD_MISSING_PARENT",
                                $"Added child chunk {addedChunk.Id} is missing a ParentId."
                            );
                        }

                        bool parentExists = (existingChunksMap.ContainsKey(addedChunk.ParentId) && !deletedChunkIds.Contains(addedChunk.ParentId)) ||
                                            (addedChunksMap.ContainsKey(addedChunk.ParentId));
                        if (!parentExists)
                        {
                            return result.SetFailureResult(
                                "UpdateKnowledgeBaseDocumentChunksAsync:CHILD_PARENT_NOT_FOUND",
                                $"Parent {addedChunk.ParentId} for new child chunk {addedChunk.Id} does not exist or is being deleted."
                            );
                        }
                    }
                }

                // Prepare MongoDB and Vector DB Payloads
                var finalMongoChunks = new List<BusinessAppKnowledgeBaseDocumentChunk>();
                var chunksToUpsertInVectorDB = new List<VectorKnowledgeBaseChunkModel>();
                var chunkIdsToDeleteFromVectorDB = new HashSet<string>(deletedChunkIds);
                var frontendToBackendIdMap = new Dictionary<string, string>();
                var keywordsForAddedChunks = new Dictionary<string, List<string>>();
                var keywordsForEditedChunks = new List<(string chunkId, List<string> oldKeywords, List<string> newKeywords)>();

                // Process existing chunks (keep, edit, or implicitly delete)
                foreach (var chunk in originalDocumentChunks)
                {
                    if (deletedChunkIds.Contains(chunk.Id))
                    {
                        // If a parent is deleted, all its children must also be deleted from the vector DB.
                        if (chunk is BusinessAppKnowledgeBaseDocumentParentChunk parent)
                        {
                            foreach (var childId in parent.ChildrenIds) chunkIdsToDeleteFromVectorDB.Add(childId);
                        }
                        continue; // Skip adding to the final list
                    }

                    if (chunk is BusinessAppKnowledgeBaseDocumentParentChunk parentChunk)
                    {
                        var updatedChildrenIds = parentChunk.ChildrenIds
                            .Where(childId => !deletedChunkIds.Contains(childId))
                            .ToList();
                        parentChunk.ChildrenIds = updatedChildrenIds;
                    }

                    if (editedChunksMap.TryGetValue(chunk.Id, out var editedChunkData))
                    {
                        string originalText = chunk.Text;
                        chunk.Text = editedChunkData.Text;

                        keywordsForEditedChunks.Add((
                            chunk.Id,
                            _keywordExtractor.Extract(originalText),
                            _keywordExtractor.Extract(chunk.Text)
                        ));

                        chunksToUpsertInVectorDB.Add(new VectorKnowledgeBaseChunkModel
                        {
                            ChunkId = chunk.Id,
                            DocumentId = documentId,
                            TextChunk = chunk.Text,
                            ParentChunkId = (chunk is BusinessAppKnowledgeBaseDocumentChildChunk child) ? child.ParentId : null
                        });
                    }
                    finalMongoChunks.Add(chunk);
                }

                // Process newly added chunks
                // Add General and Parent chunks first to generate their backend IDs
                foreach (var addedChunk in documentUpdateChunksModel.Added.Where(c => c.Type != KnowledgeBaseDocumentType.Child))
                {
                    var newChunkId = ObjectId.GenerateNewId().ToString();
                    frontendToBackendIdMap[addedChunk.Id] = newChunkId;
                    BusinessAppKnowledgeBaseDocumentChunk newMongoChunk;

                    if (addedChunk.Type == KnowledgeBaseDocumentType.General)
                    {
                        newMongoChunk = new BusinessAppKnowledgeBaseDocumentGeneralChunk
                        {
                            Id = newChunkId,
                            Text = addedChunk.Text
                        };
                    }
                    else // Parent
                    {
                        newMongoChunk = new BusinessAppKnowledgeBaseDocumentParentChunk
                        {
                            Id = newChunkId,
                            Text = addedChunk.Text,
                            ChildrenIds = new List<string>()
                        };
                    }

                    finalMongoChunks.Add(newMongoChunk);

                    // Only add general chunk as parent chunk is only for context
                    if (addedChunk.Type == KnowledgeBaseDocumentType.General)
                    {
                        chunksToUpsertInVectorDB.Add(new VectorKnowledgeBaseChunkModel
                        {
                            ChunkId = newChunkId,
                            DocumentId = documentId,
                            TextChunk = addedChunk.Text,
                            ParentChunkId = null // General chunks have no parent
                        });
                    }
                }

                // Add Child chunks, linking them to existing or new parents
                foreach (var addedChunk in documentUpdateChunksModel.Added.Where(c => c.Type == KnowledgeBaseDocumentType.Child))
                {
                    var newChunkId = ObjectId.GenerateNewId().ToString();
                    frontendToBackendIdMap[addedChunk.Id] = newChunkId;

                    // Find the backend ID for the parent
                    string backendParentId;
                    if (!frontendToBackendIdMap.TryGetValue(addedChunk.ParentId, out backendParentId))
                    {
                        backendParentId = addedChunk.ParentId; // It must be an existing parent
                    }

                    var newChildChunk = new BusinessAppKnowledgeBaseDocumentChildChunk
                    {
                        Id = newChunkId,
                        Text = addedChunk.Text,
                        ParentId = backendParentId
                    };
                    finalMongoChunks.Add(newChildChunk);

                    // Find the parent (new or existing) in the final list and add this child's ID
                    var parentInFinalList = finalMongoChunks.FirstOrDefault(c => c.Id == backendParentId) as BusinessAppKnowledgeBaseDocumentParentChunk;
                    parentInFinalList?.ChildrenIds.Add(newChunkId);

                    chunksToUpsertInVectorDB.Add(new VectorKnowledgeBaseChunkModel
                    {
                        ChunkId = newChunkId,
                        DocumentId = documentId,
                        TextChunk = addedChunk.Text,
                        ParentChunkId = backendParentId
                    });
                }

                // Extract keywords for added chunks that will be indexed
                foreach (var addedChunk in documentUpdateChunksModel.Added)
                {
                    if (addedChunk.Type == KnowledgeBaseDocumentType.General || addedChunk.Type == KnowledgeBaseDocumentType.Child)
                    {
                        var backendId = frontendToBackendIdMap[addedChunk.Id];
                        keywordsForAddedChunks[backendId] = _keywordExtractor.Extract(addedChunk.Text);
                    }
                }

                // Generate Embeddings (Critical failure point before the saga)
                if (chunksToUpsertInVectorDB.Any())
                {
                    var embeddingIntegrationResult = await _parentBusinessManager.GetIntegrationsManager().getBusinessIntegrationById(businessId, knowledgeBaseData.Configuration.Embedding.Id);
                    if (!embeddingIntegrationResult.Success)
                    {
                        return result.SetFailureResult(
                            $"UpdateKnowledgeBaseDocumentChunksAsync:{embeddingIntegrationResult.Code}",
                            embeddingIntegrationResult.Message
                        );
                    }

                    var embeddingProviderResult = await _embeddingProviderManager.BuildProviderServiceByIntegration(embeddingIntegrationResult.Data!, knowledgeBaseData.Configuration.Embedding);
                    if (!embeddingProviderResult.Success)
                    {
                        return result.SetFailureResult(
                            $"UpdateKnowledgeBaseDocumentChunksAsync:{embeddingProviderResult.Code}",
                            embeddingProviderResult.Message
                        );
                    }

                    var textsToEmbed = chunksToUpsertInVectorDB.Select(c => c.TextChunk).ToList();
                    var vectorsResult = await embeddingProviderResult.Data!.GenerateEmbeddingForTextListAsync(textsToEmbed);
                    if (!vectorsResult.Success)
                    {
                        return result.SetFailureResult(
                            $"UpdateKnowledgeBaseDocumentChunksAsync:{vectorsResult.Code}",
                            vectorsResult.Message
                        );
                    }

                    // Populate vectors back into the payload
                    for (int i = 0; i < chunksToUpsertInVectorDB.Count; i++)
                    {
                        chunksToUpsertInVectorDB[i].Embedding = new ReadOnlyMemory<float>(vectorsResult.Data![i]);
                    }
                }

                // EXECUTE THE TRANSACTION WITH COMPENSATION LOGIC (THE SAGA)
                using (var session = await _mongoClient.StartSessionAsync())
                {
                    session.StartTransaction();

                    try
                    {
                        // Execute MongoDB Operation (Single atomic update)
                        var mongoFilter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.And(
                            Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.BusinessId, businessId),
                            Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.KnowledgeBaseId, knowledgeBaseId),
                            Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId)
                        );
                        var mongoUpdate = Builders<BusinessAppKnowledgeBaseDocument>.Update.Set(d => d.Chunks, finalMongoChunks);
                        var updateResult = await _knowledgeBaseDocumentRepository.UpdateDocumentWithUpdateDefinition(businessId, knowledgeBaseId, documentId, mongoUpdate, session);
                        if (!updateResult)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "UpdateKnowledgeBaseDocumentChunksAsync:MONGODB_FAILED",
                                "Failed to update document in MongoDB. The transaction will be aborted, reverting vector database changes."
                            );
                        }

                        // Update Keyword Store
                        if (chunkIdsToDeleteFromVectorDB.Any())
                        {
                            await _ragKeywordStore.RemoveChunkReferencesAsync(knowledgeBaseId, chunkIdsToDeleteFromVectorDB.ToList(), session);
                        }
                        foreach (var edited in keywordsForEditedChunks)
                        {
                            await _ragKeywordStore.UpdateChunkKeywordsAsync(knowledgeBaseId, edited.chunkId, edited.oldKeywords, edited.newKeywords, session);
                        }
                        if (keywordsForAddedChunks.Any())
                        {
                            await _ragKeywordStore.AddChunksKeywordsAsync(knowledgeBaseId, keywordsForAddedChunks, session);
                        }

                        // Execute Vector DB Operations
                        string collectionName = $"b{businessId}_kb{knowledgeBaseId}";
                        if (chunkIdsToDeleteFromVectorDB.Any())
                        {
                            var deleteSuccess = await _documentVectorRepository.DeleteChunksAsync(collectionName, chunkIdsToDeleteFromVectorDB.ToList());
                            if (!deleteSuccess)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "UpdateKnowledgeBaseDocumentChunksAsync:VECTORDB_FAILED",
                                    "Failed to delete chunks from vector database. The transaction will be aborted, reverting MongoDB changes."
                                );
                            }
                        }
                        if (chunksToUpsertInVectorDB.Any())
                        {
                            var addSuccess = await _documentVectorRepository.AddChunksAsync(collectionName, chunksToUpsertInVectorDB);
                            if (!addSuccess)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "UpdateKnowledgeBaseDocumentChunksAsync:VECTORDB_FAILED",
                                    "Failed to add chunks to vector database. The transaction will be aborted, reverting MongoDB changes."
                                );
                            }
                        }

                        // Commit Mongo Changes
                        await session.CommitTransactionAsync();

                        // If all operations succeed, we're done.
                        var finalDocument = await _knowledgeBaseDocumentRepository.GetDocumentByIdAsync(businessId, knowledgeBaseId, documentId);
                        return result.SetSuccessResult(finalDocument);
                    }
                    catch (Exception ex)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "UpdateKnowledgeBaseDocumentChunksAsync:SAGA_FAILED",
                            $"An error occurred: {ex.Message}. The transaction will be aborted, reverting MongoDB changes. The vector database may be inconsistent."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "UpdateKnowledgeBaseDocumentChunks:EXCEPTION",
                    ex.Message
                );
            }
        }
    }
}
