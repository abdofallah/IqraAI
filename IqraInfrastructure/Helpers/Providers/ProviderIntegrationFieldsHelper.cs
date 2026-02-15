using System.Text.Json;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.ProviderBase;

namespace IqraInfrastructure.Helpers.Provider
{
    public static class ProviderIntegrationFieldsHelper
    {
        public static FunctionReturnResult<List<ProviderFieldBase>> ParseAndValidateFields(
            JsonElement fieldsElement,
            List<string> availableModelIds
        ) {
            var result = new FunctionReturnResult<List<ProviderFieldBase>>();
            var fieldsList = new List<ProviderFieldBase>();
            var fieldIds = new HashSet<string>();

            try
            {
                if (fieldsElement.ValueKind != JsonValueKind.Array)
                {
                    return result.SetFailureResult(
                        "ParseFields:INVALID_FORMAT_ARRAY",
                        "userIntegrationFields must be an array"
                    );
                }

                int index = 0;
                foreach (var fieldJson in fieldsElement.EnumerateArray())
                {
                    index++;

                    // 1. Basic Parsing & Required Checks
                    var id = GetString(fieldJson, "id");
                    var name = GetString(fieldJson, "name");
                    var type = GetString(fieldJson, "type");

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        return result.SetFailureResult(
                            "ParseFields:MISSING_ID",
                            $"Field at index {index} is missing 'id'"
                        );
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return result.SetFailureResult(
                            "ParseFields:MISSING_NAME",
                            $"Field '{id}' is missing 'name'"
                        );
                    }

                    if (string.IsNullOrWhiteSpace(type))
                    {
                        return result.SetFailureResult(
                            "ParseFields:MISSING_TYPE",
                            $"Field '{id}' is missing 'type'"
                        );
                    }

                    // Duplicate ID Check
                    if (fieldIds.Contains(id))
                    {
                        return result.SetFailureResult(
                            "ParseFields:DUPLICATE_ID",
                            $"Duplicate Field ID detected: '{id}'"
                        );
                    }

                    fieldIds.Add(id);

                    // 2. Construct Base Object
                    var field = new ProviderFieldBase
                    {
                        Id = id,
                        Name = name,
                        Type = type,
                        Tooltip = GetString(fieldJson, "tooltip"),
                        Placeholder = GetString(fieldJson, "placeholder"),
                        DefaultValue = GetString(fieldJson, "defaultValue"),
                        Required = GetBool(fieldJson, "required"),
                        IsEncrypted = GetBool(fieldJson, "isEncrypted"),
                        IsArray = GetBool(fieldJson, "isArray"),

                        // Numeric Constraints
                        MinNumberValue = GetDoubleOrNull(fieldJson, "minNumberValue"),
                        MaxNumberValue = GetDoubleOrNull(fieldJson, "maxNumberValue"),
                        DecimalPlaces = GetIntOrNull(fieldJson, "decimalPlaces"),

                        // Text Constraints
                        StringRegex = GetString(fieldJson, "stringRegex"),

                        // Array Constraints
                        MinArrayCount = GetIntOrNull(fieldJson, "minArrayCount"),
                        MaxArrayCount = GetIntOrNull(fieldJson, "maxArrayCount")
                    };

                    // 3. Parse Options (for Select/Models)
                    if (fieldJson.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                    {
                        field.Options = new List<ProviderFieldOption>();
                        foreach (var opt in optionsElement.EnumerateArray())
                        {
                            field.Options.Add(new ProviderFieldOption
                            {
                                Key = GetString(opt, "key"),
                                Value = GetString(opt, "value"),
                                IsDefault = GetBool(opt, "isDefault")
                            });
                        }
                    }

                    // 4. Parse Model Conditions
                    if (
                        fieldJson.TryGetProperty("modelCondition", out var modelCondElement) &&
                        modelCondElement.ValueKind == JsonValueKind.Object
                    ) {
                        var modelList = new List<string>();
                        if (
                            modelCondElement.TryGetProperty("models", out var modelsArr)
                            && modelsArr.ValueKind == JsonValueKind.Array
                        ) {
                            foreach (var m in modelsArr.EnumerateArray())
                            {
                                var modelId = m.GetString();
                                if (!string.IsNullOrEmpty(modelId))
                                {
                                    // Validation: Check if model exists in the provider's definition
                                    if (
                                        availableModelIds != null &&
                                        !availableModelIds.Contains(modelId)
                                    ) {
                                        return result.SetFailureResult(
                                            "ParseFields:MODEL_CONDITION_MODEL_NOT_FOUND",
                                            $"Field '{id}' references non-existent model ID: '{modelId}'"
                                        );
                                    }
                                    modelList.Add(modelId);
                                }
                            }
                        }

                        field.ModelCondition = new ProviderFieldModelCondition
                        {
                            Type = (ProviderFieldModelConitionType)GetInt(modelCondElement, "type", 0),
                            Models = modelList
                        };
                    }

                    // 5. Parse Field Conditions
                    if (
                        fieldJson.TryGetProperty("fieldConditions", out var fieldCondElement)
                        && fieldCondElement.ValueKind == JsonValueKind.Array
                    ) {
                        field.FieldConditions = new List<ProviderFieldFieldCondition>();
                        foreach (var cond in fieldCondElement.EnumerateArray())
                        {
                            var targetFieldId = GetString(cond, "fieldId");
                            if (string.IsNullOrWhiteSpace(targetFieldId)) continue;

                            field.FieldConditions.Add(new ProviderFieldFieldCondition
                            {
                                FieldId = targetFieldId,
                                Type = (ProviderFieldFieldConitionType)GetInt(cond, "type", 0),
                                Visibility = (ProviderFieldFieldConitionVisibility)GetInt(cond, "visibility", 0),
                                Value = GetString(cond, "value") 
                            });
                        }
                    }

                    fieldsList.Add(field);
                }

                // 6. Post-Parsing Validation: Logic Integrity
                foreach (var field in fieldsList)
                {
                    if (field.FieldConditions != null)
                    {
                        foreach (var cond in field.FieldConditions)
                        {
                            // Validate that the target field exists in the list
                            if (!fieldIds.Contains(cond.FieldId))
                            {
                                return result.SetFailureResult(
                                    "ParseFields:FIELD_CONDITION_FIELD_NOT_FOUND",
                                    $"Field '{field.Id}' has a condition on missing Field ID: '{cond.FieldId}'"
                                );
                            }

                            // Prevent self-reference
                            if (cond.FieldId == field.Id)
                            {
                                return result.SetFailureResult(
                                    "ParseFields:FIELD_CONDITION_SELF_REFERENCE",
                                    $"Field '{field.Id}' cannot have a condition on itself."
                                );
                            }
                        }
                    }
                }

                return result.SetSuccessResult(fieldsList);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ParseFields:EXCEPTION",
                    $"Error parsing fields: {ex.Message}"
                );
            }
        }

        // --- Helpers ---

        private static string GetString(JsonElement el, string prop)
        {
            if (
                el.TryGetProperty(prop, out var val) &&
                val.ValueKind == JsonValueKind.String
            ) {
                return val.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static bool GetBool(JsonElement el, string prop)
        {
            return el.TryGetProperty(prop, out var val) && (val.ValueKind == JsonValueKind.True || val.ValueKind == JsonValueKind.False) && val.GetBoolean();
        }

        private static int GetInt(JsonElement el, string prop, int defaultVal = 0)
        {
           if (
              el.TryGetProperty(prop, out var val) &&
              val.ValueKind == JsonValueKind.Number &&
              val.TryGetInt32(out int i)
           ) {
                return i;
           }

            return defaultVal;
        }

        private static double? GetDoubleOrNull(JsonElement el, string prop)
        {
            if (
                el.TryGetProperty(prop, out var val) &&
                val.ValueKind == JsonValueKind.Number &&
                val.TryGetDouble(out double d)
            ) {
                return d;
            }

            return null;
        }

        private static int? GetIntOrNull(JsonElement el, string prop)
        {
            if (
                el.TryGetProperty(prop, out var val) &&
                val.ValueKind == JsonValueKind.Number &&
                val.TryGetInt32(out int i)
            ) {
                return i;
            }

            return null;
        }
    }
}