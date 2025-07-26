using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.LLM;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.STT;
using IqraCore.Entities.TTS;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using System.Text.Json;

namespace IqraInfrastructure.Helpers.Business
{
    public class IntegrationConfigurationManager
    {
        private bool _dependenciesSetup = false;

        private BusinessIntegrationsManager _businessIntegrationsManager;

        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly LLMProviderManager _llmProviderManager;

        public IntegrationConfigurationManager(STTProviderManager sttProviderManager, TTSProviderManager ttsProviderManager, LLMProviderManager llmProviderManager)
        {
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
            _llmProviderManager = llmProviderManager;
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
            string? businessLanguage = null
        )
        {
            var result = new FunctionReturnResult<BusinessAppAgentIntegrationData>();

            if (!_dependenciesSetup)
            {
                result.Code = "ValidateIntegrationData:MISSING_DEPENDENCIES";
                result.Message = $"Dependencies for integration configuration managers are not setup.";
                return result;
            }

            if (!currentIntegrationElement.TryGetProperty("id", out var integrationIdElement))
            {
                result.Code = "ValidateIntegrationData:1";
                result.Message = $"{integrationType} integration id.";
                return result;
            }

            var integrationId = integrationIdElement.GetString();
            if (string.IsNullOrWhiteSpace(integrationId))
            {
                result.Code = "ValidateIntegrationData:2";
                result.Message = $"{integrationType} integration id is empty.";
                return result;
            }

            if (!currentIntegrationElement.TryGetProperty("fieldValues", out var fieldValuesElement))
            {
                result.Code = "ValidateIntegrationData:3";
                result.Message = $"{integrationType} field values not found in integration.";
                return result;
            }

            return await ValidateAndBuildIntegrationData(businessId, integrationId, fieldValuesElement, integrationType, businessLanguage);
        }

        public async Task<FunctionReturnResult<BusinessAppAgentIntegrationData>> ValidateAndBuildIntegrationData(
            long businessId,
            string integrationId,
            JsonElement fieldValuesElement,
            string integrationType,
            string? businessLanguage = null
        )
        {
            var result = new FunctionReturnResult<BusinessAppAgentIntegrationData>();

            if (!_dependenciesSetup)
            {
                result.Code = "ValidateIntegrationData:MISSING_DEPENDENCIES";
                result.Message = $"Dependencies for integration configuration managers are not setup.";
                return result;
            }

            var currentIntegrationResult = await _businessIntegrationsManager.getBusinessIntegrationById(businessId, integrationId);
            if (!currentIntegrationResult.Success)
            {
                result.Code = "ValidateIntegrationData:" + currentIntegrationResult.Code;
                result.Message = currentIntegrationResult.Message;
                return result;
            }

            dynamic providerManager;
            if (integrationType == "STT")
            {
                providerManager = _sttProviderManager;
            }
            else if (integrationType == "TTS")
            {
                providerManager = _ttsProviderManager;
            }
            else if (integrationType == "LLM")
            {
                providerManager = _llmProviderManager;
            }
            else
            {
                result.Code = "ValidateIntegrationData:4";
                result.Message = $"Unknown integration type: {integrationType}.";
                return result;
            }

            var providerData = await providerManager.GetProviderDataByIntegration(currentIntegrationResult.Data.Type);
            if (!providerData.Success)
            {
                result.Code = "ValidateIntegrationData:" + providerData.Code;
                result.Message = providerData.Message;
                return result;
            }

            var newIntegrationData = new BusinessAppAgentIntegrationData()
            {
                Id = integrationId,
            };          

            IEnumerable<ProviderFieldBase> userIntegrationFields;
            IEnumerable<ProviderModelBase> models;

            if (integrationType == "STT")
            {
                var sttData = providerData.Data as STTProviderData;
                userIntegrationFields = sttData.UserIntegrationFields;
                models = sttData.Models;
            }
            else if (integrationType == "TTS")
            {
                var ttsData = providerData.Data as TTSProviderData;
                userIntegrationFields = ttsData.UserIntegrationFields;
                models = ttsData.Models.Cast<TTSProviderSpeakerData>();
            }
            else if (integrationType == "LLM")
            {
                var llmData = providerData.Data as LLMProviderData;
                userIntegrationFields = llmData.UserIntegrationFields;
                models = llmData.Models;
            }
            else
            {
                result.Code = "ValidateIntegrationData:5";
                result.Message = $"Unknown integration type: {integrationType}.";
                return result;
            }

            if (userIntegrationFields == null || models == null)
            {
                result.Code = "ValidateIntegrationData:6";
                result.Message = $"Invalid provider data structure for {integrationType}.";
                return result;
            }

            foreach (var integrationField in userIntegrationFields)
            {
                if (!fieldValuesElement.TryGetProperty(integrationField.Id, out var fieldValueElement))
                {
                    result.Code = "ValidateIntegrationData:5";
                    result.Message = $"{integrationType} field value for field {integrationField.Name} not found in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                    return result;
                }

                if (integrationField.IsEncrypted)
                {
                    result.Code = "ValidateIntegrationData:6";
                    result.Message = $"Encrypted {integrationType} field value for field {integrationField.Name} is not supported in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                    return result;
                }

                switch (integrationField.Type)
                {
                    case "text":
                    case "string":
                        var fieldValueString = fieldValueElement.GetString();
                        if (integrationField.Required && string.IsNullOrWhiteSpace(fieldValueString))
                        {
                            result.Code = "ValidateIntegrationData:7";
                            result.Message = $"{integrationType} string value for field {integrationField.Name} is empty in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                            return result;
                        }
                        newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueString);
                        break;

                    case "select":
                    case "models":
                        var fieldValueOptionKey = fieldValueElement.GetString();
                        if (integrationField.Required && string.IsNullOrWhiteSpace(fieldValueOptionKey))
                        {
                            result.Code = "ValidateIntegrationData:8";
                            result.Message = $"{integrationType} field value for field {integrationField.Name} is empty in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                            return result;
                        }

                        if (integrationField.Type == "select")
                        {
                            if (integrationField.Options == null || integrationField.Options.Find(d => d.Key == fieldValueOptionKey) == null)
                            {
                                result.Code = "ValidateIntegrationData:9";
                                result.Message = $"{integrationType} option value for select field {integrationField.Name} not found in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                return result;
                            }
                        }

                        if (integrationField.Type == "models")
                        {
                            var fieldValueModelData = models.ToList().Find(x => x.Id == fieldValueOptionKey);
                            if (fieldValueModelData == null)
                            {
                                result.Code = "ValidateIntegrationData:10";
                                result.Message = $"{integrationType} model is not found in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                return result;
                            }

                            if (fieldValueModelData.DisabledAt != null)
                            {
                                result.Code = "ValidateIntegrationData:11";
                                result.Message = $"{integrationType} model is disabled in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                return result;
                            }

                            if (integrationType == "TTS")
                            {
                                var ttsSpeaker = fieldValueModelData as TTSProviderSpeakerData;
                                if (ttsSpeaker == null)
                                {
                                    result.Code = "ValidateIntegrationData:12";
                                    result.Message = $"Invalid TTS speaker data structure in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                    return result;
                                }

                                if (businessLanguage == null)
                                {
                                    result.Code = "ValidateIntegrationData:LANGUAGE_NOT_PROVIDED";
                                    result.Message = "Missing business language required for validation.";
                                    return result;
                                }

                                if (!ttsSpeaker.IsMultilingual && !ttsSpeaker.SupportedLanguages.Contains(businessLanguage))
                                {
                                    result.Code = "ValidateIntegrationData:13";
                                    result.Message = $"TTS speaker '{ttsSpeaker.Name}' does not support language '{businessLanguage}' in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                    return result;
                                }
                            }
                        }

                        newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueOptionKey);
                        break;

                    case "number":
                        if (fieldValueElement.ValueKind == JsonValueKind.String)
                        {
                            var fieldValueNumberString = fieldValueElement.GetString();
                            if (integrationField.Required && string.IsNullOrWhiteSpace(fieldValueNumberString))
                            {
                                result.Code = "ValidateIntegrationData:14";
                                result.Message = $"{integrationType} field value for field {integrationField.Name} is empty in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                return result;
                            }

                            if (!int.TryParse(fieldValueNumberString, out var fieldValueNumber))
                            {
                                result.Code = "ValidateIntegrationData:14.5";
                                result.Message = $"Invalid {integrationType} field value for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                return result;
                            }

                            newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueNumber);
                        }
                        else if (fieldValueElement.ValueKind == JsonValueKind.Number)
                        {
                            newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueElement.GetInt32());
                        }
                        else
                        {
                            result.Code = "ValidateIntegrationData:15";
                            result.Message = $"Invalid {integrationType} field value for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                            return result;
                        }
                        break;

                    case "double_number":
                        if (fieldValueElement.ValueKind == JsonValueKind.String)
                        {
                            var fieldValueNumberString = fieldValueElement.GetString();
                            if (integrationField.Required && string.IsNullOrWhiteSpace(fieldValueNumberString))
                            {
                                result.Code = "ValidateIntegrationData:16";
                                result.Message = $"{integrationType} field value for field {integrationField.Name} is empty in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                return result;
                            }

                            if (!double.TryParse(fieldValueNumberString, out var fieldValueNumber))
                            {
                                result.Code = "ValidateIntegrationData:16.5";
                                result.Message = $"Invalid {integrationType} field value for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                return result;
                            }

                            newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueNumber);
                        }
                        else if (fieldValueElement.ValueKind == JsonValueKind.Number)
                        {
                            newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueElement.GetDouble());
                        }
                        else
                        {
                            result.Code = "ValidateIntegrationData:17";
                            result.Message = $"Invalid {integrationType} field value for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                            return result;
                        }
                        break;

                    case "boolean":
                        if (integrationField.Required && fieldValueElement.ValueKind != JsonValueKind.True && fieldValueElement.ValueKind != JsonValueKind.False)
                        {
                            result.Code = "ValidateIntegrationData:18";
                            result.Message = $"Invalid {integrationType} field value for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                            return result;
                        }

                        if (fieldValueElement.ValueKind == JsonValueKind.True || fieldValueElement.ValueKind == JsonValueKind.False || fieldValueElement.ValueKind == JsonValueKind.String || fieldValueElement.ValueKind == JsonValueKind.Number)
                        {
                            bool fieldValueBooleanValid = fieldValueElement.GetBoolean();
                            newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueBooleanValid);
                        }
                        else
                        {
                            newIntegrationData.FieldValues.Add(integrationField.Id, null);
                        }

                        break;

                    default:
                        result.Code = "ValidateIntegrationData:19";
                        result.Message = $"Invalid {integrationType} field type for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                        return result;
                }
            }

            return result.SetSuccessResult(newIntegrationData);
        }
    }
}
