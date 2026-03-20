using System.Text.Json;
using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Repositories.Business;

namespace IqraInfrastructure.Helpers.Business
{
    public static class BusinessCampaignActionValidatorHelper
    {
        public static async Task<FunctionReturnResult<BusinessAppCampaignActionConfig>> ValidateBusinessCampaignActionData(
            long businessId,
            string businessDefaultLanguage,
            JsonElement actionToolElement,
            string actionType,
            List<CustomVariableInputTemplateVariableDefinition> argumentList,
            BusinessAppRepository businessAppRepository)
        {
            var result = new FunctionReturnResult<BusinessAppCampaignActionConfig>();
            var resultData = new BusinessAppCampaignActionConfig();

            if (!actionToolElement.TryGetProperty("toolId", out var toolIdProperty))
            {
                return result.SetFailureResult(
                    "ValidateBusinessCampaignActionData:TOOL_ID_NOT_FOUND",
                    $"{actionType} tool id not found. Must be null or id of the tool."
                );
            }

            string? toolId = toolIdProperty.GetString();
            if (toolId == null)
            {
                return result.SetSuccessResult(resultData);
            }

            var selectedToolData = await businessAppRepository.GetBusinessAppTool(businessId, toolId);
            if (selectedToolData == null)
            {
                return result.SetFailureResult(
                    "ValidateBusinessCampaignActionData:TOOL_NOT_FOUND",
                    $"{actionType} tool not found in business."
                );
            }
            resultData.ToolId = toolId;
            resultData.Arguments = new Dictionary<string, object>();

            if (!actionToolElement.TryGetProperty("arguments", out var argumentsProperty) || argumentsProperty.ValueKind == JsonValueKind.Null)
            {
                if (selectedToolData.Configuration.InputSchemea.Any(arg => arg.IsRequired))
                {
                    return result.SetFailureResult(
                        "ValidateBusinessCampaignActionData:ARGS_MISSING_BUT_REQUIRED",
                        $"{actionType} tool arguments not found, but required arguments exist."
                    );
                }
            }
            else if (argumentsProperty.ValueKind != JsonValueKind.Object)
            {
                return result.SetFailureResult(
                    "ValidateBusinessCampaignActionData:ARGS_NOT_OBJECT",
                    $"{actionType} tool 'arguments' must be an object."
                );
            }
            else
            {
                foreach (var toolInputArgument in selectedToolData.Configuration.InputSchemea)
                {
                    var propertyFound = argumentsProperty.TryGetProperty(toolInputArgument.Id, out var argumentValueProperty);

                    if (!propertyFound || argumentValueProperty.ValueKind == JsonValueKind.Null)
                    {
                        if (toolInputArgument.IsRequired)
                        {
                            return result.SetFailureResult(
                                "ValidateBusinessCampaignActionData:REQUIRED_ARG_MISSING",
                                $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} is missing. Required fields must not be empty."
                            );
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (argumentValueProperty.ValueKind != JsonValueKind.String)
                        {
                            return result.SetFailureResult(
                                "ValidateBusinessCampaignActionData:ARG_NOT_STRING",
                                $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} 'value' must be a string."
                            );
                        }

                        var argumentValue = argumentValueProperty.GetString();

                        if (string.IsNullOrWhiteSpace(argumentValue) && toolInputArgument.IsRequired)
                        {
                            return result.SetFailureResult(
                                "ValidateBusinessCampaignActionData:REQUIRED_ARG_MISSING",
                                $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} is empty. Required fields must not be empty."
                            );
                        }

                        if (!string.IsNullOrWhiteSpace(argumentValue))
                        {
                            var valueTemplateValidation = CustomVariableInputTemplateService.Validate(argumentValue, argumentList);
                            if (!valueTemplateValidation.IsValid)
                            {
                                return result.SetFailureResult(
                                    "ValidateBusinessCampaignActionData:ACTION_ARG_VARIABLE_VALUE_INVALID",
                                    $"{actionType} tool input argument 'value' is invalid:\n\n{string.Join("\n", valueTemplateValidation.Errors)}"
                                );
                            }
                        }

                        resultData.Arguments.Add(toolInputArgument.Id, argumentValue ?? string.Empty);
                    }
                }
            }

            return result.SetSuccessResult(resultData);
        }
    }
}