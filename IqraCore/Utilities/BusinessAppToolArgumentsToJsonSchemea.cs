using IqraCore.Entities.Business;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace IqraCore.Utilities
{
    public static class BusinessAppToolArgumentsToJsonSchemea
    {
        public static string ConvertToJsonSchema(List<BusinessAppToolConfigurationInputSchemea> schemaDefinitions, string defaultLanguage = "en", bool minifyOutput = false)
        {
            if (schemaDefinitions == null)
            {
                throw new ArgumentNullException(nameof(schemaDefinitions));
            }

            var schema = new Dictionary<string, object>
            {
                { "type", "object" },
                { "properties", new Dictionary<string, object>() },
                { "required", new List<string>() }
            };

            var properties = (Dictionary<string, object>)schema["properties"];
            var required = (List<string>)schema["required"];

            foreach (var definition in schemaDefinitions)
            {
                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    Console.WriteLine($"Warning: Skipping definition with missing Id.");
                    continue;
                }

                var propertySchema = new Dictionary<string, object>();

                string description = null;
                if (definition.Description != null)
                {
                    if (definition.Description.TryGetValue(defaultLanguage, out var langDesc))
                    {
                        description = langDesc;
                    }
                    else if (definition.Description.Any())
                    {
                        description = definition.Description.First().Value;
                    }
                }
                if (!string.IsNullOrWhiteSpace(description))
                {
                    propertySchema.Add("description", description);
                }

                string jsonType = null;
                string format = null;

                switch (definition.Type)
                {
                    case BusinessAppToolConfigurationInputSchemeaTypeEnum.String:
                        jsonType = "string";
                        break;
                    case BusinessAppToolConfigurationInputSchemeaTypeEnum.Number:
                        jsonType = "number";
                        break;
                    case BusinessAppToolConfigurationInputSchemeaTypeEnum.Boolean:
                        jsonType = "boolean";
                        break;
                    case BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime:
                        jsonType = "string";
                        format = "date-time"; // Default format
                        if (definition is BusinessAppToolConfigurationInputSchemeaDateTime dateTimeSchema && !string.IsNullOrEmpty(dateTimeSchema.DateTimeFormat))
                        {
                            format = dateTimeSchema.DateTimeFormat; // Use custom format if available
                        }
                        break;
                    case BusinessAppToolConfigurationInputSchemeaTypeEnum.Unknown:
                    default:
                        Console.WriteLine($"Warning: Property '{definition.Id}' has Unknown type. Omitting 'type' attribute.");
                        break;
                }

                if (definition.IsArray)
                {
                    propertySchema.Add("type", "array");
                    var itemSchema = new Dictionary<string, object>();
                    if (jsonType != null) itemSchema.Add("type", jsonType);
                    if (format != null) itemSchema.Add("format", format);
                    if (itemSchema.Any()) propertySchema.Add("items", itemSchema);
                }
                else
                {
                    if (jsonType != null) propertySchema.Add("type", jsonType);
                    if (format != null) propertySchema.Add("format", format);
                }

                if (propertySchema.Any())
                {
                    properties.Add(definition.Id, propertySchema);
                    if (definition.IsRequired)
                    {
                        required.Add(definition.Id);
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: Property '{definition.Id}' resulted in an empty schema definition. Skipping.");
                }
            }

            if (!required.Any())
            {
                schema.Remove("required");
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = !minifyOutput,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(schema, options);
        }
    }
}