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

            // BISMILLAH: STEP 2 - Define the Security Scheme for "Authorization: Token <key>"
            var apiKeyScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Token", // Important for documentation and some tools
                Description = "API Key authentication. Enter your key in the format: 'Token <your-api-key>'. Note: The 'Token ' prefix is handled by this UI, you only need to paste your key."
            };

            // Add the scheme to the document's components.
            // The key "ApiKeyAuth" is a reference ID we will use next.
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes.Add("ApiKeyAuth", apiKeyScheme);

            // BISMILLAH: STEP 3 - Apply the security scheme globally to all endpoints
            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                {
                    // This references the scheme we defined above by its ID "ApiKeyAuth"
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKeyAuth"
                        }
                    },
                    // The list of scopes is empty for ApiKey authentication
                    new string[] {}
                }
            });

            // The Task.CompletedTask is a convention for async methods that complete synchronously.
            await Task.CompletedTask;
        }
    }
}
