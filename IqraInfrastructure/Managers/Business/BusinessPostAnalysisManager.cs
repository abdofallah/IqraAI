using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessPostAnalysisManager
    {
        private BusinessManager _parent;
        private IMongoClient _mongoClient;
        private BusinessAppRepository _businessAppRepository;
        private IntegrationConfigurationManager _integrationConfigurationManager;

        private const int MAX_PCA_TAG_LEVELS = 5;
        private const int MAX_PCA_TAGS_PER_LEVEL = 5;
        private const int MAX_PCA_EXTRACTION_LEVELS = 5;
        private const int MAX_PCA_FIELDS_PER_LEVEL = 5;
        private const int MAX_PCA_RULES_PER_FIELD = 5;

        public BusinessPostAnalysisManager(
            BusinessManager businessManager,
            IMongoClient mongoClient,
            BusinessAppRepository businessAppRepository,
            IntegrationConfigurationManager integrationConfigurationManager
        ) {
            _parent = businessManager;
            _mongoClient = mongoClient;
            _businessAppRepository = businessAppRepository;
            _integrationConfigurationManager = integrationConfigurationManager;
        }

        public async Task<FunctionReturnResult<BusinessAppPostAnalysis?>> GetTemplateById(long businessId, string templateId)
        {
            var result = new FunctionReturnResult<BusinessAppPostAnalysis?>();
            try
            {
                var data = await _businessAppRepository.GetBusinessPostAnalysisTemplateById(businessId, templateId);
                if (data == null)
                {
                    return result.SetFailureResult(
                        "GetTemplateById:NOT_FOUND",
                        "Post Analysis Template not found."
                    );
                }
                return result.SetSuccessResult(data);
            }
            catch (Exception ex)
            {
                // Log the exception
                return result.SetFailureResult(
                    "GetTemplateById:EXCEPTION",
                    $"An error occurred: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<BusinessAppPostAnalysis?>> AddOrUpdateTemplateAsync(long businessId, IFormCollection formData, string postType, BusinessAppPostAnalysis? existingTemplateData)
        {
            var result = new FunctionReturnResult<BusinessAppPostAnalysis?>();

            try
            {
                if (!formData.TryGetValue("changes", out var changesJsonString) || string.IsNullOrWhiteSpace(changesJsonString))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTemplate:CHANGES_MISSING",
                        "Changes data not found in form."
                    );
                }

                JsonDocument changes;
                try
                {
                    changes = JsonDocument.Parse(changesJsonString!);
                }
                catch (JsonException ex)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTemplate:CHANGES_PARSE_FAILED",
                        $"Unable to parse changes JSON: {ex.Message}"
                    );
                }

                var newTemplate = new BusinessAppPostAnalysis();

                // General Tab
                if (!changes.RootElement.TryGetProperty("general", out var generalElement) ||
                    generalElement.ValueKind != JsonValueKind.Object
                ) {
                    return result.SetFailureResult(
                        "AddOrUpdateTemplate:GENERAL_TAB_MISSING",
                        "General tab data is missing."
                    );
                }
                else
                {
                    if (!generalElement.TryGetProperty("emoji", out var emojiProp)
                        || emojiProp.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(emojiProp.GetString())
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:EMOJI_MISSING",
                            "Template emoji is required."
                        );
                    }
                    newTemplate.General.Emoji = emojiProp.GetString()!;

                    if (!generalElement.TryGetProperty("name", out var nameProp) ||
                        nameProp.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(nameProp.GetString())
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:NAME_MISSING",
                            "Template name is required."
                        );
                    }
                    newTemplate.General.Name = nameProp.GetString()!;

                    if (!generalElement.TryGetProperty("description", out var descProp) ||
                        descProp.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(descProp.GetString())
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:DESCRIPTION_MISSING",
                            "Template description is required."
                        );
                    }
                    newTemplate.General.Description = descProp.GetString()!;
                }

                // Configuration Tab
                if (!changes.RootElement.TryGetProperty("configuration", out var configElement) ||
                    configElement.ValueKind != JsonValueKind.Object
                ) {
                    return result.SetFailureResult(
                        "AddOrUpdateTemplate:CONFIG_TAB_MISSING",
                        "Configuration tab data is missing."
                    );
                }
                else
                {
                    if (!configElement.TryGetProperty("llmIntegration", out var llmIntegrationElement)
                        || llmIntegrationElement.ValueKind != JsonValueKind.Object
                        || llmIntegrationElement.ValueKind == JsonValueKind.Null
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:CONFIG_LLM_INTEGRATION_MISSING",
                            "LLM integration in configuration is required but not provided."
                        );
                    }
                    var llmValidationResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(businessId, llmIntegrationElement, "LLM");
                    if (!llmValidationResult.Success || llmValidationResult.Data == null)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:" + llmValidationResult.Code,
                            "Configuration for LLM Integration failed: " + llmValidationResult.Message
                        );
                    }
                    newTemplate.Configuration.LLMIntegration = llmValidationResult.Data;
                }

                // Summary Tab
                if (!changes.RootElement.TryGetProperty("summary", out var summaryElement) ||
                    summaryElement.ValueKind != JsonValueKind.Object
                ) {
                    return result.SetFailureResult(
                        "AddOrUpdateTemplate:SUMMARY_TAB_MISSING",
                        "Summary tab data is missing."
                    );
                }
                else
                {
                    if (!summaryElement.TryGetProperty("isActive", out var activeProp) ||
                        (activeProp.ValueKind != JsonValueKind.True && activeProp.ValueKind != JsonValueKind.False)
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:SUMMARY_ISACTIVE_INVALID",
                            "Summary 'isActive' flag is missing or invalid."
                        );
                    }
                    newTemplate.Summary.IsActive = activeProp.GetBoolean();

                    if (!summaryElement.TryGetProperty("prompt", out var promptProp) ||
                        promptProp.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(promptProp.GetString())
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:SUMMARY_PROMPT_INVALID",
                            "Summary prompt is missing or invalid."
                        );
                    }
                    newTemplate.Summary.Prompt = promptProp.GetString()!;
                }

                // Tagging Tab
                if (!changes.RootElement.TryGetProperty("tagging", out var taggingElement) ||
                    taggingElement.ValueKind != JsonValueKind.Object
                ) {
                    return result.SetFailureResult(
                        "AddOrUpdateTemplate:TAGS_DATA_MISSING",
                        "Tagging tab data is missing or invalid."
                    );
                }
                else
                {
                    if (!taggingElement.TryGetProperty("isActive", out var activeProp) ||
                        (activeProp.ValueKind != JsonValueKind.True && activeProp.ValueKind != JsonValueKind.False)
                    )
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:TAGGING_ISACTIVE_INVALID",
                            "Tagging 'isActive' flag is missing or invalid."
                        );
                    }
                    newTemplate.Tagging.IsActive = activeProp.GetBoolean();

                    if (!taggingElement.TryGetProperty("tags", out var tagsArray) ||
                        tagsArray.ValueKind != JsonValueKind.Array)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:TAGS_DATA_MISSING",
                            "Tagging 'tags' array is missing or invalid."
                        );
                    }

                    var tagsValidationResult = ValidateTagsRecursive(tagsArray, 0);
                    if (!tagsValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            $"AddOrUpdateTemplate:{tagsValidationResult.Code}",
                            tagsValidationResult.Message
                        );
                    }
                    newTemplate.Tagging.Tags = tagsValidationResult.Data!;
                }

                // Extraction Tab
                if (!changes.RootElement.TryGetProperty("extraction", out var extractionElement) ||
                    extractionElement.ValueKind != JsonValueKind.Object
                ) {
                    return result.SetFailureResult(
                        "AddOrUpdateTemplate:EXTRACTION_DATA_MISSING",
                        "Extraction tab data is missing or invalid."
                    );
                }
                else
                {
                    if (!extractionElement.TryGetProperty("isActive", out var activeProp) ||
                        (activeProp.ValueKind != JsonValueKind.True && activeProp.ValueKind != JsonValueKind.False)
                    )
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:EXTRACTION_ISACTIVE_INVALID",
                            "Extraction 'isActive' flag is missing or invalid."
                        );
                    }
                    newTemplate.Extraction.IsActive = activeProp.GetBoolean();

                    if (!extractionElement.TryGetProperty("fields", out var fieldsArray) ||
                        fieldsArray.ValueKind != JsonValueKind.Array
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:FIELDS_DATA_MISSING",
                            "Extraction 'fields' array is missing or invalid."
                        );
                    }

                    var uniqueKeys = new HashSet<string>();
                    var fieldsValidationResult = ValidateFieldsRecursive(fieldsArray, uniqueKeys, 0);
                    if (!fieldsValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            $"AddOrUpdateTemplate:{fieldsValidationResult.Code}",
                            fieldsValidationResult.Message
                        );
                    }
                    newTemplate.Extraction.Fields = fieldsValidationResult.Data!;
                }

                // Final DB Operation
                using (var session = await _mongoClient.StartSessionAsync())
                {
                    session.StartTransaction();
                    try
                    {
                        if (postType == "new")
                        {
                            newTemplate.Id = ObjectId.GenerateNewId().ToString();
                            var addResult = await _businessAppRepository.AddBusinessAppPostAnalysisTemplate(businessId, newTemplate, session);
                            if (!addResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "AddOrUpdateTemplate:DB_ADD_FAILED",
                                    "Failed to add new template to the database."
                                );
                            }
                        }
                        else // "edit"
                        {
                            newTemplate.Id = existingTemplateData!.Id;
                            var updateResult = await _businessAppRepository.UpdateBusinessAppPostAnalysisTemplate(businessId, newTemplate, session);
                            if (!updateResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "AddOrUpdateTemplate:DB_UPDATE_FAILED",
                                    "Failed to update template in the database."
                                );
                            }

                            if (existingTemplateData.Configuration.LLMIntegration.Id != newTemplate.Configuration.LLMIntegration.Id)
                            {
                                var removePALLMReferenceFromIntegration = await _businessAppRepository.RemovePostAnalysisLLMReferenceFromIntegration(businessId, existingTemplateData.Configuration.LLMIntegration.Id, newTemplate.Id, session);
                                if (!removePALLMReferenceFromIntegration)
                                {
                                    await session.AbortTransactionAsync();
                                    return result.SetFailureResult(
                                        "AddOrUpdateTemplate:FAILED_TO_REMOVE_PALLM_REFERENCE_FROM_INTEGRATION",
                                        "Failed to remove Post Analysis LLM reference from integration."
                                    );
                                }
                            }
                        }

                        var addPALLMReferenceToIntegration = await _businessAppRepository.AddPostAnalysisLLMReferenceToIntegration(businessId, newTemplate.Configuration.LLMIntegration.Id, newTemplate.Id, session);
                        if (!addPALLMReferenceToIntegration)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateTemplate:FAILED_TO_ADD_PALLM_REFERENCE_TO_INTEGRATION",
                                "Failed to add Post Analysis LLM reference to integration."
                            );
                        }

                        await session.CommitTransactionAsync();
                        return result.SetSuccessResult(newTemplate);
                    }
                    catch (Exception ex)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:DB_EXCEPTION",
                            $"An unexpected error occurred: {ex.Message}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception
                return result.SetFailureResult(
                    "AddOrUpdateTemplate:EXCEPTION",
                    $"An unexpected error occurred: {ex.Message}"
                );
            }
        }

        // Recursive Validation Helpers
        private FunctionReturnResult<List<BusinessAppPostAnalysisTagDefinition>?> ValidateTagsRecursive(JsonElement tagsArray, int currentLevel, string path = "Tags")
        {
            var result = new FunctionReturnResult<List<BusinessAppPostAnalysisTagDefinition>?>();

            if (currentLevel >= MAX_PCA_TAG_LEVELS)
            {
                return result.SetFailureResult(
                    "ValidateTagsRecursive:MAX_DEPTH_EXCEEDED",
                    $"{path}: Exceeded maximum tag nesting depth of {MAX_PCA_TAG_LEVELS} levels."
                );
            }

            if (tagsArray.GetArrayLength() > MAX_PCA_TAGS_PER_LEVEL)
            {
                return result.SetFailureResult(
                    "ValidateTagsRecursive:MAX_COUNT_EXCEEDED",
                    $"{path}: Exceeded maximum of {MAX_PCA_TAGS_PER_LEVEL} tags per level."
                );
            }

            var validatedTags = new List<BusinessAppPostAnalysisTagDefinition>();
            foreach (var (tagElement, i) in tagsArray.EnumerateArray().Select((value, i) => (value, i)))
            {
                var newTag = new BusinessAppPostAnalysisTagDefinition();
                string currentPath = $"{path}[{i}]";

                if (!tagElement.TryGetProperty("id", out var idProp) ||
                    idProp.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(idProp.GetString())
                ) {
                    return result.SetFailureResult(
                        "ValidateTagsRecursive:ID_MISSING",
                        $"{currentPath}: Tag ID is missing or invalid."
                    );
                }
                newTag.Id = idProp.GetString()!;

                if (!tagElement.TryGetProperty("name", out var nameProp) ||
                    nameProp.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(nameProp.GetString())
                ) {
                    return result.SetFailureResult(
                        "ValidateTagsRecursive:NAME_MISSING",
                        $"{currentPath}: Tag Name is missing or invalid."
                    );
                }
                newTag.Name = nameProp.GetString()!;

                if (!tagElement.TryGetProperty("description", out var descProp) ||
                    descProp.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(descProp.GetString())
                ) {
                    return result.SetFailureResult(
                        "ValidateTagsRecursive:DESC_MISSING",
                        $"{currentPath}: Tag Description is missing or invalid."
                    );
                }
                newTag.Description = descProp.GetString()!;

                if (!tagElement.TryGetProperty("subTags", out var subTagsArray)
                    || subTagsArray.ValueKind != JsonValueKind.Array
                ) {
                    return result.SetFailureResult(
                        "ValidateTagsRecursive:SUB_TAGS_MISSING",
                        $"{currentPath}: Tag 'subTags' array field is missing or invalid."
                    );
                }
                else
                {
                    var subTagsResult = ValidateTagsRecursive(subTagsArray, currentLevel + 1, $"{currentPath}.SubTags");
                    if (!subTagsResult.Success)
                    {
                        return result.SetFailureResult(
                            $"ValidateTagsRecursive:{subTagsResult.Code}",
                            subTagsResult.Message
                        );
                    }

                    newTag.SubTags = subTagsResult.Data!;
                }

                if (newTag.SubTags.Count != 0)
                {
                    if (!tagElement.TryGetProperty("rules", out var rulesElement))
                    {
                        return result.SetFailureResult(
                            "ValidateTagsRecursive:RULES_MISSING",
                            $"{currentPath}: Tag 'rules' array field is missing or invalid."
                        );
                    }
                    else
                    {
                        if (!rulesElement.TryGetProperty("allowMultiple", out var amProp) ||
                            (amProp.ValueKind != JsonValueKind.True && amProp.ValueKind != JsonValueKind.False)
                        )
                        {
                            return result.SetFailureResult(
                                "ValidateTagsRecursive:ALLOW_MULTIPLE_INVALID",
                                $"{currentPath}: Tag Rule 'allowMultiple' is missing or invalid."
                            );
                        }
                        newTag.Rules.AllowMultiple = amProp.GetBoolean();

                        if (!rulesElement.TryGetProperty("isRequired", out var irProp) ||
                            (irProp.ValueKind != JsonValueKind.True && irProp.ValueKind != JsonValueKind.False)
                        )
                        {
                            return result.SetFailureResult(
                                "ValidateTagsRecursive:IS_REQUIRED_INVALID",
                                $"{currentPath}: Tag Rule 'isRequired' is missing or invalid."
                            );
                        }
                        newTag.Rules.IsRequired = irProp.GetBoolean();
                    }
                }

                validatedTags.Add(newTag);
            }

            return result.SetSuccessResult(validatedTags);
        }

        private FunctionReturnResult<List<BusinessAppPostAnalysisExtractionField>> ValidateFieldsRecursive(JsonElement fieldsArray, HashSet<string> uniqueKeys, int currentLevel, string path = "Fields")
        {
            var result = new FunctionReturnResult<List<BusinessAppPostAnalysisExtractionField>>();

            if (currentLevel >= MAX_PCA_EXTRACTION_LEVELS)
            {
                return result.SetFailureResult(
                    "ValidateFieldsRecursive:MAX_DEPTH_EXCEEDED",
                    $"{path}: Exceeded maximum field nesting depth of {MAX_PCA_EXTRACTION_LEVELS} levels."
                );
            }

            if (fieldsArray.GetArrayLength() > MAX_PCA_FIELDS_PER_LEVEL)
            {
                return result.SetFailureResult(
                    "ValidateFieldsRecursive:MAX_COUNT_EXCEEDED",
                    $"{path}: Exceeded maximum of {MAX_PCA_FIELDS_PER_LEVEL} fields per level."
                );
            }

            var validatedFields = new List<BusinessAppPostAnalysisExtractionField>();
            foreach (var (fieldElement, i) in fieldsArray.EnumerateArray().Select((value, i) => (value, i)))
            {
                var newField = new BusinessAppPostAnalysisExtractionField();
                string currentPath = $"{path}[{i}]";

                if (!fieldElement.TryGetProperty("id", out var idProp) ||
                    idProp.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(idProp.GetString())
                ) {
                    return result.SetFailureResult(
                        "ValidateFieldsRecursive:ID_MISSING",
                        $"{currentPath}: Field ID is missing or invalid."
                    );
                }
                newField.Id = idProp.GetString()!;

                if (!fieldElement.TryGetProperty("keyName", out var keyProp) ||
                    keyProp.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(keyProp.GetString())
                ) {
                    return result.SetFailureResult(
                        "ValidateFieldsRecursive:KEY_NAME_MISSING",
                        $"{currentPath}: Field Key Name is missing or invalid."
                    );
                }
                else
                {
                    var keyName = keyProp.GetString()!;
                    if (uniqueKeys.Contains(keyName))
                    {
                        return result.SetFailureResult(
                            "ValidateFieldsRecursive:KEY_NAME_DUPLICATE",
                            $"{currentPath}: Field Key Name '{keyName}' must be unique."
                        );
                    }
                    uniqueKeys.Add(keyName);
                    newField.KeyName = keyName;
                }

                if (!fieldElement.TryGetProperty("description", out var descProp) ||
                    descProp.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(descProp.GetString())
                ) {
                    return result.SetFailureResult(
                        "ValidateFieldsRecursive:DESC_MISSING",
                        $"{currentPath}: Field Description is missing or invalid."
                    );
                }
                newField.Description = descProp.GetString()!;

                if (!fieldElement.TryGetProperty("isRequired", out var reqProp) ||
                    (reqProp.ValueKind != JsonValueKind.True && reqProp.ValueKind != JsonValueKind.False)
                ) {
                    return result.SetFailureResult(
                        "ValidateFieldsRecursive:IS_REQUIRED_INVALID",
                        $"{currentPath}: Field 'isRequired' flag is missing or invalid."
                    );
                }
                newField.IsRequired = reqProp.GetBoolean();

                if (!fieldElement.TryGetProperty("isEmptyOrNullAllowed", out var emptyOrNullProp) ||
                    (emptyOrNullProp.ValueKind != JsonValueKind.True && emptyOrNullProp.ValueKind != JsonValueKind.False)
                )
                {
                    return result.SetFailureResult(
                        "ValidateFieldsRecursive:IS_EMPTY_OR_NULL_ALLOWED_INVALID",
                        $"{currentPath}: Field 'isEmptyOrNullAllowed' flag is missing or invalid."
                    );
                }
                newField.IsEmptyOrNullAllowed = emptyOrNullProp.GetBoolean();

                if (!fieldElement.TryGetProperty("dataType", out var dtProp) ||
                    dtProp.ValueKind != JsonValueKind.Number ||
                    !dtProp.TryGetInt32(out var dtInt) ||
                    !Enum.IsDefined(typeof(BusinessAppPostAnalysisExtractionFieldDataType), dtInt)
                ) {
                    return result.SetFailureResult(
                        "ValidateFieldsRecursive:DATATYPE_INVALID",
                        $"{currentPath}: Field 'dataType' is missing or invalid."
                    );
                }
                else
                {
                    newField.DataType = (BusinessAppPostAnalysisExtractionFieldDataType)dtInt;

                    if (newField.DataType == BusinessAppPostAnalysisExtractionFieldDataType.Enum)
                    {
                        if (!fieldElement.TryGetProperty("options", out var optsArray)
                            || optsArray.ValueKind != JsonValueKind.Array
                        ) {
                            return result.SetFailureResult(
                                $"{currentPath}:OPTIONS_INVALID",
                                "Enum field requires an 'options' array."
                            );
                        }

                        newField.Options = new List<string>();
                        foreach (var opt in optsArray.EnumerateArray())
                        {
                            if (opt.ValueKind != JsonValueKind.String)
                            {
                                return result.SetFailureResult(
                                    $"{currentPath}:OPTIONS_INVALID",
                                    "Enum field options must be strings."
                                );
                            }

                            var value = opt.GetString();
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                return result.SetFailureResult(
                                    $"{currentPath}:OPTIONS_INVALID",
                                    "Enum field options cannot be empty strings."
                                );
                            }

                            if (newField.Options.Contains(value))
                            {
                                return result.SetFailureResult(
                                    $"{currentPath}:OPTIONS_INVALID",
                                    $"Enum field option '{value}' must be unique."
                                );
                            }

                            newField.Options.Add(value);
                        }
                    }
                }

                if (!fieldElement.TryGetProperty("validation", out var validationElement) ||
                    validationElement.ValueKind != JsonValueKind.Object
                ) {
                    return result.SetFailureResult(
                        "ValidateFieldsRecursive:VALIDATION_SECTION_INVALID",
                        $"{currentPath}: Validation object is missing or invalid."
                    );
                }
                else
                {
                    if (newField.DataType == BusinessAppPostAnalysisExtractionFieldDataType.String)
                    {
                        if (
                            !validationElement.TryGetProperty("pattern", out var patternProp) ||
                            patternProp.ValueKind != JsonValueKind.String
                        )
                        {
                            return result.SetFailureResult(
                                "ValidateFieldsRecursive:PATTERN_INVALID",
                                $"{currentPath}: Field 'pattern' is missing or invalid. Can be string or empty."
                            );
                        }
                        else
                        {
                            newField.Validation.Pattern = patternProp.GetString();
                            
                            if (!string.IsNullOrWhiteSpace(newField.Validation.Pattern))
                            {
                                try
                                {
                                    Regex regex = new Regex(newField.Validation.Pattern);
                                }
                                catch (ArgumentException ex)
                                {
                                    return result.SetFailureResult(
                                        "ValidateFieldsRecursive:PATTERN_INVALID",
                                        $"{currentPath}: Field 'pattern' is invalid: {ex.Message}"
                                    );
                                }
                            }
                        }
                    }
                    else if (newField.DataType == BusinessAppPostAnalysisExtractionFieldDataType.Number)
                    {
                        if (!validationElement.TryGetProperty("min", out var minProp) ||
                            (minProp.ValueKind != JsonValueKind.Number && minProp.ValueKind != JsonValueKind.Null)
                        ) {
                            return result.SetFailureResult(
                                "ValidateFieldsRecursive:MIN_INVALID",
                                $"{currentPath}: Field 'min' is missing or invalid. Can be null or number."
                            );
                        }
                        else
                        {
                            if (minProp.ValueKind == JsonValueKind.Number)
                            {
                                newField.Validation.Min = minProp.GetInt32();
                            }
                        }

                        if (!validationElement.TryGetProperty("max", out var maxProp) ||
                            (maxProp.ValueKind != JsonValueKind.Number && maxProp.ValueKind != JsonValueKind.Null)
                        ) {
                            return result.SetFailureResult(
                                "ValidateFieldsRecursive:MAX_INVALID",
                                $"{currentPath}: Field 'max' is missing or invalid. Can be null or number."
                            );
                        }
                        else
                        {
                            if (maxProp.ValueKind == JsonValueKind.Number)
                            {
                                newField.Validation.Max = maxProp.GetInt32();
                            }
                        }

                        if (newField.Validation.Min.HasValue &&
                            newField.Validation.Max.HasValue &&
                            newField.Validation.Min > newField.Validation.Max
                        ) {
                            return result.SetFailureResult(
                                "ValidateFieldsRecursive:MIN_MAX_INVALID",
                                $"{currentPath}: Min value cannot be greater than Max value."
                            );
                        }
                    }
                }

                if (!fieldElement.TryGetProperty("conditionalRules", out var rulesArray) ||
                    rulesArray.ValueKind != JsonValueKind.Array
                )
                {
                    return result.SetFailureResult(
                        "ValidateFieldsRecursive:RULES_MISSING",
                        $"{currentPath}: Field 'conditionalRules' is missing or invalid."
                    );
                }
                else
                {
                    var rulesResult = ValidateRulesRecursive(rulesArray, uniqueKeys, currentLevel, $"{currentPath}.ConditionalRules");
                    if (!rulesResult.Success)
                    {
                        return result.SetFailureResult(
                            $"ValidateFieldsRecursive:{rulesResult}",
                            rulesResult.Message
                        );
                    }

                    newField.ConditionalRules = rulesResult.Data!;
                }

                validatedFields.Add(newField);
            }

            return result.SetSuccessResult(validatedFields);
        }
        private FunctionReturnResult<List<BusinessAppPostAnalysisExtractionConditionalRule>> ValidateRulesRecursive(JsonElement rulesArray, HashSet<string> uniqueKeys, int parentFieldLevel, string path)
        {
            var result = new FunctionReturnResult<List<BusinessAppPostAnalysisExtractionConditionalRule>>();

            if (rulesArray.GetArrayLength() > MAX_PCA_RULES_PER_FIELD)
            {
                return result.SetFailureResult(
                    "ValidateRulesRecursive:MAX_RULES_EXCEEDED",
                    $"{path}: Exceeded maximum of {MAX_PCA_RULES_PER_FIELD} rules per field."
                );
            }

            var validatedRules = new List<BusinessAppPostAnalysisExtractionConditionalRule>();
            foreach (var (ruleElement, i) in rulesArray.EnumerateArray().Select((value, i) => (value, i)))
            {
                var newRule = new BusinessAppPostAnalysisExtractionConditionalRule();
                string currentPath = $"{path}[{i}]";

                if (!ruleElement.TryGetProperty("id", out var idProp) ||
                    idProp.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(idProp.GetString())
                ) {
                    return result.SetFailureResult(
                        "ValidateRulesRecursive:ID_INVALID",
                        $"{currentPath}: Rule ID is missing or invalid."
                    );
                }
                newRule.Id = idProp.GetString()!;

                if (!ruleElement.TryGetProperty("condition", out var condElement) ||
                    condElement.ValueKind != JsonValueKind.Object
                ) {
                    return result.SetFailureResult(
                        "ValidateRulesRecursive:CONDITION_INVALID",
                        $"{currentPath}: Rule condition is missing or invalid."
                    );
                }
                else
                {
                    if (!condElement.TryGetProperty("operator", out var opProp) ||
                        opProp.ValueKind != JsonValueKind.Number ||
                        !opProp.TryGetInt32(out var opInt) ||
                        !Enum.IsDefined(typeof(BusinessAppPostAnalysisExtractionConditionOperator), opInt)
                    ) {
                        return result.SetFailureResult(
                            "ValidateRulesRecursive:OPERATOR_INVALID",
                            $"{currentPath}: Rule condition operator is missing or invalid."
                        );
                    }
                    newRule.Condition.Operator = (BusinessAppPostAnalysisExtractionConditionOperator)opInt;

                    if (!condElement.TryGetProperty("value", out var valProp) ||
                        valProp.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(valProp.GetString())
                    ) {
                        return result.SetFailureResult(
                            "ValidateRulesRecursive:VALUE_INVALID",
                            $"{currentPath}: Rule condition value is missing or invalid."
                        );
                    }
                    newRule.Condition.Value = valProp.GetString()!;
                }

                if (!ruleElement.TryGetProperty("fieldsToExtract", out var fieldsArray) ||
                    fieldsArray.ValueKind != JsonValueKind.Array
                ) {
                    return result.SetFailureResult(
                        "ValidateRulesRecursive:FIELDS_MISSING",
                        $"{currentPath}: Rule fieldsToExtract is missing or invalid."
                    );
                }
                else
                {
                    var fieldsResult = ValidateFieldsRecursive(fieldsArray, uniqueKeys, parentFieldLevel + 1, $"{currentPath}.FieldsToExtract");
                    if (!fieldsResult.Success)
                    {
                        return result.SetFailureResult(
                            $"ValidateRulesRecursive:{fieldsResult}",
                            fieldsResult.Message
                        );
                    }
                    newRule.FieldsToExtract = fieldsResult.Data!;

                    if (newRule.FieldsToExtract.Count == 0)
                    {
                        return result.SetFailureResult(
                            "ValidateRulesRecursive:FIELDS_EMPTY",
                            $"{currentPath}: A conditional rule must define at least one field to extract."
                        );
                    }
                }

                validatedRules.Add(newRule);
            }

            return result.SetSuccessResult(validatedRules);
        }
    }
}
