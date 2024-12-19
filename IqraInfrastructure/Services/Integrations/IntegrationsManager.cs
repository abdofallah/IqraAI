using IqraCore.Entities.Helpers;
using IqraCore.Entities.Integrations;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Integrations;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace IqraInfrastructure.Services.Integrations
{
    public class IntegrationsManager
    {
        private readonly IntegrationsRepository _integrationsRepository;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly IntegrationsLogoRepository _integrationsLogoRepository;
        private readonly string _logoFolder = "integration.logo";

        public IntegrationsManager(
            IntegrationsRepository integrationsRepository,
            BusinessAppRepository businessAppRepository,
            IntegrationsLogoRepository logoManager)
        {
            _integrationsRepository = integrationsRepository;
            _businessAppRepository = businessAppRepository;
            _integrationsLogoRepository = logoManager;
        }

        public async Task<FunctionReturnResult<List<IntegrationData>?>> GetIntegrationsList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<IntegrationData>?>();

            try
            {
                var integrations = await _integrationsRepository.GetIntegrationsListAsync(page, pageSize);
                result.Success = true;
                result.Data = integrations;
            }
            catch (Exception ex)
            {
                result.Code = "GetIntegrationsList:1";
                result.Message = "Error retrieving integrations list: " + ex.Message;
            }

            return result;
        }

        public async Task<FunctionReturnResult<IntegrationData?>> AddOrUpdateIntegration(
            string changesJson,
            string postType,
            string? existingIntegrationId,
            IFormFile? logoFile)
        {
            var result = new FunctionReturnResult<IntegrationData?>();

            try
            {
                // Parse changes
                JsonDocument? changes = JsonDocument.Parse(changesJson);
                if (changes == null)
                {
                    result.Code = "AddOrUpdateIntegration:1";
                    result.Message = "Unable to parse changes json string.";
                    return result;
                }

                // Validate basic required fields
                if (!ValidateBasicFields(changes.RootElement, out var validationError))
                {
                    result.Code = "AddOrUpdateIntegration:2";
                    result.Message = validationError;
                    return result;
                }

                // Create new integration data
                var integrationData = new IntegrationData
                {
                    Id = changes.RootElement.GetProperty("id").GetString()!,
                    Name = changes.RootElement.GetProperty("name").GetString()!,
                    Description = changes.RootElement.GetProperty("description").GetString() ?? "",
                    Type = changes.RootElement.GetProperty("type").EnumerateArray()
                        .Select(e => e.GetString()!)
                        .ToList(),
                    Fields = changes.RootElement.GetProperty("fields").EnumerateArray()
                        .Select(f => ParseField(f))
                        .ToList(),
                    Help = ParseHelp(changes.RootElement.GetProperty("help"))
                };

                // Validate fields
                if (!ValidateFields(integrationData.Fields, out validationError))
                {
                    result.Code = "AddOrUpdateIntegration:3";
                    result.Message = validationError;
                    return result;
                }

                // Handle logo upload if present
                if (logoFile != null)
                {
                    var (webpImage, hash) = await ImageHelper.ConvertScaleAndHashToWebp(logoFile);
                    bool fileExists = await _integrationsLogoRepository.FileExists(hash);
                    if (!fileExists)
                    {
                        await _integrationsLogoRepository.PutFileAsByteData(hash + ".webp", webpImage, new Dictionary<string, string>());
                    }

                    integrationData.Logo = hash;
                }
                else if (postType == "edit")
                {
                    var existingIntegration = await _integrationsRepository.GetIntegrationAsync(existingIntegrationId!);
                    integrationData.Logo = existingIntegration?.Logo ?? null;
                }

                // Save or update
                if (postType == "new")
                {
                    // Check if ID already exists
                    var existingIntegration = await _integrationsRepository.GetIntegrationAsync(integrationData.Id);
                    if (existingIntegration != null)
                    {
                        result.Code = "AddOrUpdateIntegration:4";
                        result.Message = "Integration ID already exists.";
                        return result;
                    }

                    await _integrationsRepository.AddIntegrationAsync(integrationData);
                }
                else
                {
                    var updateResult = await _integrationsRepository.UpdateIntegrationAsync(integrationData);
                    if (updateResult.ModifiedCount == 0)
                    {
                        result.Code = "AddOrUpdateIntegration:5";
                        result.Message = "Failed to update integration.";
                        return result;
                    }
                }

                result.Success = true;
                result.Data = integrationData;
            }
            catch (Exception ex)
            {
                result.Code = "AddOrUpdateIntegration:6";
                result.Message = "Error processing integration: " + ex.Message;
            }

            return result;
        }

        private bool ValidateBasicFields(JsonElement changes, out string error)
        {
            if (!changes.TryGetProperty("id", out var idElement) || string.IsNullOrWhiteSpace(idElement.GetString()))
            {
                error = "Integration ID is required.";
                return false;
            }

            if (!changes.TryGetProperty("name", out var nameElement) || string.IsNullOrWhiteSpace(nameElement.GetString()))
            {
                error = "Integration name is required.";
                return false;
            }

            if (!changes.TryGetProperty("type", out var typeElement) || !typeElement.EnumerateArray().Any())
            {
                error = "At least one integration type is required.";
                return false;
            }

            if (!changes.TryGetProperty("fields", out var fieldsElement) || !fieldsElement.EnumerateArray().Any())
            {
                error = "At least one field is required.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private bool ValidateFields(List<IntegrationFieldData> fields, out string error)
        {
            var fieldIds = new HashSet<string>();

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Id))
                {
                    error = "Field ID is required for all fields.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(field.Name))
                {
                    error = "Field name is required for all fields.";
                    return false;
                }

                if (!fieldIds.Add(field.Id))
                {
                    error = $"Duplicate field ID found: {field.Id}";
                    return false;
                }

                if (field.Type == "select" && (field.Options == null || !field.Options.Any()))
                {
                    error = $"Select field '{field.Name}' must have at least one option.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private IntegrationFieldData ParseField(JsonElement field)
        {
            var fieldData = new IntegrationFieldData
            {
                Id = field.GetProperty("id").GetString()!,
                Name = field.GetProperty("name").GetString()!,
                Type = field.GetProperty("type").GetString()!,
                Required = field.GetProperty("required").GetBoolean(),
                IsEncrypted = field.GetProperty("isEncrypted").GetBoolean()
            };

            if (field.TryGetProperty("tooltip", out var tooltipElement))
            {
                fieldData.Tooltip = tooltipElement.GetString();
            }

            if (field.TryGetProperty("options", out var optionsElement))
            {
                fieldData.Options = optionsElement.EnumerateArray()
                    .Select(o => new IntegrationFieldOption
                    {
                        Key = o.GetProperty("key").GetString()!,
                        Value = o.GetProperty("value").GetString()!
                    })
                    .ToList();
            }

            return fieldData;
        }

        private IntegrationHelpData ParseHelp(JsonElement help)
        {
            return new IntegrationHelpData
            {
                Text = help.GetProperty("text").GetString() ?? "",
                Uri = help.GetProperty("uri").GetString() ?? ""
            };
        }
    }
}
