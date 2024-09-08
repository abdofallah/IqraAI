using IqraCore.Entities.Helpers;
using IqraCore.Entities.Languages;
using IqraInfrastructure.Repositories.Languages;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace IqraInfrastructure.Services.Languages
{
    public class LanguagesManager
    {
        private readonly LanguagesRepository _languagesRepository;
        public LanguagesManager(LanguagesRepository languagesRepository)
        {
            _languagesRepository = languagesRepository;
        }

        public async Task<FunctionReturnResult<LanguagesData?>> AddUpdateLanguage(IFormCollection formData, string postType, string languageCode, LanguagesData? oldLanguageData)
        {
            var result = new FunctionReturnResult<LanguagesData?>();

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddUpdateLanguage:1";
                return result;
            }

            if (string.IsNullOrEmpty(changesJsonString))
            {
                result.Code = "AddUpdateLanguage:2";
                return result;
            }

            var changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJsonString);
            if (changesJsonElement == null)
            {
                result.Code = "AddUpdateLanguage:3";
                return result;
            }

            LanguagesData newLanguagesData = new LanguagesData()
            {
                Id = languageCode
            };

            if (!changesJsonElement.RootElement.TryGetProperty("name", out var languageNameElement))
            {
                result.Code = "AddUpdateLanguage:4";
                result.Message = "Language name Not Found";
                return result;
            }
            string? languageName = languageNameElement.GetString();
            if (string.IsNullOrEmpty(languageName))
            {
                result.Code = "AddUpdateLanguage:5";
                result.Message = "Language name Not Found";
                return result;
            }
            newLanguagesData.Name = languageName;

            if (!changesJsonElement.RootElement.TryGetProperty("localeName", out var languageLocaleNameElement))
            {
                result.Code = "AddUpdateLanguage:6";
                result.Message = "Language locale name Not Found";
                return result;
            }
            string? languageLocalceName = languageLocaleNameElement.GetString();
            if (string.IsNullOrEmpty(languageLocalceName))
            {
                result.Code = "AddUpdateLanguage:7";
                result.Message = "Language locale name Not Found";
                return result;
            }
            newLanguagesData.LocaleName = languageLocalceName;

            if (!changesJsonElement.RootElement.TryGetProperty("disabled", out var languageDisabledElement))
            {
                result.Code = "AddUpdateLanguage:8";
                result.Message = "Language disabled Not Found";
                return result;
            }
            bool? languageDisabled = languageDisabledElement.GetBoolean();
            if (languageDisabled == null)
            {
                result.Code = "AddUpdateLanguage:9";
                result.Message = "Language disabled Not Found";
                return result;
            }
            
            if (languageDisabled == true)
            {
                newLanguagesData.DisabledAt = DateTime.UtcNow;

                if (oldLanguageData != null)
                {
                    if (oldLanguageData.DisabledAt != null)
                    {
                        newLanguagesData.DisabledAt = oldLanguageData.DisabledAt;
                    }
                }
            }

            if (postType == "new")
            {
                bool addResult = await _languagesRepository.AddNewLanguage(newLanguagesData);
            }
            else if (postType == "edit")
            {
                bool replaceResult = await _languagesRepository.ReplaceLanguage(newLanguagesData);
            }

            result.Success = true;
            result.Data = newLanguagesData;
            return result;
        }

        public async Task<FunctionReturnResult<LanguagesData?>> GetLanguageByCode(string languageCode)
        {
            var result = new FunctionReturnResult<LanguagesData?>();

            var getResult = await _languagesRepository.GetLanguageByCode(languageCode);
            if (getResult == null)
            {
                result.Code = "GetLanguageByCode:1";
                return result;
            }
            
            result.Success = true;
            result.Data = getResult;
            return result;
        }

        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetLanguagesList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

            var getResult = await _languagesRepository.GetLanguagesList(page, pageSize);
            if (getResult == null)
            {
                result.Code = "GetLanguagesList:1";
                return result;
            }
            
            result.Success = true;
            result.Data = getResult;
            return result;
        }
    }
}
