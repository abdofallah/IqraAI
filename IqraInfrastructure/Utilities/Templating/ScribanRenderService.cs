using IqraCore.Entities.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;
using System.Collections;

namespace IqraInfrastructure.Utilities.Templating
{
    public class ScribanRenderService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ScribanRenderService> _logger;

        // Cache templates for 1 hour to save parsing time
        private readonly TimeSpan _templateCacheDuration = TimeSpan.FromHours(1);

        // Custom function to match previous implementation
        private static readonly Func<string, DateTime> ParseDateTimeFunc = (inputString) => DateTime.Parse(inputString);

        public ScribanRenderService(IMemoryCache cache, ILogger<ScribanRenderService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<FunctionReturnResult<string?>> RenderAsync(string? templateText, Dictionary<string, object?> context)
        {
            var result = new FunctionReturnResult<string?>();

            if (string.IsNullOrEmpty(templateText))
            {
                // Nothing to render
                return result.SetSuccessResult(templateText);
            }

            // Optimization: If the text doesn't contain {{, just return it (avoid overhead)
            if (!templateText.Contains("{{"))
            {
                return result.SetSuccessResult(templateText);
            }

            try
            {
                // 1. Get or Parse Template (Cached)
                // We use the template string itself as the cache key. 
                // For very large templates, we might want to hash this, but for prompt/tool strings, this is acceptable.
                var template = await _cache.GetOrCreateAsync(templateText, async entry =>
                {
                    entry.SlidingExpiration = _templateCacheDuration;
                    entry.Size = templateText.Length; // Optional: helps cache manage memory pressure

                    var parsed = Template.Parse(templateText);
                    if (parsed.HasErrors)
                    {
                        // We don't cache errors effectively, but we need to return the object to check errors outside
                        return parsed; 
                    }
                    return parsed;
                });

                if (template == null || template.HasErrors)
                {
                    var errorMsg = template != null 
                        ? string.Join("\n", template.Messages.Select(m => m.Message)) 
                        : "Unknown parsing error";
                    
                    // _logger.LogWarning("Scriban parse error: {Error}", errorMsg);
                    return result.SetFailureResult("RenderAsync:PARSE_ERROR", $"Template parsing errors: {errorMsg}");
                }

                // 2. Prepare Context
                var templateContext = CreateStandardContext(context);

                // 3. Render
                string rendered = await template.RenderAsync(templateContext);
                
                return result.SetSuccessResult(rendered);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Runtime error rendering Scriban template");
                return result.SetFailureResult("RenderAsync:RUNTIME_ERROR", $"Template rendering error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult<Dictionary<string, object?>>> RenderDictionaryAsync(Dictionary<string, object?> inputData, Dictionary<string, object?> context)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>>();

            try
            {
                // We need a deep copy logic here because we are modifying values
                var processedData = await ProcessObjectRecursively(inputData, context);
                
                if (processedData is Dictionary<string, object?> dict)
                {
                    return result.SetSuccessResult(dict);
                }
                
                // Should not happen if inputData is Dictionary
                return result.SetFailureResult("RenderDictionaryAsync:TYPE_ERROR", "Failed to process dictionary.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dictionary render");
                return result.SetFailureResult("RenderDictionaryAsync:EXCEPTION", ex.Message);
            }
        }

        // --- Helper Methods ---

        private TemplateContext CreateStandardContext(Dictionary<string, object?> variableDictionary)
        {
            var context = new TemplateContext
            {
                LoopLimit = 1000,
                RecursiveLimit = 64,
                StrictVariables = false, // Allow missing variables to be null/empty
                LimitToString = 16000,   // Prevent massive string expansion attacks
                // Standard member renamer: Allows accessing C# properties normally
                MemberRenamer = member => member.Name 
            };

            var scriptObject = new ScriptObject();

            // 1. Import Variables
            foreach (var kvp in variableDictionary)
            {
                // Import safely
                scriptObject.Add(kvp.Key, kvp.Value);
            }

            // 2. Import Standard Functions
            scriptObject.Import("datetimeparse", ParseDateTimeFunc);
            
            scriptObject.Import(typeof(StringFunctions));
            scriptObject.Import(typeof(DateTimeFunctions));
            scriptObject.Import(typeof(TimeSpanFunctions));
            scriptObject.Import(typeof(MathFunctions));
            scriptObject.Import(typeof(RegexFunctions));
            scriptObject.Import(typeof(ArrayFunctions));
            scriptObject.Import(typeof(ObjectFunctions)); // Required for keys/values helpers

            context.PushGlobal(scriptObject);

            return context;
        }

        private async Task<object?> ProcessObjectRecursively(object? obj, Dictionary<string, object?> context)
        {
            if (obj == null) return null;

            // 1. Handle String (Render it)
            if (obj is string str)
            {
                var renderResult = await RenderAsync(str, context);
                return renderResult.Success ? renderResult.Data : str; // Fallback to original on error?
            }

            // 2. Handle Dictionary (Recurse)
            if (obj is Dictionary<string, object?> dict)
            {
                var newDict = new Dictionary<string, object?>();
                foreach (var kvp in dict)
                {
                    newDict[kvp.Key] = await ProcessObjectRecursively(kvp.Value, context);
                }
                return newDict;
            }

            // 3. Handle Lists/Arrays (Recurse)
            if (obj is IEnumerable list && !(obj is string))
            {
                var newList = new List<object?>();
                foreach (var item in list)
                {
                    newList.Add(await ProcessObjectRecursively(item, context));
                }
                return newList;
            }

            // 4. Handle JSON Elements (System.Text.Json) compatibility
            // Sometimes inputData comes from System.Text.Json deserialization
            if (obj is System.Text.Json.JsonElement jsonEl)
            {
                switch (jsonEl.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.String:
                        var renderResult = await RenderAsync(jsonEl.GetString(), context);
                        return renderResult.Success ? renderResult.Data : jsonEl.GetString();
                    case System.Text.Json.JsonValueKind.Object:
                        // Convert JsonElement Object to Dictionary manually to recurse
                        var dictFromEl = new Dictionary<string, object?>();
                        foreach (var prop in jsonEl.EnumerateObject())
                        {
                            // We need to convert property value to object to recurse
                            dictFromEl[prop.Name] = await ProcessObjectRecursively(prop.Value, context);
                        }
                        return dictFromEl;
                    case System.Text.Json.JsonValueKind.Array:
                        var listFromEl = new List<object?>();
                        foreach (var item in jsonEl.EnumerateArray())
                        {
                            listFromEl.Add(await ProcessObjectRecursively(item, context));
                        }
                        return listFromEl;
                    // Primitives
                    case System.Text.Json.JsonValueKind.Number: return jsonEl.GetDouble(); 
                    case System.Text.Json.JsonValueKind.True: return true;
                    case System.Text.Json.JsonValueKind.False: return false;
                    case System.Text.Json.JsonValueKind.Null: return null;
                }
            }

            // 5. Return Primitives (Int, Bool, etc.) as is
            return obj;
        }
    }
}