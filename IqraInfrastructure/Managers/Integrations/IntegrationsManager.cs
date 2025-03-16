using IqraCore.Entities.Helpers;
using IqraCore.Entities.Integrations;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Integrations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Integrations
{
    public class IntegrationsManager
    {
        private readonly ILogger<IntegrationsManager> _logger;

        private readonly IntegrationsRepository _integrationsRepository;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly IntegrationsLogoRepository _integrationsLogoRepository;
        private readonly string _logoFolder = "integration.logo";
        private readonly AES256EncryptionService _integrationFieldEncryptionService;

        public IntegrationsManager(
            ILogger<IntegrationsManager> logger,
            IntegrationsRepository integrationsRepository,
            BusinessAppRepository businessAppRepository,
            IntegrationsLogoRepository logoManager,
            AES256EncryptionService integrationFieldEncryptionService
        )
        {
            _logger = logger;

            _integrationsRepository = integrationsRepository;
            _businessAppRepository = businessAppRepository;
            _integrationsLogoRepository = logoManager;
            _integrationFieldEncryptionService = integrationFieldEncryptionService;
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
                var changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJson);
                if (changesJsonElement == null)
                {
                    result.Code = "AddOrUpdateIntegration:1";
                    result.Message = "Unable to parse changes json string.";
                    return result;
                }

                // Validate basic required fields
                if (!ValidateBasicFields(changesJsonElement.RootElement, out var validationError))
                {
                    result.Code = "AddOrUpdateIntegration:2";
                    result.Message = validationError;
                    return result;
                }

                IntegrationData? existingIntegration = null;
                if (postType == "edit")
                {
                    existingIntegration = await _integrationsRepository.GetIntegrationAsync(existingIntegrationId!);
                    if (existingIntegration == null)
                    {
                        result.Code = "AddOrUpdateIntegration:3";
                        result.Message = "Integration not found.";
                        return result;
                    }
                }

                // Create new integration data
                IntegrationData integrationData = new IntegrationData
                {
                    Id = changesJsonElement.RootElement.GetProperty("id").GetString()!,
                    Name = changesJsonElement.RootElement.GetProperty("name").GetString()!,
                    Description = changesJsonElement.RootElement.GetProperty("description").GetString() ?? "",
                    Type = changesJsonElement.RootElement.GetProperty("type").EnumerateArray()
                        .Select(e => e.GetString()!)
                        .ToList(),
                    Fields = changesJsonElement.RootElement.GetProperty("fields").EnumerateArray()
                        .Select(f => ParseField(f))
                        .ToList(),
                    Help = ParseHelp(changesJsonElement.RootElement.GetProperty("help"))
                };

                // Handle disabled state
                if (!changesJsonElement.RootElement.TryGetProperty("disabled", out var disabledElement))
                {
                    result.Code = "AddOrUpdateIntegration:4";
                    result.Message = "Integration disabled state not found";
                    return result;
                }

                bool disabled = disabledElement.GetBoolean();
                if (disabled)
                {
                    integrationData.DisabledAt = DateTime.UtcNow;

                    if (postType == "edit")
                    {
                        if (existingIntegration?.DisabledAt != null)
                        {
                            integrationData.DisabledAt = existingIntegration.DisabledAt;
                        }
                    }
                }
                else
                {
                    integrationData.DisabledAt = null;
                }

                // Validate fields
                if (!ValidateFields(integrationData.Fields, out validationError))
                {
                    result.Code = "AddOrUpdateIntegration:5";
                    result.Message = validationError;
                    return result;
                }

                // Handle logo
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
                else if (postType == "edit" && existingIntegration?.Logo != null)
                {
                    integrationData.Logo = existingIntegration?.Logo;
                }
                else
                {
                    result.Code = "AddOrUpdateIntegration:6";
                    result.Message = "Logo is required for new integrations.";
                    return result;
                }

                // Save or update
                if (postType == "new")
                {
                    await _integrationsRepository.AddIntegrationAsync(integrationData);
                }
                else
                {
                    var updateResult = await _integrationsRepository.UpdateIntegrationAsync(integrationData);
                    if (updateResult.ModifiedCount == 0)
                    {
                        result.Code = "AddOrUpdateIntegration:8";
                        result.Message = "Failed to update integration.";
                        return result;
                    }
                }

                result.Success = true;
                result.Data = integrationData;
            }
            catch (Exception ex)
            {
                result.Code = "AddOrUpdateIntegration:9";
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
                // Check required fields
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

                // Check duplicate field IDs
                if (!fieldIds.Add(field.Id))
                {
                    error = $"Duplicate field ID found: {field.Id}";
                    return false;
                }

                // Validate select field options
                if (field.Type == "select")
                {
                    if (field.Options == null || !field.Options.Any())
                    {
                        error = $"Select field '{field.Name}' must have at least one option.";
                        return false;
                    }

                    // Validate options
                    var defaultOptionsCount = field.Options.Count(o => o.IsDefault);
                    if (defaultOptionsCount > 1)
                    {
                        error = $"Select field '{field.Name}' cannot have multiple default options.";
                        return false;
                    }

                    foreach (var option in field.Options)
                    {
                        if (string.IsNullOrWhiteSpace(option.Key) ||
                            string.IsNullOrWhiteSpace(option.Value))
                        {
                            error = $"All options in select field '{field.Name}' must have both key and value.";
                            return false;
                        }
                    }
                }
                else
                {
                    // Validate default value for number type
                    if (field.Type == "number" && !string.IsNullOrEmpty(field.DefaultValue))
                    {
                        if (!decimal.TryParse(field.DefaultValue, out _))
                        {
                            error = $"Default value for number field '{field.Name}' must be a valid number.";
                            return false;
                        }
                    }
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

            // Handle optional fields
            if (field.TryGetProperty("tooltip", out var tooltipElement))
            {
                fieldData.Tooltip = tooltipElement.GetString();
            }

            if (field.TryGetProperty("placeholder", out var placeholderElement))
            {
                fieldData.Placeholder = placeholderElement.GetString();
            }

            // Handle default value based on field type
            if (field.TryGetProperty("defaultValue", out var defaultValueElement))
            {
                string? defaultValue = defaultValueElement.GetString();
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    if (fieldData.Type == "number")
                    {
                        // Validate if default value is a valid number
                        if (decimal.TryParse(defaultValue, out _))
                        {
                            fieldData.DefaultValue = defaultValue;
                        }
                    }
                    else if (fieldData.Type == "text")
                    {
                        fieldData.DefaultValue = defaultValue;
                    }
                }
            }

            // Handle select options
            if (fieldData.Type == "select" && field.TryGetProperty("options", out var optionsElement))
            {
                fieldData.Options = optionsElement.EnumerateArray()
                    .Select(o => new IntegrationFieldOption
                    {
                        Key = o.GetProperty("key").GetString()!,
                        Value = o.GetProperty("value").GetString()!,
                        IsDefault = o.GetProperty("isDefault").GetBoolean()
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

        public async Task<bool> IntegrationExists(string currentIntegrationId)
        {
            return await _integrationsRepository.IntegrationExistsAsync(currentIntegrationId);
        }

        public async Task<FunctionReturnResult<List<IntegrationData>?>> GetIntegrationsList()
        {
            var result = new FunctionReturnResult<List<IntegrationData>?>();

            try
            {
                var integrations = await _integrationsRepository.GetAllIntegrationsAsync();

                result.Success = true;
                result.Data = integrations;
            }
            catch (Exception ex)
            {
                result.Code = "GetActiveIntegrationsList:1";
                result.Message = "Error retrieving integrations list: " + ex.Message;
            }

            return result;
        }

        public async Task<FunctionReturnResult<IntegrationData?>> getIntegrationData(string integrationId)
        {
            var result = new FunctionReturnResult<IntegrationData?>();

            try
            {
                var integration = await _integrationsRepository.GetIntegrationById(integrationId);

                if (integration == null)
                {
                    result.Code = "getIntegrationData:1";
                    result.Message = "Integration not found.";
                    return result;
                }

                result.Success = true;
                result.Data = integration;
            }
            catch (Exception ex)
            {
                result.Code = "getIntegrationData:2";
                result.Message = "Error retrieving integration: " + ex.Message;
            }

            return result;
        }

        public string EncryptField(string fieldValue)
        {
            return _integrationFieldEncryptionService.Encrypt(fieldValue);
        }

        public string DecryptField(string encryptedFieldValue)
        {
            return _integrationFieldEncryptionService.Decrypt(encryptedFieldValue);
        }
    }
}
