using IqraCore.Entities.Helpers;
using IqraCore.Entities.Languages;
using System.Text.Json;

namespace IqraCore.Utilities
{
    public static class MultiLanguagePropertyHelper
    {
        public static FunctionReturnResult ValidateAndAssignMultiLanguageProperty(
            IEnumerable<string> languages,
            JsonElement jsonElement,
            string propertyKey,
            IDictionary<string, string> targetDictionary,
            bool isOptional = false)
        {
            List<LanguagesData> languageList = languages.Select(x => new LanguagesData() { Id = x }).ToList();

            return ValidateAndAssignMultiLanguageProperty(languageList, jsonElement, propertyKey, targetDictionary, isOptional);
        }

        public static FunctionReturnResult ValidateAndAssignMultiLanguageProperty(
            IEnumerable<LanguagesData> languages,
            JsonElement jsonElement,
            string propertyKey,
            IDictionary<string, string> targetDictionary,
            bool isOptional = false)
        {
            var result = new FunctionReturnResult();

            if (!jsonElement.TryGetProperty(propertyKey, out var multiLangElement)
                || multiLangElement.ValueKind != JsonValueKind.Object)
            {
                result.Code = "MultiLanguagePropertyValidation:1";
                result.Message = $"Invalid or missing {propertyKey}";
                return result;
            }

            var tempDictionary = new Dictionary<string, string>();
            foreach (var property in multiLangElement.EnumerateObject())
            {
                tempDictionary[property.Name] = property.Value.GetString() ?? string.Empty;
            }

            if (tempDictionary.Count != languages.Count())
            {
                result.Code = "MultiLanguagePropertyValidation:2";
                result.Message = $"Mismatch in number of {propertyKey} and application languages";
                return result;
            }

            foreach (var language in languages)
            {
                if (!tempDictionary.TryGetValue(language.Id, out var value)
                    || (string.IsNullOrWhiteSpace(value) && !isOptional))
                {
                    result.Code = "MultiLanguagePropertyValidation:3";
                    result.Message = $"Missing or empty {propertyKey} for language: {language.Id}";
                    return result;
                }

                targetDictionary[language.Id] = value;
            }

            result.Success = true;
            return result;
        }

        public static FunctionReturnResult ValidateAndAssignMultiLanguageListProperty(
            IEnumerable<string> languages,
            JsonElement jsonElement,
            string propertyKey,
            IDictionary<string, List<string>> targetDictionary,
            bool isOptional = false)
        {
            List<LanguagesData> languageList = languages.Select(x => new LanguagesData() { Id = x }).ToList();

            return ValidateAndAssignMultiLanguageListProperty(languageList, jsonElement, propertyKey, targetDictionary, isOptional);
        }

        public static FunctionReturnResult ValidateAndAssignMultiLanguageListProperty(
            IEnumerable<LanguagesData> languages,
            JsonElement jsonElement,
            string propertyKey,
            IDictionary<string, List<string>> targetDictionary,
            bool isOptional = false)
        {
            var result = new FunctionReturnResult();

            if (!jsonElement.TryGetProperty(propertyKey, out var multiLangElement)
                || multiLangElement.ValueKind != JsonValueKind.Object)
            {
                result.Code = "MultiLanguageListPropertyValidation:1";
                result.Message = $"Invalid or missing {propertyKey}";
                return result;
            }

            var tempDictionary = new Dictionary<string, List<string>>();
            foreach (var property in multiLangElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                {
                    result.Code = "MultiLanguageListPropertyValidation:2";
                    result.Message = $"Invalid value type for {propertyKey} in language: {property.Name}. Expected array.";
                    return result;
                }

                tempDictionary[property.Name] = new List<string>();
                foreach (var item in property.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        result.Code = "MultiLanguageListPropertyValidation:3";
                        result.Message = $"Invalid array item type for {propertyKey} in language: {property.Name}. Expected string.";
                        return result;
                    }

                    var value = item.GetString();
                    if (string.IsNullOrWhiteSpace(value) && !isOptional)
                    {
                        result.Code = "MultiLanguageListPropertyValidation:4";
                        result.Message = $"Empty array item found for {propertyKey} in language: {property.Name}";
                        return result;
                    }

                    tempDictionary[property.Name].Add(value ?? string.Empty);
                }
            }

            if (tempDictionary.Count != languages.Count())
            {
                result.Code = "MultiLanguageListPropertyValidation:5";
                result.Message = $"Mismatch in number of {propertyKey} and application languages";
                return result;
            }

            foreach (var language in languages)
            {
                if (!tempDictionary.TryGetValue(language.Id, out var list))
                {
                    result.Code = "MultiLanguageListPropertyValidation:6";
                    result.Message = $"Missing {propertyKey} for language: {language.Id}";
                    return result;
                }

                if (list.Count == 0 && !isOptional)
                {
                    result.Code = "MultiLanguageListPropertyValidation:7";
                    result.Message = $"Empty list for {propertyKey} in language: {language.Id}";
                    return result;
                }

                targetDictionary[language.Id] = list;
            }

            result.Success = true;
            return result;
        }
    }
}
