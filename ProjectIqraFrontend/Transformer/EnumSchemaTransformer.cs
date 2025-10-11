using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Text;

namespace ProjectIqraFrontend.Transformer
{
    public class EnumSchemaTransformer : IOpenApiSchemaTransformer
    {
        public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            // BISMILLAH: CORRECTED - We get the C# Type from the JsonTypeInfo property.
            var enumType = context.JsonTypeInfo.Type;

            // First, check if the type we're transforming is actually an enum.
            if (!enumType.IsEnum)
            {
                // If not, do nothing and return.
                await Task.CompletedTask;
                return;
            }

            // The generator, seeing your custom converter, likely set the type to "integer".
            // We will confirm this and add the rich details.
            schema.Type = "integer";
            schema.Format = "int32";

            // Clear any default enum values the generator might have added.
            schema.Enum.Clear();

            // Add all possible integer values of the enum to the schema's "enum" list.
            // This will help UI tools like Scalar to render a dropdown of valid options.
            // BISMILLAH: CORRECTED - Use the correct enumType variable.
            foreach (var value in Enum.GetValues(enumType))
            {
                schema.Enum.Add(new OpenApiInteger((int)value));
            }

            // BISMILLAH: Build a rich HTML description to show the name-to-value mapping.
            // This is the key to making the documentation user-friendly.
            var description = new StringBuilder("<h6>Enum Members:</h6><ul>");
            // BISMILLAH: CORRECTED - Use the correct enumType variable.
            foreach (var memberName in Enum.GetNames(enumType))
            {
                // BISMILLAH: CORRECTED - Use the correct enumType variable.
                var memberValue = (int)Enum.Parse(enumType, memberName);
                description.Append($"<li><strong>{memberName}</strong> = {memberValue}</li>");
            }
            description.Append("</ul>");

            // Append our rich description to any existing description the property might have.
            if (!string.IsNullOrEmpty(schema.Description))
            {
                schema.Description += "<br/><br/>" + description.ToString();
            }
            else
            {
                schema.Description = description.ToString();
            }

            await Task.CompletedTask;
        }
    }
}
