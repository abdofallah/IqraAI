using IqraCore.Entities.Business;
using IqraCore.Entities.Helper;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraCore.Utilities.Audio;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IqraInfrastructure.Services.Business
{
    public class BusinessToolsManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;
        private readonly BusinessToolAudioRepository _businessToolAudioRepository;

        private readonly AudioFileProcessor _audioProcessor;

        public BusinessToolsManager(BusinessManager businessManager, BusinessAppRepository businessAppRepository, BusinessRepository businessRepository, BusinessToolAudioRepository businessToolAudioRepository, AudioFileProcessor audioProcessor)
        {
            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
            _businessToolAudioRepository = businessToolAudioRepository;

            _audioProcessor = audioProcessor;
        }

        /**
         * 
         * Tools Tab
         * 
        **/

        public async Task<bool> CheckBusinessToolExists(long businessId, string toolId)
        {
            var result = await _businessAppRepository.CheckBusinessAppToolExists(businessId, toolId);

            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppTool?>> AddOrUpdateUserBusinessTools(long businessId, IFormCollection formData, string postType, string? exisitingToolId)
        {
            var result = new FunctionReturnResult<BusinessAppTool?>();

            List<string> businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateUserBusinessTools:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateUserBusinessTools:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var NewBusinessAppToolData = new BusinessAppTool();

            // General Tab
            if (!changes.RootElement.TryGetProperty("general", out var generalTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessTools:3";
                result.Message = "General tab not found.";
                return result;
            }
            else
            {
                var generalNameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                generalTabRootElement,
                "name",
                NewBusinessAppToolData.General.Name
            );
                if (!generalNameValidationResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessTools:" + generalNameValidationResult.Code;
                    result.Message = generalNameValidationResult.Message;
                    return result;
                }

                var generalShortDescriptionValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    generalTabRootElement,
                    "shortDescription",
                    NewBusinessAppToolData.General.ShortDescription
                );
                if (!generalShortDescriptionValidationResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessTools:" + generalShortDescriptionValidationResult.Code;
                    result.Message = generalShortDescriptionValidationResult.Message;
                    return result;
                }
            }

            // Configuration Tab
            if (!changes.RootElement.TryGetProperty("configuration", out var configurationTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessTools:4";
                result.Message = "Configuration tab not found.";
                return result;
            }
            else
            {
                if (!configurationTabRootElement.TryGetProperty("inputSchemea", out var configurationTabInputSchemeaProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:5";
                    result.Message = "Configuration tab input scheme property not found.";
                    return result;
                }
                foreach (var inputScheme in configurationTabInputSchemeaProperty.EnumerateArray())
                {
                    BusinessAppToolConfigurationInputSchemea newInputSchemeaData = new BusinessAppToolConfigurationInputSchemea();

                    var inputSchemeNameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                        businessLanguages,
                        inputScheme,
                        "name",
                        newInputSchemeaData.Name
                    );
                    if (!inputSchemeNameValidationResult.Success)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:" + inputSchemeNameValidationResult.Code;
                        result.Message = inputSchemeNameValidationResult.Message;
                        return result;
                    }

                    var inputSchemeDescriptionValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                        businessLanguages,
                        inputScheme,
                        "description",
                        newInputSchemeaData.Description
                    );
                    if (!inputSchemeDescriptionValidationResult.Success)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:" + inputSchemeDescriptionValidationResult.Code;
                        result.Message = inputSchemeDescriptionValidationResult.Message;
                        return result;
                    }

                    if (!inputScheme.TryGetProperty("type", out var inputSchemeTypeProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:6";
                        result.Message = "Configuration tab input scheme type property not found.";
                        return result;
                    }
                    if (!inputSchemeTypeProperty.TryGetInt32(out var inputSchemeTypeInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:7";
                        result.Message = "Invaldi configuration tab input scheme value.";
                        return result;
                    }
                    if (!Enum.IsDefined(typeof(BusinessAppToolConfigurationInputSchemeaTypeEnum), inputSchemeTypeInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:7";
                        result.Message = "Configuration tab input scheme type not found.";
                        return result;
                    }
                    newInputSchemeaData.Type = (BusinessAppToolConfigurationInputSchemeaTypeEnum)inputSchemeTypeInt;

                    if (!inputScheme.TryGetProperty("isArray", out var isArrayProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:8";
                        result.Message = "Configuration tab input scheme isArray property not found.";
                        return result;
                    }
                    newInputSchemeaData.IsArray = isArrayProperty.GetBoolean();

                    if (!inputScheme.TryGetProperty("isRequired", out var isRequiredProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:9";
                        result.Message = "Configuration tab input scheme isRequired property not found.";
                        return result;
                    }
                    newInputSchemeaData.IsRequired = isRequiredProperty.GetBoolean();

                    newInputSchemeaData.Id = Guid.NewGuid().ToString(); // todo make more name friendly

                    NewBusinessAppToolData.Configuration.InputSchemea.Add(newInputSchemeaData);
                }

                if (!configurationTabRootElement.TryGetProperty("requestType", out var configurationRequestTypeProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:10";
                    result.Message = "Configuration tab configuration request type property not found.";
                    return result;
                }
                if (!configurationRequestTypeProperty.TryGetInt32(out var configurationRequestTypeInt))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:11";
                    result.Message = "Invaldi configuration tab configuration request type value.";
                    return result;
                }
                if (!Enum.IsDefined(typeof(HttpMethodEnum), configurationRequestTypeInt))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:12";
                    result.Message = "Configuration tab configuration request type not found.";
                    return result;
                }
                NewBusinessAppToolData.Configuration.RequestType = (HttpMethodEnum)configurationRequestTypeInt;

                if (!configurationTabRootElement.TryGetProperty("endpoint", out var configurationEndpointUrlProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:13";
                    result.Message = "Configuration tab endpoint property not found.";
                    return result;
                }
                string? configurationEndpointUrl = configurationEndpointUrlProperty.GetString();
                if (string.IsNullOrWhiteSpace(configurationEndpointUrl))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:14";
                    result.Message = "Configuration tab endpoint is empty.";
                    return result;
                }
                NewBusinessAppToolData.Configuration.Endpoint = configurationEndpointUrl;

                if (!configurationTabRootElement.TryGetProperty("headers", out var configurationHeadersProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:15";
                    result.Message = "Configuration tab headers property not found.";
                    return result;
                }
                foreach (var header in configurationHeadersProperty.EnumerateObject())
                {
                    string? headerKey = header.Name;
                    string? headerValue = header.Value.GetString();
                    if (string.IsNullOrWhiteSpace(headerKey) || string.IsNullOrWhiteSpace(headerValue))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:16";
                        result.Message = "Configuration tab header key or value is empty.";
                        return result;
                    }

                    if (NewBusinessAppToolData.Configuration.Headers.ContainsKey(headerKey))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:17";
                        result.Message = $"Configuration tab header key {headerKey} already exists.";
                        return result;
                    }

                    NewBusinessAppToolData.Configuration.Headers.Add(headerKey, headerValue);
                }

                if (!configurationTabRootElement.TryGetProperty("bodyType", out var configurationBodyTypeProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:18";
                    result.Message = "Configuration tab body type property not found.";
                    return result;
                }
                if (!configurationBodyTypeProperty.TryGetInt32(out var configurationBodyTypeInt))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:19";
                    result.Message = "Invaldi configuration tab body type value.";
                    return result;
                }
                if (!Enum.IsDefined(typeof(HttpBodyEnum), configurationBodyTypeInt))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:20";
                    result.Message = "Configuration tab body type not found.";
                    return result;
                }
                NewBusinessAppToolData.Configuration.BodyType = (HttpBodyEnum)configurationBodyTypeInt;

                switch (NewBusinessAppToolData.Configuration.BodyType)
                {
                    case HttpBodyEnum.None:
                        break;

                    case HttpBodyEnum.Raw:
                        if (!configurationTabRootElement.TryGetProperty("bodyData", out var configurationbodyRawDataProperty))
                        {
                            result.Code = "AddOrUpdateUserBusinessTools:22";
                            result.Message = "Configuration tab raw body data property not found.";
                            return result;
                        }

                        string? configurationbodyRawDataString = configurationbodyRawDataProperty.GetString();

                        if (string.IsNullOrWhiteSpace(configurationbodyRawDataString))
                        {
                            result.Code = "AddOrUpdateUserBusinessTools:23";
                            result.Message = "Configuration tab raw body data is empty.";
                            return result;
                        }

                        NewBusinessAppToolData.Configuration.BodyData = configurationbodyRawDataString;
                        break;

                    case HttpBodyEnum.FormData:
                    case HttpBodyEnum.XWWWFormUrlencoded:
                        if (!configurationTabRootElement.TryGetProperty("bodyData", out var configurationbodyFormDataProperty))
                        {
                            result.Code = "AddOrUpdateUserBusinessTools:22";
                            result.Message = "Configuration tab form body data property not found.";
                            return result;
                        }

                        Dictionary<string, string> configurationBodyFormData = new Dictionary<string, string>();

                        foreach (var bodyFormData in configurationbodyFormDataProperty.EnumerateObject())
                        {
                            var key = bodyFormData.Name;
                            var value = bodyFormData.Value.GetString();

                            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                            {
                                result.Code = "AddOrUpdateUserBusinessTools:24";
                                result.Message = "Configuration tab form body data key or value is empty.";
                                return result;
                            }

                            if (configurationBodyFormData.ContainsKey(key))
                            {
                                result.Code = "AddOrUpdateUserBusinessTools:25";
                                result.Message = $"Configuration tab form body data key {key} already exists.";
                                return result;
                            }

                            configurationBodyFormData.Add(key, value);
                        }

                        NewBusinessAppToolData.Configuration.BodyData = configurationBodyFormData;
                        break;
                }
            }

            // Response Tab
            if (!changes.RootElement.TryGetProperty("response", out var responseTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessTools:26";
                result.Message = "Response tab root element not found.";
                return result;
            }
            else
            {
                foreach (var responseData in responseTabRootElement.EnumerateObject())
                {
                    var statusKey = responseData.Name;
                    var responseDataValue = responseData.Value;

                    if (string.IsNullOrWhiteSpace(statusKey))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:27";
                        result.Message = "Response tab status key is empty.";
                        return result;
                    }
                    if (!int.TryParse(statusKey, out var statusKeyInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:28";
                        result.Message = "Invalid response tab status key value.";
                        return result;
                    }
                    if (!Enum.IsDefined(typeof(HttpStatusEnum), statusKeyInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:29";
                        result.Message = "Response tab status key not found.";
                        return result;
                    }

                    if (NewBusinessAppToolData.Response.ContainsKey((HttpStatusEnum)statusKeyInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:30";
                        result.Message = $"Response tab status key {(HttpStatusEnum)statusKeyInt} already exists.";
                        return result;
                    }

                    BusinessAppToolResponse newToolResponseData = new BusinessAppToolResponse();

                    if (!responseDataValue.TryGetProperty("javascript", out var responseJavascriptProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:31";
                        result.Message = "Response tab javascript property not found.";
                        return result;
                    }
                    string? responseJavascriptString = responseJavascriptProperty.GetString();
                    if (string.IsNullOrWhiteSpace(responseJavascriptString))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:32";
                        result.Message = "Response tab javascript is empty.";
                        return result;
                    }
                    // TODO validate that javascript is valid returning a value
                    newToolResponseData.Javascript = responseJavascriptString;

                    if (!responseDataValue.TryGetProperty("hasStaticResponse", out var hasStaticResponseProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:33";
                        result.Message = "Response tab has static response property not found.";
                        return result;
                    }
                    newToolResponseData.HasStaticResponse = hasStaticResponseProperty.GetBoolean();

                    if (newToolResponseData.HasStaticResponse)
                    {
                        newToolResponseData.StaticResponse = new Dictionary<string, string>();
                        var responseStaticResponeValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                            businessLanguages,
                            responseDataValue,
                            "staticResponse",
                            newToolResponseData.StaticResponse
                        );
                        if (!responseStaticResponeValidationResult.Success)
                        {
                            result.Code = "AddOrUpdateUserBusinessTools:" + responseStaticResponeValidationResult.Code;
                            result.Message = responseStaticResponeValidationResult.Message;
                            return result;
                        }
                    }

                    NewBusinessAppToolData.Response.Add((HttpStatusEnum)statusKeyInt, newToolResponseData);
                }
            }

            // Audio Tab
            if (formData.Files.Count > 0)
            {
                var beforeSpeakingAudio = formData.Files.GetFile("audioBeforeSpeaking");
                var duringSpeakingAudio = formData.Files.GetFile("audioDuringSpeaking");
                var afterSpeakingAudio = formData.Files.GetFile("audioAfterSpeaking");

                if (beforeSpeakingAudio != null)
                {
                    var validationResult = await _audioProcessor.ValidateAudioFile(beforeSpeakingAudio);
                    if (!validationResult.IsValid)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:34";
                        result.Message = $"Before speaking audio validation failed: {validationResult.ErrorMessage}.";
                        return result;
                    }

                    bool fileExists = await _businessToolAudioRepository.FileExists(validationResult.Hash);
                    if (!fileExists)
                    {
                        var metadata = new Dictionary<string, string>
                        {
                            { "fileContentType", validationResult.ContentType }
                        };

                        await _businessToolAudioRepository.PutFileAsByteData(
                            validationResult.Hash,
                            validationResult.FileBytes,
                            metadata
                        );
                    }

                    NewBusinessAppToolData.Audio.BeforeSpeaking = validationResult.Hash;
                }

                if (duringSpeakingAudio != null)
                {
                    var validationResult = await _audioProcessor.ValidateAudioFile(duringSpeakingAudio);
                    if (!validationResult.IsValid)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:35";
                        result.Message = $"During speaking audio validation failed: {validationResult.ErrorMessage}.";
                        return result;
                    }

                    bool fileExists = await _businessToolAudioRepository.FileExists(validationResult.Hash);
                    if (!fileExists)
                    {
                        var metadata = new Dictionary<string, string>
                        {
                            { "ContentType", validationResult.ContentType }
                        };

                        await _businessToolAudioRepository.PutFileAsByteData(
                            validationResult.Hash,
                            validationResult.FileBytes,
                            metadata
                        );
                    }

                    NewBusinessAppToolData.Audio.DuringSpeaking = validationResult.Hash;
                }

                if (afterSpeakingAudio != null)
                {
                    var validationResult = await _audioProcessor.ValidateAudioFile(afterSpeakingAudio);
                    if (!validationResult.IsValid)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:36";
                        result.Message = $"After speaking audio validation failed: {validationResult.ErrorMessage}.";
                        return result;
                    }

                    bool fileExists = await _businessToolAudioRepository.FileExists(validationResult.Hash);
                    if (!fileExists)
                    {
                        var metadata = new Dictionary<string, string>
                        {
                            { "ContentType", validationResult.ContentType }
                        };

                        await _businessToolAudioRepository.PutFileAsByteData(
                            validationResult.Hash,
                            validationResult.FileBytes,
                            metadata
                        );
                    }

                    NewBusinessAppToolData.Audio.AfterSpeaking = validationResult.Hash;
                }
            }

            // Saving or Adding to Database
            if (postType == "new")
            {
                NewBusinessAppToolData.Id = Guid.NewGuid().ToString();

                var addBusinessAppToolResult = await _businessAppRepository.AddBusinessAppTool(businessId, NewBusinessAppToolData);
                if (!addBusinessAppToolResult)
                {
                    result.Code = "AddOrUpdateUserBusinessTools:37";
                    result.Message = "Failed to add business app tool.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                NewBusinessAppToolData.Id = exisitingToolId;

                var saveBusinessAppToolResult = await _businessAppRepository.UpdateBusinessAppTool(businessId, NewBusinessAppToolData);
                if (!saveBusinessAppToolResult)
                {
                    result.Code = "AddOrUpdateUserBusinessTools:38";
                    result.Message = "Failed to save business app tool.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = NewBusinessAppToolData;

            return result;
        }

    }
}
