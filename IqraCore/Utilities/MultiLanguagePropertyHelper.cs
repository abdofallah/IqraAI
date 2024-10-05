using IqraCore.Entities.Helpers;
using IqraCore.Entities.Languages;
using System.Text.Json;

namespace IqraCore.Utilities
{
    public class MultiLanguagePropertyHelper
    {
        public static FunctionReturnResult<bool> ValidateAndAssignMultiLanguageProperty(
            IEnumerable<LanguagesData> languages,
            JsonElement jsonElement,
            string propertyKey,
            IDictionary<string, string> targetDictionary)
        {
            var result = new FunctionReturnResult<bool>();

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
                    || string.IsNullOrWhiteSpace(value))
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
    }
}
