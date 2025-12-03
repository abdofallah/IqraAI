using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using IqraInfrastructure.Managers.Languages;
using IqraCore.Entities.S3Storage;
using IqraInfrastructure.Repositories.S3Storage;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessSettingsManager
    {
        private readonly ILogger<BusinessSettingsManager> _logger;

        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessRepository _businessRepository;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessLogoRepository _businessLogoRepository;
        private readonly LanguagesManager _languagesManager;
        private readonly S3StorageClientFactory _s3StorageClientFactory;

        public BusinessSettingsManager(
            ILogger<BusinessSettingsManager> logger,
            BusinessManager businessManager,
            BusinessRepository businessRepository,
            BusinessAppRepository businessAppRepository,
            BusinessLogoRepository businessLogoRepository,
            LanguagesManager languagesManager,
            S3StorageClientFactory s3StorageClientFactory
        )
        {
            _logger = logger;

            _parentBusinessManager = businessManager;

            _businessRepository = businessRepository;
            _businessAppRepository = businessAppRepository;
            _businessLogoRepository = businessLogoRepository;
            _languagesManager = languagesManager;
            _s3StorageClientFactory = s3StorageClientFactory;
        }

        /**
         * 
         * Settings Tab
         * General
         * 
        **/

        public async Task<FunctionReturnResult<bool?>> UpdateUserBusinessSettings(long businessId, IFormCollection formData)
        {
            var result = new FunctionReturnResult<bool?>();

            bool updateBusinessApp = false;

            BusinessData? businessDataBackup = await _businessRepository.GetBusinessAsync(businessId);

            BusinessData? businessData = await _businessRepository.GetBusinessAsync(businessId);
            BusinessApp? businessApp = await _businessAppRepository.GetBusinessAppAsync(businessId);

            List<UpdateDefinition<BusinessData>> updateDefinitions = new List<UpdateDefinition<BusinessData>>();

            // General
            string? businessName = formData["general.name"];
            if (!string.IsNullOrWhiteSpace(businessName))
            {
                updateDefinitions.Add(Builders<BusinessData>.Update.Set(x => x.Name, businessName));
            }

            IFormFile? businessLogo = formData.Files.FirstOrDefault(x => x.Name == "general.logo");
            if (businessLogo != null)
            {
                int logoValidateResult = ImageHelper.ValidateBusinessLogoFile(businessLogo);

                if (logoValidateResult == 0)
                {
                    result.Code = "UpdateUserBusinessSettings:4";
                    result.Message = "The business logo file is too big. Maximum size is 3MB.";
                    return result;
                }
                else if (logoValidateResult == 1)
                {
                    result.Code = "UpdateUserBusinessSettings:3";
                    result.Message = "The business logo file is not valid.";
                    return result;
                }
                else if (logoValidateResult != 200)
                {
                    result.Code = "UpdateUserBusinessSettings:4";
                    result.Message = "The business logo file is not valid.";
                    return result;
                }
            }

            // Languages
            string? businessSettingLanguageTabChangesString = formData["languagesTab"].ToString();
            if (!string.IsNullOrWhiteSpace(businessSettingLanguageTabChangesString))
            {
                JsonElement langaugeTabRootJson;
                try
                {
                    langaugeTabRootJson = JsonSerializer.Deserialize<JsonElement>(businessSettingLanguageTabChangesString);
                }
                catch
                {
                    result.Code = "UpdateUserBusinessSettings:5";
                    result.Message = "Unable to parse languages tab changes data.";
                    return result;
                }

                if (!langaugeTabRootJson.TryGetProperty("defaultLanguage", out var businessDefaultLanguage))
                {
                    result.Code = "UpdateUserBusinessSettings:6";
                    result.Message = "Business default language property not found.";
                    return result;
                }
                if (businessDefaultLanguage.ValueKind != JsonValueKind.String)
                {
                    result.Code = "UpdateUserBusinessSettings:7";
                    result.Message = "Business default language is not a string.";
                    return result;
                }
                var businessDefaultLanguageString = businessDefaultLanguage.GetString();
                if (string.IsNullOrWhiteSpace(businessDefaultLanguageString))
                {
                    result.Code = "UpdateUserBusinessSettings:8";
                    result.Message = "Business default language is empty.";
                    return result;
                }

                if (!langaugeTabRootJson.TryGetProperty("languages", out var businessLangauges))
                {
                    result.Code = "UpdateUserBusinessSettings:9";
                    result.Message = "Business languages property not found.";
                    return result;
                }
                var businessLanguagesJsonList = businessLangauges.EnumerateArray().ToList();
                if (businessLanguagesJsonList == null || businessLanguagesJsonList.Count == 0)
                {
                    result.Code = "UpdateUserBusinessSettings:10";
                    result.Message = "Must have at least one language selected.";
                    return result;
                }              

                List<string> builtLangaugesList = new List<string>();
                for (int i = 0; i < businessLanguagesJsonList.Count; i++)
                {
                    var languageCode = businessLanguagesJsonList[i];

                    if (languageCode.ValueKind != JsonValueKind.String)
                    {
                        result.Code = "UpdateUserBusinessSettings:11";
                        result.Message = $"Language code at index {i} is not a string";
                        return result;
                    }

                    var langaugeCodeString = languageCode.GetString();
                    if (string.IsNullOrWhiteSpace(langaugeCodeString))
                    {
                        result.Code = "UpdateUserBusinessSettings:12";
                        result.Message = $"Language code at index {i} is empty.";
                        return result;
                    }

                    var langaugeDataResult = await _languagesManager.GetLanguageByCode(langaugeCodeString);
                    if (!langaugeDataResult.Success)
                    {
                        result.Code = "UpdateUserBusinessSettings:" + langaugeDataResult.Code;
                        result.Message = langaugeDataResult.Message;
                        return result;
                    }

                    if (langaugeDataResult.Data.DisabledAt != null)
                    {
                        result.Code = "UpdateUserBusinessSettings:13";
                        result.Message = $"Selected language {langaugeCodeString} is disabled.";
                        return result;
                    }

                    builtLangaugesList.Add(langaugeCodeString);
                }

                if (!builtLangaugesList.Contains(businessDefaultLanguageString))
                {
                    result.Code = "UpdateUserBusinessSettings:14";
                    result.Message = "Default language must be selected.";
                    return result;
                }

                int addedCount = 0;
                int remainedCount = 0;
                foreach (string oldLanguage in businessData.Languages)
                {
                    if (builtLangaugesList.Contains(oldLanguage))
                    {
                        remainedCount++;
                    }
                }
                foreach (string newLanguage in builtLangaugesList)
                {
                    if (!businessData.Languages.Contains(newLanguage))
                    {
                        addedCount++;
                    }
                }

                if (remainedCount != businessData.Languages.Count || addedCount > 0)
                {
                    var businessLanguagesUpdateResult = MultiLanguageHelper.UpdateObjectMultiLanguages(businessApp, builtLangaugesList, businessData.Languages);
                    if (!businessLanguagesUpdateResult.Success)
                    {
                        result.Code = businessLanguagesUpdateResult.Code;
                        result.Message = businessLanguagesUpdateResult.Message;
                        return result;
                    }
                }

                updateDefinitions.Add(Builders<BusinessData>.Update.Set(d => d.Languages, builtLangaugesList));
                updateDefinitions.Add(Builders<BusinessData>.Update.Set(d => d.DefaultLanguage, businessDefaultLanguageString));

                // todo unpublish the business, calls etc if languages are different than saved ones

                updateBusinessApp = true;
            }

            // Logo upload after all the validation
            if (businessLogo != null)
            {
                var (webpImage, hash) = await ImageHelper.ConvertScaleAndHashToWebp(businessLogo);
                bool fileExists = await _businessLogoRepository.FileExists(hash);
                if (!fileExists)
                {
                    await _businessLogoRepository.PutFileAsByteData(hash + ".webp", webpImage, new Dictionary<string, string>());
                }

                updateDefinitions.Add(
                    Builders<BusinessData>.Update.Set(
                        x => x.LogoS3StorageLink,
                        new S3StorageFileLink
                        {
                            ObjectName = hash,
                            OriginRegion = _s3StorageClientFactory.GetCurrentRegion()
                        }
                    )
                );
            }

            if (updateDefinitions.Count == 0)
            {
                result.Code = "UpdateUserBusinessSettings:15";
                result.Message = "Nothing to update.";
                return result;
            }

            // If all is valid, update business and businesapp
            var updateBusinessResult = await _businessRepository.UpdateBusinessAsync(businessData.Id, Builders<BusinessData>.Update.Combine(updateDefinitions));
            if (!updateBusinessResult)
            {
                result.Code = "UpdateUserBusinessSettings:16";
                result.Message = "Failed to update business data.";
                return result;
            }

            if (updateBusinessApp)
            {
                var updateBusinessAppResult = await _businessAppRepository.ReplaceBusinessAppAsync(businessApp);
                if (!updateBusinessAppResult)
                {
                    await _businessRepository.ReplaceBusinessAsync(businessDataBackup);

                    result.Code = "UpdateUserBusinessSettings:17";
                    result.Message = "Failed to update business app so reverted business data changes.";
                    return result;
                }
            }

            result.Success = true;
            return result;
        }

    }
}
