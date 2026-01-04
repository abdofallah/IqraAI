using System.Text.Json;
using IqraCore.Entities.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.Validation;

namespace IqraInfrastructure.Utilities.Validation
{
    public class NJsonSchemaValidator
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<NJsonSchemaValidator> _logger;

        // Cache schema objects for 1 hour to avoid expensive re-parsing
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

        public NJsonSchemaValidator(IMemoryCache cache, ILogger<NJsonSchemaValidator> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<FunctionReturnResult> ValidateAsync(JsonElement jsonData, string jsonSchema, string schemaKey)
        {
            var result = new FunctionReturnResult();

            if (string.IsNullOrWhiteSpace(jsonSchema))
            {
                // If no schema is provided, we assume no validation is needed (Passthrough)
                return result.SetSuccessResult();
            }

            JsonSchema? schema = null;

            try
            {
                // 1. Get or Create Cached Schema Object
                // Parsing string -> JsonSchema object is CPU intensive, so we cache it.
                schema = await _cache.GetOrCreateAsync(schemaKey, async entry =>
                {
                    entry.SlidingExpiration = _cacheDuration;
                    return await JsonSchema.FromJsonAsync(jsonSchema);
                });

                if (schema == null)
                {
                    return result.SetFailureResult("ValidateAsync:SCHEMA_NULL", "Failed to parse or retrieve schema.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse JSON Schema for key {SchemaKey}", schemaKey);
                return result.SetFailureResult("ValidateAsync:SCHEMA_PARSE_ERROR", $"Invalid App Schema Definition: {ex.Message}");
            }

            try
            {
                // 2. Prepare Data for NJsonSchema
                // NJsonSchema works best with strings when not using Newtonsoft.Json types directly.
                // Converting the JsonElement to a string ensures a clean handoff to the validator.
                string jsonString = jsonData.ToString();

                // 3. Execute Validation
                ICollection<ValidationError> errors = schema.Validate(jsonString);

                // 4. Handle Results
                if (errors != null && errors.Count > 0)
                {
                    // Format errors into a readable string
                    var errorMessages = errors.Select(e =>
                    {
                        var path = string.IsNullOrEmpty(e.Path) ? "Root" : e.Path;
                        return $"[Field: {path}] {e.Kind}";
                    });

                    string combinedErrors = string.Join(" | ", errorMessages);
                    
                    _logger.LogWarning("Validation failed for {SchemaKey}. Errors: {Errors}", schemaKey, combinedErrors);
                    
                    return result.SetFailureResult("ValidateAsync:VALIDATION_FAILED", $"Input validation failed: {combinedErrors}");
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during validation execution for {SchemaKey}", schemaKey);
                return result.SetFailureResult("ValidateAsync:EXCEPTION", $"Validator exception: {ex.Message}");
            }
        }
    }
}