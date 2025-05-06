using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using System.Text.Json;

namespace IqraCore.Utilities
{
    public static class BusinessAppToolPropertyValidator
    {
        public static FunctionReturnResult<object> ValidateArgumentValue(string businessDefaultLanguage, JsonElement value, BusinessAppToolConfigurationInputSchemea argument, string actionType)
        {
            var result = new FunctionReturnResult<object>();

            switch (argument.Type)
            {
                case BusinessAppToolConfigurationInputSchemeaTypeEnum.String:
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        result.Code = "ValidateArgumentValue:1";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected string.";
                        return result;
                    }

                    string? stringValue = value.GetString();
                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        if (argument.IsRequired)
                        {
                            result.Code = "ValidateArgumentValue:2";
                            result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} value is empty but it is required.";
                            return result;
                        }
                        result.Data = string.Empty;
                        break;
                    }

                    result.Data = stringValue;
                    break;

                case BusinessAppToolConfigurationInputSchemeaTypeEnum.Number:
                    if (value.ValueKind != JsonValueKind.Number && argument.IsRequired)
                    {
                        result.Code = "ValidateArgumentValue:3";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected number.";
                        return result;
                    }
                    else if (value.ValueKind == JsonValueKind.String)
                    {
                        result.Data = string.Empty;
                        break;
                    }

                    if (!value.TryGetDouble(out var numberValue))
                    {
                        result.Code = "ValidateArgumentValue:4";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} value type mismatch.";
                        return result;
                    }

                    result.Data = numberValue;
                    break;

                case BusinessAppToolConfigurationInputSchemeaTypeEnum.Boolean:
                    if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False && argument.IsRequired)
                    {
                        result.Code = "ValidateArgumentValue:5";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected boolean.";
                        return result;
                    }
                    else if (value.ValueKind == JsonValueKind.String)
                    {
                        result.Data = string.Empty;
                        break;
                    }

                    result.Data = value.GetBoolean();
                    break;

                case BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime:
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        result.Code = "ValidateArgumentValue:6";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected date time string.";
                        return result;
                    }

                    string? dateTimeString = value.GetString();
                    if (string.IsNullOrWhiteSpace(dateTimeString))
                    {
                        if (argument.IsRequired)
                        {
                            result.Code = "ValidateArgumentValue:7";
                            result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} value is empty but it is required.";
                            return result;
                        }
                        result.Data = string.Empty;
                        break;
                    }

                    result.Data = dateTimeString;
                    break;

                default:
                    result.Code = "ValidateArgumentValue:9";
                    result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} has unknown type.";
                    return result;
            }

            result.Success = true;
            return result;
        }

        public static FunctionReturnResult<object> ValidateArgumentValue(string businessDefaultLanguage, object? value, BusinessAppToolConfigurationInputSchemea argument, string actionType)
        {
            var result = new FunctionReturnResult<object>();

            if (value == null)
            {
                if (argument.IsRequired)
                {
                    switch (argument.Type)
                    {
                        case BusinessAppToolConfigurationInputSchemeaTypeEnum.String:
                            result.Code = "ValidateArgumentValue:2";
                            break;
                        case BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime:
                            result.Code = "ValidateArgumentValue:7";
                            break;
                        case BusinessAppToolConfigurationInputSchemeaTypeEnum.Number:
                            result.Code = "ValidateArgumentValue:3";
                            result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} is null but it is required (expected number).";
                            return result;
                        case BusinessAppToolConfigurationInputSchemeaTypeEnum.Boolean:
                            result.Code = "ValidateArgumentValue:5";
                            result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} is null but it is required (expected boolean).";
                            return result;
                        default:
                            result.Code = "ValidateArgumentValue:GEN_REQ_NULL";
                            break;
                    }
                    if (argument.Type == BusinessAppToolConfigurationInputSchemeaTypeEnum.String || argument.Type == BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime)
                    {
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} value is empty but it is required.";
                    }
                    return result;
                }
                else
                {
                    switch (argument.Type)
                    {
                        case BusinessAppToolConfigurationInputSchemeaTypeEnum.String:
                        case BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime:
                            result.Data = string.Empty;
                            break;
                        case BusinessAppToolConfigurationInputSchemeaTypeEnum.Number:
                            result.Data = string.Empty;
                            break;
                        case BusinessAppToolConfigurationInputSchemeaTypeEnum.Boolean:
                            result.Data = string.Empty;
                            break;
                        default:
                            result.Data = null;
                            break;
                    }
                    result.Success = true;
                    return result;
                }
            }

            switch (argument.Type)
            {
                case BusinessAppToolConfigurationInputSchemeaTypeEnum.String:
                    if (value is not string stringValue)
                    {
                        result.Code = "ValidateArgumentValue:1";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected string (received {value.GetType().Name}).";
                        return result;
                    }

                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        if (argument.IsRequired)
                        {
                            result.Code = "ValidateArgumentValue:2";
                            result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} value is empty but it is required.";
                            return result;
                        }
                        result.Data = string.Empty;
                        break;
                    }

                    result.Data = stringValue;
                    break;

                case BusinessAppToolConfigurationInputSchemeaTypeEnum.Number:
                    if (value is double d_val) result.Data = d_val;
                    else if (value is int i_val) result.Data = (double)i_val;
                    else if (value is long l_val) result.Data = (double)l_val;
                    else if (value is float f_val) result.Data = (double)f_val;
                    else if (value is decimal dec_val) result.Data = Convert.ToDouble(dec_val);
                    else if (value is short s_val) result.Data = (double)s_val;
                    else if (value is byte b_val) result.Data = (double)b_val;
                    else if (value is string && !argument.IsRequired)
                    {
                        result.Data = string.Empty;
                    }
                    else
                    {
                        result.Code = argument.IsRequired ? "ValidateArgumentValue:3" : "ValidateArgumentValue:4";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected number (received {value.GetType().Name}).";
                        return result;
                    }
                    break;

                case BusinessAppToolConfigurationInputSchemeaTypeEnum.Boolean:
                    if (value is bool boolValue)
                    {
                        result.Data = boolValue;
                    }
                    else if (value is string && !argument.IsRequired)
                    {
                        result.Data = string.Empty;
                    }
                    else
                    {
                        result.Code = "ValidateArgumentValue:5";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected boolean (received {value.GetType().Name}).";
                        return result;
                    }
                    break;

                case BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime:
                    if (value is not string dateTimeString)
                    {
                        result.Code = "ValidateArgumentValue:6";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected date time string (received {value.GetType().Name}).";
                        return result;
                    }

                    if (string.IsNullOrWhiteSpace(dateTimeString))
                    {
                        if (argument.IsRequired)
                        {
                            result.Code = "ValidateArgumentValue:7";
                            result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} value is empty but it is required.";
                            return result;
                        }
                        result.Data = string.Empty;
                        break;
                    }
                    result.Data = dateTimeString;
                    break;

                default:
                    result.Code = "ValidateArgumentValue:9";
                    result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} has unknown type ({argument.Type}).";
                    return result;
            }

            result.Success = true;
            return result;
        }
    }
}
