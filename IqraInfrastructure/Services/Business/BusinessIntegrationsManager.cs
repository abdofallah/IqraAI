using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Integrations;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Services.Integrations;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IqraInfrastructure.Services.Business
{
    public class BusinessIntegrationsManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;

        public BusinessIntegrationsManager(BusinessManager businessManager, BusinessAppRepository businessAppRepository)
        {
            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
        }

        /**
         * 
         * Integration Tab
         * 
        **/

        public async Task<FunctionReturnResult<BusinessAppIntegration?>> getBusinessIntegrationById(long businessId, string currentIntegrationId)
        {
            var result = new FunctionReturnResult<BusinessAppIntegration?>();

            try
            {
                var integration = await _businessAppRepository.getBusinessIntegrationById(businessId, currentIntegrationId);

                if (integration == null)
                {
                    result.Code = "getBusinessIntegrationById:1";
                    result.Message = "Integration not found.";
                    return result;
                }

                result.Success = true;
                result.Data = integration;
            }
            catch (Exception ex)
            {
                result.Code = "getBusinessIntegrationById:2";
                result.Message = "Error retrieving business integration: " + ex.Message;
            }

            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppIntegration?>> AddOrUpdateBusinessIntegration(long businessId, IFormCollection formData, string postType, IntegrationData integrationTypeData, BusinessAppIntegration? businessIntegrationData, IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<BusinessAppIntegration?>();

            try
            {
                // Get changes from form data
                if (!formData.TryGetValue("changes", out var changesJsonString))
                {
                    result.Code = "AddOrUpdateBusinessIntegration:1";
                    result.Message = "Changes data not found";
                    return result;
                }

                var changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJsonString);
                if (changesJsonElement == null)
                {
                    result.Code = "AddOrUpdateBusinessIntegration:2";
                    result.Message = "Unable to parse changes json string.";
                    return result;
                }

                // Create new integration object
                var newIntegration = new BusinessAppIntegration
                {
                    Id = postType == "new" ? Guid.NewGuid().ToString() : businessIntegrationData!.Id,
                    Type = integrationTypeData.Id
                };

                // Friendly name validation
                if (!changesJsonElement.RootElement.TryGetProperty("friendlyName", out var friendlyNameElement))
                {
                    result.Code = "AddOrUpdateBusinessIntegration:3";
                    result.Message = "Friendly name not found.";
                    return result;
                }

                string? friendlyName = friendlyNameElement.GetString();
                if (string.IsNullOrWhiteSpace(friendlyName))
                {
                    result.Code = "AddOrUpdateBusinessIntegration:4";
                    result.Message = "Friendly name is required.";
                    return result;
                }
                newIntegration.FriendlyName = friendlyName;

                // Process fields
                var fieldsElement = changesJsonElement.RootElement.GetProperty("fields");
                foreach (var field in integrationTypeData.Fields)
                {
                    if (fieldsElement.TryGetProperty(field.Id, out var valueElement))
                    {
                        string? value = valueElement.GetString();
                        if (string.IsNullOrEmpty(value) && field.Required)
                        {
                            result.Code = "AddOrUpdateBusinessIntegration:5";
                            result.Message = $"Field {field.Name} is required.";
                            return result;
                        }

                        // Validate field value based on type
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (field.Type == "number" && !decimal.TryParse(value, out _))
                            {
                                result.Code = "AddOrUpdateBusinessIntegration:6";
                                result.Message = $"Field {field.Name} must be a valid number.";
                                return result;
                            }
                            else if (field.Type == "select")
                            {
                                if (!field.Options!.Any(o => o.Key == value))
                                {
                                    result.Code = "AddOrUpdateBusinessIntegration:7";
                                    result.Message = $"Invalid option selected for {field.Name}.";
                                    return result;
                                }
                            }
                        }

                        if (field.IsEncrypted)
                        {
                            value = integrationsManager.EncryptField(value);
                            newIntegration.Fields.Add(field.Id, "value_encrypted_after_saving");
                            newIntegration.EncryptedFields.Add(field.Id, value);
                        }
                        else
                        {
                            newIntegration.Fields.Add(field.Id, value);
                        }

                    }
                    else if (field.Required)
                    {
                        result.Code = "AddOrUpdateBusinessIntegration:8";
                        result.Message = $"Required field {field.Name} is missing.";
                        return result;
                    }
                }

                // Save the integration
                if (postType == "new")
                {
                    var addResult = await _businessAppRepository.AddBusinessIntegration(businessId, newIntegration);
                    if (!addResult)
                    {
                        result.Code = "AddOrUpdateBusinessIntegration:9";
                        result.Message = "Failed to add integration.";
                        return result;
                    }
                }
                else
                {
                    var updateResult = await _businessAppRepository.UpdateBusinessIntegration(businessId, newIntegration);
                    if (!updateResult)
                    {
                        result.Code = "AddOrUpdateBusinessIntegration:10";
                        result.Message = "Failed to update integration.";
                        return result;
                    }
                }

                result.Success = true;
                result.Data = newIntegration;
            }
            catch (Exception ex)
            {
                result.Code = "AddOrUpdateBusinessIntegration:11";
                result.Message = "Error processing integration: " + ex.Message;
            }

            return result;
        }

    }
}
