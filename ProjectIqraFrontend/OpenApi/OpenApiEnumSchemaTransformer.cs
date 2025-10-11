using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Text;

namespace ProjectIqraFrontend.Transformer
{
    public class OpenApiEnumSchemaTransformer : IOpenApiSchemaTransformer
    {
        public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            var enumType = context.JsonTypeInfo.Type;
            if (!enumType.IsEnum)
            {
                await Task.CompletedTask;
                return;
            }

            schema.Type = "integer";
            schema.Format = "int32";

            schema.Enum.Clear();
            foreach (var value in Enum.GetValues(enumType))
            {
                schema.Enum.Add(new OpenApiInteger((int)value));
            }

            var description = new StringBuilder("<h6>Enum Members:</h6><ul>");
            foreach (var memberName in Enum.GetNames(enumType))
            {
                var memberValue = (int)Enum.Parse(enumType, memberName);
                description.Append($"<li><strong>{memberName}</strong> = {memberValue}</li>");
            }
            description.Append("</ul>");

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
