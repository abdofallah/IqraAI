using IqraCore.Entities.Business;
using IqraCore.Entities.Embedding;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.LLM;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.Rerank;
using IqraCore.Entities.STT;
using IqraCore.Entities.TTS;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IqraInfrastructure.Helpers.Business
{
    public class IntegrationConfigurationManager
    {
        private bool _dependenciesSetup = false;

        private BusinessIntegrationsManager _businessIntegrationsManager;

        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly EmbeddingProviderManager _embeddingProviderManager;
        private readonly RerankProviderManager _rerankProviderManager;

        public IntegrationConfigurationManager(
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            LLMProviderManager llmProviderManager,
            EmbeddingProviderManager embeddingProviderManager,
            RerankProviderManager rerankProviderManager)
        {
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
            _llmProviderManager = llmProviderManager;
            _embeddingProviderManager = embeddingProviderManager;
            _rerankProviderManager = rerankProviderManager;
        }

        public void SetupDependencies(BusinessIntegrationsManager businessIntegrationsManager)
        {
            if (_dependenciesSetup) return;

            _businessIntegrationsManager = businessIntegrationsManager;
            _dependenciesSetup = true;
        }

        public async Task<FunctionReturnResult<BusinessAppAgentIntegrationData>> ValidateAndBuildIntegrationData(
            long businessId,
            JsonElement currentIntegrationElement,
            string integrationType,
            string? businessLanguage = null)
        {
            var result = new FunctionReturnResult<BusinessAppAgentIntegrationData>();

            if (!_dependenciesSetup)
            {
                return result.SetFailureResult(
                    "ValidateIntegrationData:MISSING_DEPENDENCIES",
                    "Dependencies for integration configuration managers are not setup."
                );
            }

            if (!currentIntegrationElement.TryGetProperty("id", out var integrationIdElement))
            {
                return result.SetFailureResult(
                    "ValidateIntegrationData:MISSING_ID",
                    $"{integrationType} integration id is missing."
                );
            }

            var integrationId = integrationIdElement.GetString();
            if (string.IsNullOrWhiteSpace(integrationId))
            {
                return result.SetFailureResult(
                    "ValidateIntegrationData:EMPTY_ID",
                    $"{integrationType} integration id is empty."
                );
            }

            if (!currentIntegrationElement.TryGetProperty("fieldValues", out var fieldValuesElement))
            {
                return result.SetFailureResult(
                    "ValidateIntegrationData:MISSING_FIELDS",
                    $"{integrationType} field values not found in integration."
                );
            }

            return await ValidateAndBuildIntegrationData(businessId, integrationId, fieldValuesElement, integrationType, businessLanguage);
        }

        public async Task<FunctionReturnResult<BusinessAppAgentIntegrationData>> ValidateAndBuildIntegrationData(
            long businessId,
            string integrationId,
            JsonElement fieldValuesElement,
            string integrationType,
            string? businessLanguage = null)
        {
            var result = new FunctionReturnResult<BusinessAppAgentIntegrationData>();

            if (!_dependenciesSetup)
            {
                return result.SetFailureResult(
                    "ValidateIntegrationData:MISSING_DEPENDENCIES",
                    "Dependencies for integration configuration managers are not setup."
                );
            }

            var currentIntegrationResult = await _businessIntegrationsManager.getBusinessIntegrationById(businessId, integrationId);
            if (!currentIntegrationResult.Success || currentIntegrationResult.Data == null)
            {
                return result.SetFailureResult(
                    "ValidateIntegrationData:" + currentIntegrationResult.Code,
                    currentIntegrationResult.Message
                );
            }

            // 1. Fetch Provider Data dynamically based on Type
            FunctionReturnResult<object?> providerDataResult = new FunctionReturnResult<object?>();

            switch (integrationType)
            {
                case "STT":
                    var sttResult = await _sttProviderManager.GetProviderDataByIntegration(currentIntegrationResult.Data.Type);
                    providerDataResult.Success = sttResult.Success; providerDataResult.Data = sttResult.Data; providerDataResult.Message = sttResult.Message; providerDataResult.Code = sttResult.Code;
                    break;
                case "TTS":
                    var ttsResult = await _ttsProviderManager.GetProviderDataByIntegration(currentIntegrationResult.Data.Type);
                    providerDataResult.Success = ttsResult.Success; providerDataResult.Data = ttsResult.Data; providerDataResult.Message = ttsResult.Message; providerDataResult.Code = ttsResult.Code;
                    break;
                case "LLM":
                    var llmResult = await _llmProviderManager.GetProviderDataByIntegration(currentIntegrationResult.Data.Type);
                    providerDataResult.Success = llmResult.Success; providerDataResult.Data = llmResult.Data; providerDataResult.Message = llmResult.Message; providerDataResult.Code = llmResult.Code;
                    break;
                case "Embedding":
                    var embResult = await _embeddingProviderManager.GetProviderDataByIntegration(currentIntegrationResult.Data.Type);
                    providerDataResult.Success = embResult.Success; providerDataResult.Data = embResult.Data; providerDataResult.Message = embResult.Message; providerDataResult.Code = embResult.Code;
                    break;
                case "Rerank":
                    var rankResult = await _rerankProviderManager.GetProviderDataByIntegration(currentIntegrationResult.Data.Type);
                    providerDataResult.Success = rankResult.Success; providerDataResult.Data = rankResult.Data; providerDataResult.Message = rankResult.Message; providerDataResult.Code = rankResult.Code;
                    break;
                default:
                    return result.SetFailureResult("ValidateIntegrationData:UNKNOWN_TYPE", $"Unknown integration type: {integrationType}.");
            }

            if (!providerDataResult.Success || providerDataResult.Data == null)
            {
                return result.SetFailureResult(
                    "ValidateIntegrationData:" + providerDataResult.Code,
                    providerDataResult.Message
                );
            }

            // 2. Extract Schema (Fields and Models)
            IEnumerable<ProviderFieldBase> schemaFields;
            IEnumerable<ProviderModelBase> schemaModels;

            try
            {
                dynamic data = providerDataResult.Data;
                schemaFields = data.UserIntegrationFields;
                var modelsProp = data.GetType().GetProperty("Models");
                var modelsValue = modelsProp.GetValue(data);
                schemaModels = ((System.Collections.IEnumerable)modelsValue).Cast<ProviderModelBase>();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ValidateIntegrationData:CAST_ERROR",
                    $"Internal error mapping provider schema: {ex.Message}"
                );
            }

            var newIntegrationData = new BusinessAppAgentIntegrationData()
            {
                Id = integrationId,
            };

            // --- PASS 1: Extract all raw inputs into a dictionary ---
            var rawInputs = new Dictionary<string, JsonElement>();
            if (fieldValuesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in fieldValuesElement.EnumerateObject())
                {
                    rawInputs[prop.Name] = prop.Value;
                }
            }

            // --- PASS 2: Validate the Model Field FIRST (Absolute truth for visibility) ---
            string selectedModelId = "";
            var modelFieldSchema = schemaFields.FirstOrDefault(f => f.Type == "models");

            if (modelFieldSchema != null)
            {
                bool modelExists = rawInputs.TryGetValue(modelFieldSchema.Id, out var rawModelValue);
                bool isModelEmpty = !modelExists || rawModelValue.ValueKind == JsonValueKind.Null || (rawModelValue.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(rawModelValue.GetString()));

                if (isModelEmpty)
                {
                    if (modelFieldSchema.Required)
                    {
                        return result.SetFailureResult("ValidateIntegrationData:MODEL_REQUIRED", $"Required {integrationType} field '{modelFieldSchema.Name}' is missing.");
                    }
                }
                else
                {
                    var modelValidation = ValidateFieldValue(
                        modelFieldSchema,
                        rawModelValue,
                        schemaModels,
                        integrationType,
                        currentIntegrationResult.Data.FriendlyName,
                        selectedModelId
                    );
                    if (!modelValidation.Success)
                    {
                        return result.SetFailureResult($"ValidateIntegrationData:{modelValidation.Code}", modelValidation.Message);
                    }

                    selectedModelId = modelValidation.Data?.ToString() ?? "";
                    newIntegrationData.FieldValues.Add(modelFieldSchema.Id, selectedModelId);
                }
            }

            // --- PASS 3: Process the remaining fields securely ---
            foreach (var fieldSchema in schemaFields)
            {
                // Skip model field as it's already processed
                if (fieldSchema.Type == "models") continue;

                if (fieldSchema.IsEncrypted)
                {
                    return result.SetFailureResult(
                        "ValidateIntegrationData:ENCRYPTED_NOT_SUPPORTED",
                        $"Encrypted {integrationType} field value for '{fieldSchema.Name}' is not supported via client config."
                    );
                }

                // Check Dynamic Visibility (Model & Field conditions)
                if (!IsFieldVisible(fieldSchema, selectedModelId, rawInputs))
                {
                    // Hidden field: Even if the bad actor sent data, we ignore it completely.
                    continue;
                }

                // Field is visible, proceed with validation
                bool fieldExists = rawInputs.TryGetValue(fieldSchema.Id, out var jsonValue);
                bool isEmptyOrNull = !fieldExists || jsonValue.ValueKind == JsonValueKind.Null || (jsonValue.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(jsonValue.GetString()));

                if (isEmptyOrNull)
                {
                    if (fieldSchema.Required)
                    {
                        return result.SetFailureResult(
                            "ValidateIntegrationData:REQUIRED_MISSING",
                            $"Required {integrationType} field '{fieldSchema.Name}' is missing or empty."
                        );
                    }
                    else
                    {
                        // Optional and empty. Apply default if schema defines one.
                        if (!string.IsNullOrEmpty(fieldSchema.DefaultValue))
                        {
                            ApplyDefaultValue(newIntegrationData, fieldSchema);
                        }
                        continue;
                    }
                }

                // Validate the specific value strictly against the schema rules
                var validationResult = ValidateFieldValue(
                    fieldSchema,
                    jsonValue,
                    schemaModels,
                    integrationType,
                    currentIntegrationResult.Data.FriendlyName,
                    selectedModelId
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"ValidateIntegrationData:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                // Validated successfully, add to final clean config dictionary
                if (validationResult.Data != null)
                {
                    newIntegrationData.FieldValues.Add(fieldSchema.Id, validationResult.Data);
                }
            }

            return result.SetSuccessResult(newIntegrationData);
        }

        // ==========================================
        // Helper Methods
        // ==========================================

        private bool IsFieldVisible(ProviderFieldBase field, string selectedModelId, Dictionary<string, JsonElement> rawInputs)
        {
            // 1. Model Condition Check
            if (field.ModelCondition != null && field.ModelCondition.Models != null && field.ModelCondition.Models.Any())
            {
                bool containsModel = field.ModelCondition.Models.Contains(selectedModelId);

                if (field.ModelCondition.Type == ProviderFieldModelConitionType.Include && !containsModel)
                    return false;

                if (field.ModelCondition.Type == ProviderFieldModelConitionType.Exclude && containsModel)
                    return false;
            }

            // 2. Field Conditions Check
            if (field.FieldConditions != null && field.FieldConditions.Any())
            {
                foreach (var cond in field.FieldConditions)
                {
                    string dependencyValue = "";
                    if (rawInputs.TryGetValue(cond.FieldId, out var depElement))
                    {
                        dependencyValue = depElement.ValueKind == JsonValueKind.String
                                            ? depElement.GetString() ?? ""
                                            : depElement.GetRawText().Trim('"');
                    }

                    // Directly accessing the new 'Value' string property
                    string conditionValue = cond.Value ?? "";

                    bool isMatch = false;
                    switch (cond.Type)
                    {
                        case ProviderFieldFieldConitionType.Equal:
                            isMatch = dependencyValue.Equals(conditionValue, StringComparison.OrdinalIgnoreCase);
                            break;
                        case ProviderFieldFieldConitionType.NotEqual:
                            isMatch = !dependencyValue.Equals(conditionValue, StringComparison.OrdinalIgnoreCase);
                            break;
                        case ProviderFieldFieldConitionType.Include:
                            isMatch = dependencyValue.Contains(conditionValue, StringComparison.OrdinalIgnoreCase);
                            break;
                        case ProviderFieldFieldConitionType.Exclude:
                            isMatch = !dependencyValue.Contains(conditionValue, StringComparison.OrdinalIgnoreCase);
                            break;
                        case ProviderFieldFieldConitionType.StartsWith:
                            isMatch = dependencyValue.StartsWith(conditionValue, StringComparison.OrdinalIgnoreCase);
                            break;
                        case ProviderFieldFieldConitionType.EndsWith:
                            isMatch = dependencyValue.EndsWith(conditionValue, StringComparison.OrdinalIgnoreCase);
                            break;
                        case ProviderFieldFieldConitionType.GreaterThan:
                            if (double.TryParse(dependencyValue, out var depNum) && double.TryParse(conditionValue, out var condNum))
                                isMatch = depNum > condNum;
                            break;
                        case ProviderFieldFieldConitionType.LessThan:
                            if (double.TryParse(dependencyValue, out var depNum2) && double.TryParse(conditionValue, out var condNum2))
                                isMatch = depNum2 < condNum2;
                            break;
                        case ProviderFieldFieldConitionType.GreaterThanOrEqual:
                            if (double.TryParse(dependencyValue, out var depNum3) && double.TryParse(conditionValue, out var condNum3))
                                isMatch = depNum3 >= condNum3;
                            break;
                        case ProviderFieldFieldConitionType.LessThanOrEqual:
                            if (double.TryParse(dependencyValue, out var depNum4) && double.TryParse(conditionValue, out var condNum4))
                                isMatch = depNum4 <= condNum4;
                            break;
                    }

                    if (cond.Visibility == ProviderFieldFieldConitionVisibility.Visible && !isMatch) return false;
                    if (cond.Visibility == ProviderFieldFieldConitionVisibility.Hidden && isMatch) return false;
                }
            }

            return true;
        }

        private void ApplyDefaultValue(BusinessAppAgentIntegrationData data, ProviderFieldBase field)
        {
            try
            {
                if (field.Type == "number" && int.TryParse(field.DefaultValue, out int defaultInt))
                {
                    data.FieldValues.Add(field.Id, defaultInt);
                }
                else if (field.Type == "double_number" && double.TryParse(field.DefaultValue, out double defaultDouble))
                {
                    data.FieldValues.Add(field.Id, defaultDouble);
                }
                else if (field.Type == "boolean")
                {
                    var lower = field.DefaultValue.ToLower();
                    if (lower == "true" || lower == "on" || lower == "yes") data.FieldValues.Add(field.Id, true);
                    else if (lower == "false" || lower == "off" || lower == "no") data.FieldValues.Add(field.Id, false);
                }
                else
                {
                    data.FieldValues.Add(field.Id, field.DefaultValue);
                }
            }
            catch { /* Ignore parsing errors on defaults */ }
        }

        private FunctionReturnResult<object> ValidateFieldValue(
            ProviderFieldBase field,
            JsonElement jsonValue,
            IEnumerable<ProviderModelBase> models,
            string integrationType,
            string integrationName,
            string selectedModelId)
        {
            var result = new FunctionReturnResult<object>();

            // --- Handle Arrays First ---
            if (field.IsArray)
            {
                var list = new List<string>();

                if (jsonValue.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in jsonValue.EnumerateArray())
                    {
                        list.Add(item.ToString());
                    }
                }
                else if (jsonValue.ValueKind == JsonValueKind.String)
                {
                    var s = jsonValue.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        list.AddRange(s.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)));
                    }
                }
                else
                {
                    return result.SetFailureResult("INVALID_ARRAY_FORMAT", $"Field '{field.Name}' expects an array or comma-separated string.");
                }

                if (field.MinArrayCount.HasValue && list.Count < field.MinArrayCount.Value)
                {
                    return result.SetFailureResult("ARRAY_MIN", $"Field '{field.Name}' requires at least {field.MinArrayCount.Value} items.");
                }

                if (field.MaxArrayCount.HasValue && list.Count > field.MaxArrayCount.Value)
                {
                    return result.SetFailureResult("ARRAY_MAX", $"Field '{field.Name}' exceeds maximum of {field.MaxArrayCount.Value} items.");
                }

                return result.SetSuccessResult((object)string.Join(", ", list));
            }

            // --- Handle Single Values ---
            switch (field.Type)
            {
                case "text":
                case "string":
                    {
                        var val = jsonValue.ValueKind == JsonValueKind.String ? jsonValue.GetString() : jsonValue.ToString();
                        val = val ?? "";

                        if (!string.IsNullOrEmpty(field.StringRegex) && !string.IsNullOrEmpty(val))
                        {
                            if (!Regex.IsMatch(val, field.StringRegex))
                            {
                                return result.SetFailureResult("REGEX_FAIL", $"Format for field '{field.Name}' is invalid.");
                            }
                        }
                        return result.SetSuccessResult(val);
                    }

                case "number":
                    {
                        int intVal = 0;
                        if (jsonValue.ValueKind == JsonValueKind.Number && jsonValue.TryGetInt32(out intVal)) { }
                        else if (jsonValue.ValueKind == JsonValueKind.String && int.TryParse(jsonValue.GetString(), out intVal)) { }
                        else
                        {
                            return result.SetFailureResult("INVALID_INT", $"Field '{field.Name}' requires a valid whole number.");
                        }

                        if (field.MinNumberValue.HasValue && intVal < field.MinNumberValue.Value)
                        {
                            return result.SetFailureResult("MIN_VAL", $"'{field.Name}' must be >= {field.MinNumberValue.Value}.");
                        }

                        if (field.MaxNumberValue.HasValue && intVal > field.MaxNumberValue.Value)
                        {
                            return result.SetFailureResult("MAX_VAL", $"'{field.Name}' must be <= {field.MaxNumberValue.Value}.");
                        }

                        return result.SetSuccessResult(intVal);
                    }

                case "double_number":
                    {
                        double dVal = 0;
                        if (jsonValue.ValueKind == JsonValueKind.Number && jsonValue.TryGetDouble(out dVal)) { }
                        else if (jsonValue.ValueKind == JsonValueKind.String && double.TryParse(jsonValue.GetString(), out dVal)) { }
                        else
                        {
                            return result.SetFailureResult("INVALID_DOUBLE", $"Field '{field.Name}' requires a valid decimal number.");
                        }

                        if (field.MinNumberValue.HasValue && dVal < field.MinNumberValue.Value)
                        {
                            return result.SetFailureResult("MIN_VAL", $"'{field.Name}' must be >= {field.MinNumberValue.Value}.");
                        }

                        if (field.MaxNumberValue.HasValue && dVal > field.MaxNumberValue.Value)
                        {
                            return result.SetFailureResult("MAX_VAL", $"'{field.Name}' must be <= {field.MaxNumberValue.Value}.");
                        }

                        if (field.DecimalPlaces.HasValue)
                        {
                            dVal = Math.Round(dVal, field.DecimalPlaces.Value);
                        }

                        return result.SetSuccessResult(dVal);
                    }

                case "select":
                    {
                        var val = jsonValue.ValueKind == JsonValueKind.String ? jsonValue.GetString() : jsonValue.ToString();
                        val = val ?? "";

                        if (field.Options != null && !string.IsNullOrEmpty(val))
                        {
                            var validOption = field.Options.FirstOrDefault(o => o.Key == val);
                            if (validOption == null)
                            {
                                return result.SetFailureResult("INVALID_OPTION", $"Option '{val}' is not valid for '{field.Name}'.");
                            }
                        }

                        return result.SetSuccessResult(val);
                    }

                case "models":
                    {
                        var modelId = jsonValue.ValueKind == JsonValueKind.String ? jsonValue.GetString() : jsonValue.ToString();
                        modelId = modelId ?? "";

                        var modelData = models.FirstOrDefault(m => m.Id == modelId);
                        if (modelData == null)
                        {
                            return result.SetFailureResult("INVALID_MODEL", $"{integrationType} model '{modelId}' is not found in integration '{integrationName}'.");
                        }

                        if (modelData.DisabledAt != null)
                        {
                            return result.SetFailureResult("DISABLED_MODEL", $"{integrationType} model '{modelId}' is currently disabled.");
                        }

                        return result.SetSuccessResult(modelId);
                    }

                case "boolean":
                    {
                        if (jsonValue.ValueKind == JsonValueKind.True) return result.SetSuccessResult(true);
                        if (jsonValue.ValueKind == JsonValueKind.False) return result.SetSuccessResult(false);

                        if (jsonValue.ValueKind == JsonValueKind.String)
                        {
                            var strVal = jsonValue.GetString()?.ToLower();
                            if (strVal == "true" || strVal == "on") return result.SetSuccessResult(true);
                            if (strVal == "false" || strVal == "off") return result.SetSuccessResult(false);
                        }

                        return result.SetFailureResult("INVALID_BOOL", $"Field '{field.Name}' requires a boolean value.");
                    }

                case "model_vector_dimensions":
                    {
                        if (integrationType != "Embedding")
                        {
                            return result.SetFailureResult("INVALID_PROVIDER", $"Field type '{field.Type}' is only valid for Embedding integrations.");
                        }

                        int dimVal = 0;
                        if (jsonValue.ValueKind == JsonValueKind.Number && jsonValue.TryGetInt32(out dimVal)) { }
                        else if (jsonValue.ValueKind == JsonValueKind.String && int.TryParse(jsonValue.GetString(), out dimVal)) { }
                        else
                        {
                            return result.SetFailureResult("INVALID_DIM", $"Field '{field.Name}' requires a valid whole number.");
                        }

                        if (string.IsNullOrWhiteSpace(selectedModelId))
                        {
                            return result.SetFailureResult("MISSING_MODEL", $"A model must be selected before '{field.Name}' can be validated.");
                        }

                        var modelData = models.FirstOrDefault(m => m.Id == selectedModelId) as EmbeddingProviderModelData;
                        if (modelData == null)
                        {
                            return result.SetFailureResult("INVALID_MODEL", $"Selected model '{selectedModelId}' not found.");
                        }

                        if (modelData.AvailableVectorDimensions == null || !modelData.AvailableVectorDimensions.Contains(dimVal))
                        {
                            return result.SetFailureResult("INVALID_DIM_FOR_MODEL", $"Vector dimension {dimVal} is not supported by model '{selectedModelId}'.");
                        }

                        return result.SetSuccessResult(dimVal);
                    }

                default:
                    {
                        return result.SetFailureResult("INVALID_TYPE", $"Unsupported field type '{field.Type}' for '{field.Name}'.");
                    }
            }
        }
    }
}