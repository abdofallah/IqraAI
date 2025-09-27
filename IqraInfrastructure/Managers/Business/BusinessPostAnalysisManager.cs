using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessPostAnalysisManager
    {
        private BusinessManager _parent;
        private BusinessAppRepository _businessAppRepository;

        public BusinessPostAnalysisManager(BusinessManager businessManager, BusinessAppRepository businessAppRepository)
        {
            _parent = businessManager;
            _businessAppRepository = businessAppRepository;
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
                    if (!taggingElement.TryGetProperty("tags", out var tagsArray) ||
                        tagsArray.ValueKind != JsonValueKind.Array)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:TAGS_DATA_MISSING",
                            "Tagging 'tags' array is missing or invalid."
                        );
                    }

                    var tagsValidationResult = ValidateTagsRecursive(tagsArray);
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
                    if (!extractionElement.TryGetProperty("fields", out var fieldsArray) ||
                        fieldsArray.ValueKind != JsonValueKind.Array
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:FIELDS_DATA_MISSING",
                            "Extraction 'fields' array is missing or invalid."
                        );
                    }

                    var uniqueKeys = new HashSet<string>();
                    var fieldsValidationResult = ValidateFieldsRecursive(fieldsArray, uniqueKeys);
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
                if (postType == "new")
                {
                    newTemplate.Id = Guid.NewGuid().ToString();
                    var addResult = await _businessAppRepository.AddBusinessAppPostAnalysisTemplate(businessId, newTemplate);
                    if (!addResult)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:DB_ADD_FAILED",
                            "Failed to add new template to the database."
                        );
                    }
                }
                else // "edit"
                {
                    newTemplate.Id = existingTemplateData!.Id;
                    var updateResult = await _businessAppRepository.UpdateBusinessAppPostAnalysisTemplate(businessId, newTemplate);
                    if (!updateResult)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTemplate:DB_UPDATE_FAILED",
                            "Failed to update template in the database."
                        );
                    }
                }

                return result.SetSuccessResult(newTemplate);
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
        private FunctionReturnResult<List<BusinessAppPostAnalysisTagDefinition>?> ValidateTagsRecursive(JsonElement tagsArray, string path = "Tags")
        {
            var result = new FunctionReturnResult<List<BusinessAppPostAnalysisTagDefinition>?>();

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
                    ) {
                        return result.SetFailureResult(
                            "ValidateTagsRecursive:ALLOW_MULTIPLE_INVALID",
                            $"{currentPath}: Tag Rule 'allowMultiple' is invalid."
                        );
                    }
                    newTag.Rules.AllowMultiple = amProp.GetBoolean();

                    if (!rulesElement.TryGetProperty("isRequired", out var irProp) ||
                        (irProp.ValueKind != JsonValueKind.True && irProp.ValueKind != JsonValueKind.False)
                    ) {
                        return result.SetFailureResult(
                            "ValidateTagsRecursive:IS_REQUIRED_INVALID",
                            $"{currentPath}: Tag Rule 'isRequired' is invalid."
                        );
                    }
                    newTag.Rules.IsRequired = irProp.GetBoolean();
                }

                if (tagElement.TryGetProperty("subTags", out var subTagsArray) && subTagsArray.ValueKind == JsonValueKind.Array)
                {
                    var subTagsResult = ValidateTagsRecursive(subTagsArray, $"{currentPath}.SubTags");
                    if (!subTagsResult.Success)
                    {
                        return result.SetFailureResult(
                            $"ValidateTagsRecursive:{subTagsResult.Code}",
                            subTagsResult.Message
                        );
                    }

                    newTag.SubTags = subTagsResult.Data!;
                }

                validatedTags.Add(newTag);
            }
            return result.SetSuccessResult(validatedTags);
        }

        private FunctionReturnResult<List<BusinessAppPostAnalysisExtractionField>> ValidateFieldsRecursive(JsonElement fieldsArray, HashSet<string> uniqueKeys, string path = "Fields")
        {
            var result = new FunctionReturnResult<List<BusinessAppPostAnalysisExtractionField>>();
            var validatedFields = new List<BusinessAppPostAnalysisExtractionField>();

            foreach (var (fieldElement, i) in fieldsArray.EnumerateArray().Select((value, i) => (value, i)))
            {
                var newField = new BusinessAppPostAnalysisExtractionField();
                string currentPath = $"{path}[{i}]";

                if (!fieldElement.TryGetProperty("id", out var idProp) || string.IsNullOrWhiteSpace(idProp.GetString())) return result.SetFailureResult($"{currentPath}:ID_MISSING", "Field ID is missing.");
                newField.Id = idProp.GetString()!;

                if (!fieldElement.TryGetProperty("keyName", out var keyProp) || string.IsNullOrWhiteSpace(keyProp.GetString())) return result.SetFailureResult($"{currentPath}:KEY_NAME_MISSING", "Field Key Name is required.");
                var keyName = keyProp.GetString()!;
                if (uniqueKeys.Contains(keyName)) return result.SetFailureResult($"{currentPath}:KEY_NAME_DUPLICATE", $"Field Key Name '{keyName}' must be unique.");
                uniqueKeys.Add(keyName);
                newField.KeyName = keyName;

                if (!fieldElement.TryGetProperty("description", out var descProp) || descProp.ValueKind != JsonValueKind.String) return result.SetFailureResult($"{currentPath}:DESC_MISSING", "Field Description is required.");
                newField.Description = descProp.GetString()!;

                if (!fieldElement.TryGetProperty("isRequired", out var reqProp) || (reqProp.ValueKind != JsonValueKind.True && reqProp.ValueKind != JsonValueKind.False)) return result.SetFailureResult($"{currentPath}:IS_REQUIRED_INVALID", "Field 'isRequired' flag is invalid.");
                newField.IsRequired = reqProp.GetBoolean();

                if (!fieldElement.TryGetProperty("dataType", out var dtProp) || !dtProp.TryGetInt32(out var dtInt) || !Enum.IsDefined(typeof(FieldDataType), dtInt)) return result.SetFailureResult($"{currentPath}:DATATYPE_INVALID", "Field 'dataType' is invalid.");
                newField.DataType = (BusinessAppPostAnalysisExtractionFieldDataType)dtInt;

                if (newField.DataType == BusinessAppPostAnalysisExtractionFieldDataType.Enum)
                {
                    if (!fieldElement.TryGetProperty("options", out var optsArray) || optsArray.ValueKind != JsonValueKind.Array) return result.SetFailureResult($"{currentPath}:OPTIONS_INVALID", "Enum field requires an 'options' array.");
                    newField.Options = optsArray.EnumerateArray().Select(o => o.GetString()!).ToList();
                }

                if (fieldElement.TryGetProperty("conditionalRules", out var rulesArray) && rulesArray.ValueKind == JsonValueKind.Array)
                {
                    var rulesResult = ValidateRulesRecursive(rulesArray, uniqueKeys, $"{currentPath}.ConditionalRules");
                    if (!rulesResult.Success) return rulesResult.ToGeneric<List<BusinessAppPostAnalysisExtractionField>>();
                    newField.ConditionalRules = rulesResult.Data;
                }
                validatedFields.Add(newField);
            }
            return result.SetSuccessResult(validatedFields);
        }

        private FunctionReturnResult<List<BusinessAppPostAnalysisExtractionConditionalRule>> ValidateRulesRecursive(JsonElement rulesArray, HashSet<string> uniqueKeys, string path)
        {
            var result = new FunctionReturnResult<List<BusinessAppPostAnalysisExtractionConditionalRule>>();
            var validatedRules = new List<BusinessAppPostAnalysisExtractionConditionalRule>();
            foreach (var (ruleElement, i) in rulesArray.EnumerateArray().Select((value, i) => (value, i)))
            {
                var newRule = new BusinessAppPostAnalysisExtractionConditionalRule();
                string currentPath = $"{path}[{i}]";

                if (!ruleElement.TryGetProperty("id", out var idProp) || string.IsNullOrWhiteSpace(idProp.GetString())) return result.SetFailureResult($"{currentPath}:ID_MISSING", "Rule ID is missing.");
                newRule.Id = idProp.GetString()!;

                if (!ruleElement.TryGetProperty("condition", out var condElement)) return result.SetFailureResult($"{currentPath}:CONDITION_MISSING", "Rule condition is missing.");
                if (!condElement.TryGetProperty("operator", out var opProp) || !opProp.TryGetInt32(out var opInt) || !Enum.IsDefined(typeof(BusinessAppPostAnalysisExtractionConditionOperator), opInt)) return result.SetFailureResult($"{currentPath}:OPERATOR_INVALID", "Rule condition operator is invalid.");
                if (!condElement.TryGetProperty("value", out var valProp) || valProp.ValueKind != JsonValueKind.String) return result.SetFailureResult($"{currentPath}:VALUE_INVALID", "Rule condition value is invalid.");
                newRule.Condition = new BusinessAppPostAnalysisExtractionFieldCondition { Operator = (BusinessAppPostAnalysisExtractionConditionOperator)opInt, Value = valProp.GetString()! };

                if (ruleElement.TryGetProperty("fieldsToExtract", out var fieldsArray) && fieldsArray.ValueKind == JsonValueKind.Array)
                {
                    var fieldsResult = ValidateFieldsRecursive(fieldsArray, uniqueKeys, $"{currentPath}.FieldsToExtract");
                    if (!fieldsResult.Success) return fieldsResult.ToGeneric<List<BusinessAppPostAnalysisExtractionConditionalRule>>();
                    newRule.FieldsToExtract = fieldsResult.Data;
                }
                validatedRules.Add(newRule);
            }
            return result.SetSuccessResult(validatedRules);
        }
    }
}
