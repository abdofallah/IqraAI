using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;

namespace IqraCore.Utilities
{
    public class JsonFormBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // 1. Get the name of the property we are trying to bind.
            var modelName = bindingContext.ModelName;

            // 2. Get the value from the form data using that name.
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            // 3. If no value is found, do nothing.
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            // 4. Get the string value.
            var jsonString = valueProviderResult.FirstValue;

            // 5. If the string is null or empty, it can't be deserialized.
            if (string.IsNullOrEmpty(jsonString))
            {
                return Task.CompletedTask;
            }

            try
            {
                // 6. Deserialize the JSON string to the target model type.
                var options = new JsonSerializerOptions { };
                var deserializedObject = JsonSerializer.Deserialize(jsonString, bindingContext.ModelType, options);

                // 7. If successful, set the binding result.
                bindingContext.Result = ModelBindingResult.Success(deserializedObject);
            }
            catch (JsonException ex)
            {
                // 8. If deserialization fails, add a model state error.
                bindingContext.ModelState.AddModelError(modelName, $"Cannot convert value '{jsonString}' to {bindingContext.ModelType.Name}. Error: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
