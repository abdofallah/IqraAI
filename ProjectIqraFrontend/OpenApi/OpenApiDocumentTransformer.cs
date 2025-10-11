using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace ProjectIqraFrontend.Transformer
{
    public class OpenApiDocumentTransformer : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            if (document.Servers.Count > 0)
            {
                var firstDocument = document.Servers[0];

                firstDocument.Url = firstDocument.Url.Replace("http://", "https://");
                firstDocument.Description = "Primary Endpoint";
            }

            var apiKeyScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Token", // Important for documentation and some tools
                Description = "API Key authentication. Enter your key in the format: 'Token <your-api-key>'. Note: The 'Token ' prefix is handled by this UI, you only need to paste your key."
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes.Add("ApiKeyAuth", apiKeyScheme);

            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKeyAuth"
                        }
                    },
                    new string[] {}
                }
            });

            await Task.CompletedTask;
        }
    }
}
