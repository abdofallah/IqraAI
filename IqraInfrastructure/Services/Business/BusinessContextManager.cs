using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IqraInfrastructure.Services.Business
{
    public class BusinessContextManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;

        public BusinessContextManager(BusinessManager businessManager, BusinessAppRepository businessAppRepository, BusinessRepository businessRepository)
        {
            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
        }

        /**
         * 
         * Context Tab
         * 
        **/

        public async Task<FunctionReturnResult<BusinessAppContextBranding?>> UpdateUserBusinessContextBranding(long businessId, IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppContextBranding?>();

            List<string> businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "UpdateUserBusinessContextBranding:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "UpdateUserBusinessContextBranding:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newBusinessContextBranding = new BusinessAppContextBranding();

            // Name validation and assignment
            var nameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "name",
                newBusinessContextBranding.Name
            );
            if (!nameValidationResult.Success)
            {
                result.Code = "UpdateUserBusinessContextBranding:" + nameValidationResult.Code;
                result.Message = nameValidationResult.Message;
                return result;
            }

            // Country validation and assignment
            var countryValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "country",
                newBusinessContextBranding.Country
            );
            if (!countryValidationResult.Success)
            {
                result.Code = "UpdateUserBusinessContextBranding:" + countryValidationResult.Code;
                result.Message = countryValidationResult.Message;
                return result;
            }

            // Email validation and assignment
            var emailValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "email",
                newBusinessContextBranding.Email
            );
            if (!emailValidationResult.Success)
            {
                result.Code = "UpdateUserBusinessContextBranding:" + emailValidationResult.Code;
                result.Message = emailValidationResult.Message;
                return result;
            }

            // Phone validation and assignment
            var phoneValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "phone",
                newBusinessContextBranding.Phone
            );
            if (!phoneValidationResult.Success)
            {
                result.Code = "UpdateUserBusinessContextBranding:" + phoneValidationResult.Code;
                result.Message = phoneValidationResult.Message;
                return result;
            }

            // Website validation and assignment
            var websiteValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "website",
                newBusinessContextBranding.Website
            );
            if (!websiteValidationResult.Success)
            {
                result.Code = "UpdateUserBusinessContextBranding:" + websiteValidationResult.Code;
                result.Message = websiteValidationResult.Message;
                return result;
            }

            // Other Information validation and assignment
            if (!changes.RootElement.TryGetProperty("otherInformation", out var otherInformationElement))
            {
                result.Code = "UpdateUserBusinessContextBranding:3";
                result.Message = "Other information not found.";
                return result;
            }

            foreach (var language in businessLanguages)
            {
                if (!otherInformationElement.TryGetProperty(language, out var languageElement))
                {
                    result.Code = "UpdateUserBusinessContextBranding:4";
                    result.Message = $"Other information for language {language} not found.";
                    return result;
                }

                var languageInfo = new Dictionary<string, string>();
                foreach (var info in languageElement.EnumerateObject())
                {
                    string key = info.Name;
                    string? value = info.Value.GetString();

                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    {
                        result.Code = "UpdateUserBusinessContextBranding:5";
                        result.Message = $"Invalid other information entry for language {language}";
                        return result;
                    }

                    languageInfo.Add(key, value);
                }
                newBusinessContextBranding.OtherInformation[language] = languageInfo;
            }

            // Save to database
            var saveResult = await _businessAppRepository.UpdateBusinessContextBranding(businessId, newBusinessContextBranding);
            if (!saveResult)
            {
                result.Code = "UpdateUserBusinessContextBranding:6";
                result.Message = "Failed to save business context branding.";
                return result;
            }

            result.Success = true;
            result.Data = newBusinessContextBranding;

            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppContextBranch?>> AddOrUpdateUserBusinessContextBranch(long businessId, IFormCollection formData, string postType, string exisitingBranchIdValue)
        {
            var result = new FunctionReturnResult<BusinessAppContextBranch?>();

            List<string> businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newBusinessContextBranch = new BusinessAppContextBranch();

            // General Section
            if (!changes.RootElement.TryGetProperty("general", out var generalElement))
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:3";
                result.Message = "General section not found.";
                return result;
            }

            // Name validation and assignment
            var nameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                generalElement,
                "name",
                newBusinessContextBranch.General.Name
            );
            if (!nameValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:" + nameValidationResult.Code;
                result.Message = nameValidationResult.Message;
                return result;
            }

            // Address validation and assignment
            var addressValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                generalElement,
                "address",
                newBusinessContextBranch.General.Address
            );
            if (!addressValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:" + addressValidationResult.Code;
                result.Message = addressValidationResult.Message;
                return result;
            }

            // Phone validation and assignment
            var phoneValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                generalElement,
                "phone",
                newBusinessContextBranch.General.Phone
            );
            if (!phoneValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:" + phoneValidationResult.Code;
                result.Message = phoneValidationResult.Message;
                return result;
            }

            // Email validation and assignment
            var emailValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                generalElement,
                "email",
                newBusinessContextBranch.General.Email
            );
            if (!emailValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:" + emailValidationResult.Code;
                result.Message = emailValidationResult.Message;
                return result;
            }

            // Website validation and assignment
            var websiteValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                generalElement,
                "website",
                newBusinessContextBranch.General.Website
            );
            if (!websiteValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:" + websiteValidationResult.Code;
                result.Message = websiteValidationResult.Message;
                return result;
            }

            // Other Information validation and assignment
            if (!generalElement.TryGetProperty("otherInformation", out var otherInformationElement))
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:4";
                result.Message = "Other information not found.";
                return result;
            }

            foreach (var language in businessLanguages)
            {
                if (!otherInformationElement.TryGetProperty(language, out var languageElement))
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:5";
                    result.Message = $"Other information for language {language} not found.";
                    return result;
                }

                var languageInfo = new Dictionary<string, string>();
                foreach (var info in languageElement.EnumerateObject())
                {
                    string key = info.Name;
                    string? value = info.Value.GetString();

                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    {
                        result.Code = "AddOrUpdateUserBusinessContextBranch:6";
                        result.Message = $"Invalid other information entry for language {language}";
                        return result;
                    }

                    languageInfo.Add(key, value);
                }
                newBusinessContextBranch.General.OtherInformation[language] = languageInfo;
            }

            // Working Hours
            if (!changes.RootElement.TryGetProperty("workingHours", out var workingHoursElement))
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:7";
                result.Message = "Working hours not found.";
                return result;
            }

            foreach (var dayElement in workingHoursElement.EnumerateObject())
            {
                if (!Enum.TryParse<DayOfWeek>(dayElement.Name, out var day))
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:8";
                    result.Message = $"Invalid day value: {dayElement.Name}";
                    return result;
                }

                var workingHours = new BusinessAppContextBranchWorkingHours();

                if (!dayElement.Value.TryGetProperty("isClosed", out var isClosedElement))
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:9";
                    result.Message = $"IsClosed property not found for day {day}";
                    return result;
                }
                workingHours.IsClosed = isClosedElement.GetBoolean();

                if (!workingHours.IsClosed)
                {
                    if (!dayElement.Value.TryGetProperty("timings", out var timingsElement))
                    {
                        result.Code = "AddOrUpdateUserBusinessContextBranch:10";
                        result.Message = $"Timings not found for day {day}";
                        return result;
                    }

                    foreach (var timing in timingsElement.EnumerateArray())
                    {
                        if (timing.GetArrayLength() != 2)
                        {
                            result.Code = "AddOrUpdateUserBusinessContextBranch:11";
                            result.Message = $"Invalid timing format for day {day}";
                            return result;
                        }

                        if (!TimeOnly.TryParse(timing[0].GetString(), out var startTime) ||
                            !TimeOnly.TryParse(timing[1].GetString(), out var endTime))
                        {
                            result.Code = "AddOrUpdateUserBusinessContextBranch:12";
                            result.Message = $"Invalid time format for day {day}";
                            return result;
                        }

                        workingHours.Timings.Add((startTime, endTime));
                    }
                }

                newBusinessContextBranch.WorkingHours[dayElement.Name] = workingHours;
            }

            // Team
            if (!changes.RootElement.TryGetProperty("team", out var teamElement))
            {
                result.Code = "AddOrUpdateUserBusinessContextBranch:13";
                result.Message = "Team not found.";
                return result;
            }

            foreach (var teamMember in teamElement.EnumerateArray())
            {
                var newTeamMember = new BusinessAppContextBranchTeam();

                // Name validation and assignment
                var teamNameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    teamMember,
                    "name",
                    newTeamMember.Name
                );
                if (!teamNameValidationResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:" + teamNameValidationResult.Code;
                    result.Message = teamNameValidationResult.Message;
                    return result;
                }

                // Role validation and assignment
                var teamRoleValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    teamMember,
                    "role",
                    newTeamMember.Role
                );
                if (!teamRoleValidationResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:" + teamRoleValidationResult.Code;
                    result.Message = teamRoleValidationResult.Message;
                    return result;
                }

                // Email validation and assignment
                var teamEmailValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    teamMember,
                    "email",
                    newTeamMember.Email,
                    true
                );
                if (!teamEmailValidationResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:" + teamEmailValidationResult.Code;
                    result.Message = teamEmailValidationResult.Message;
                    return result;
                }

                // Phone validation and assignment
                var teamPhoneValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    teamMember,
                    "phone",
                    newTeamMember.Phone,
                    true
                );
                if (!teamPhoneValidationResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:" + teamPhoneValidationResult.Code;
                    result.Message = teamPhoneValidationResult.Message;
                    return result;
                }

                // Information validation and assignment
                var teamInformationValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    teamMember,
                    "information",
                    newTeamMember.Information,
                    true
                );
                if (!teamInformationValidationResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:" + teamInformationValidationResult.Code;
                    result.Message = teamInformationValidationResult.Message;
                    return result;
                }

                newBusinessContextBranch.Team.Add(newTeamMember);
            }

            // Save to database
            if (postType == "new")
            {
                newBusinessContextBranch.Id = Guid.NewGuid().ToString();
                var addResult = await _businessAppRepository.AddBusinessContextBranch(businessId, newBusinessContextBranch);
                if (!addResult)
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:14";
                    result.Message = "Failed to add business context branch.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newBusinessContextBranch.Id = exisitingBranchIdValue;
                var updateResult = await _businessAppRepository.UpdateBusinessContextBranch(businessId, newBusinessContextBranch);
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateUserBusinessContextBranch:15";
                    result.Message = "Failed to update business context branch.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newBusinessContextBranch;
            return result;
        }

        public async Task<bool> CheckBusinessBranchExists(long businessId, string branchId)
        {
            var result = await _businessAppRepository.CheckBusinessAppBranchExists(businessId, branchId);

            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppContextService?>> AddOrUpdateUserBusinessContextService(long businessId, IFormCollection formData, string postType, string? existingServiceId)
        {
            var result = new FunctionReturnResult<BusinessAppContextService?>();

            List<string> businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateUserBusinessContextService:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateUserBusinessContextService:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newBusinessContextService = new BusinessAppContextService();

            // Name validation and assignment
            var nameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "name",
                newBusinessContextService.Name
            );
            if (!nameValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextService:" + nameValidationResult.Code;
                result.Message = nameValidationResult.Message;
                return result;
            }

            // Short Description validation and assignment
            var shortDescValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "shortDescription",
                newBusinessContextService.ShortDescription
            );
            if (!shortDescValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextService:" + shortDescValidationResult.Code;
                result.Message = shortDescValidationResult.Message;
                return result;
            }

            // Long Description validation and assignment
            var longDescValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "longDescription",
                newBusinessContextService.LongDescription
            );
            if (!longDescValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextService:" + longDescValidationResult.Code;
                result.Message = longDescValidationResult.Message;
                return result;
            }

            // Available Branches validation
            if (!changes.RootElement.TryGetProperty("availableAtBranches", out var branchesElement))
            {
                result.Code = "AddOrUpdateUserBusinessContextService:3";
                result.Message = "Available branches not found.";
                return result;
            }

            foreach (var branchId in branchesElement.EnumerateArray())
            {
                string? branchIdString = branchId.GetString();
                if (string.IsNullOrWhiteSpace(branchIdString))
                {
                    result.Code = "AddOrUpdateUserBusinessContextService:4";
                    result.Message = "Invalid branch id found.";
                    return result;
                }

                bool branchExists = await CheckBusinessBranchExists(businessId, branchIdString);
                if (!branchExists)
                {
                    result.Code = "AddOrUpdateUserBusinessContextService:5";
                    result.Message = $"Branch not found: {branchIdString}";
                    return result;
                }

                newBusinessContextService.AvailableAtBranches.Add(branchIdString);
            }

            // Related Products validation
            if (!changes.RootElement.TryGetProperty("relatedProducts", out var productsElement))
            {
                result.Code = "AddOrUpdateUserBusinessContextService:6";
                result.Message = "Related products not found.";
                return result;
            }

            foreach (var productId in productsElement.EnumerateArray())
            {
                string? productIdString = productId.GetString();
                if (string.IsNullOrWhiteSpace(productIdString))
                {
                    result.Code = "AddOrUpdateUserBusinessContextService:7";
                    result.Message = "Invalid product id found.";
                    return result;
                }

                bool productExists = await CheckBusinessProductExists(businessId, productIdString);
                if (!productExists)
                {
                    result.Code = "AddOrUpdateUserBusinessContextService:8";
                    result.Message = $"Product not found: {productIdString}";
                    return result;
                }

                newBusinessContextService.RelatedProducts.Add(productIdString);
            }

            // Other Information validation and assignment
            if (!changes.RootElement.TryGetProperty("otherInformation", out var otherInfoElement))
            {
                result.Code = "AddOrUpdateUserBusinessContextService:9";
                result.Message = "Other information not found.";
                return result;
            }

            foreach (var language in businessLanguages)
            {
                if (!otherInfoElement.TryGetProperty(language, out var languageElement))
                {
                    result.Code = "AddOrUpdateUserBusinessContextService:10";
                    result.Message = $"Other information for language {language} not found.";
                    return result;
                }

                var languageInfo = new Dictionary<string, string>();
                foreach (var info in languageElement.EnumerateObject())
                {
                    string key = info.Name;
                    string? value = info.Value.GetString();

                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    {
                        result.Code = "AddOrUpdateUserBusinessContextService:11";
                        result.Message = $"Invalid other information entry for language {language}";
                        return result;
                    }

                    languageInfo.Add(key, value);
                }
                newBusinessContextService.OtherInformation[language] = languageInfo;
            }

            // Save to database
            if (postType == "new")
            {
                newBusinessContextService.Id = Guid.NewGuid().ToString();
                var addResult = await _businessAppRepository.AddBusinessContextService(businessId, newBusinessContextService);
                if (!addResult)
                {
                    result.Code = "AddOrUpdateUserBusinessContextService:12";
                    result.Message = "Failed to add business context service.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newBusinessContextService.Id = existingServiceId;
                var updateResult = await _businessAppRepository.UpdateBusinessContextService(businessId, newBusinessContextService);
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateUserBusinessContextService:13";
                    result.Message = "Failed to update business context service.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newBusinessContextService;
            return result;
        }

        public async Task<bool> CheckBusinessServiceExists(long businessId, string exisitingServiceId)
        {
            var result = await _businessAppRepository.CheckBusinessAppContextServiceExists(businessId, exisitingServiceId);

            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppContextProduct?>> AddOrUpdateUserBusinessContextProduct(long businessId, IFormCollection formData, string postType, string? existingProductId)
        {
            var result = new FunctionReturnResult<BusinessAppContextProduct?>();

            List<string> businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateUserBusinessContextProduct:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateUserBusinessContextProduct:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newBusinessContextProduct = new BusinessAppContextProduct();

            // Name validation and assignment
            var nameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "name",
                newBusinessContextProduct.Name
            );
            if (!nameValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextProduct:" + nameValidationResult.Code;
                result.Message = nameValidationResult.Message;
                return result;
            }

            // Short Description validation and assignment
            var shortDescValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "shortDescription",
                newBusinessContextProduct.ShortDescription
            );
            if (!shortDescValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextProduct:" + shortDescValidationResult.Code;
                result.Message = shortDescValidationResult.Message;
                return result;
            }

            // Long Description validation and assignment
            var longDescValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                changes.RootElement,
                "longDescription",
                newBusinessContextProduct.LongDescription
            );
            if (!longDescValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessContextProduct:" + longDescValidationResult.Code;
                result.Message = longDescValidationResult.Message;
                return result;
            }

            // Available Branches validation
            if (!changes.RootElement.TryGetProperty("availableAtBranches", out var branchesElement))
            {
                result.Code = "AddOrUpdateUserBusinessContextProduct:3";
                result.Message = "Available branches not found.";
                return result;
            }

            foreach (var branchId in branchesElement.EnumerateArray())
            {
                string? branchIdString = branchId.GetString();
                if (string.IsNullOrWhiteSpace(branchIdString))
                {
                    result.Code = "AddOrUpdateUserBusinessContextProduct:4";
                    result.Message = "Invalid branch id found.";
                    return result;
                }

                bool branchExists = await CheckBusinessBranchExists(businessId, branchIdString);
                if (!branchExists)
                {
                    result.Code = "AddOrUpdateUserBusinessContextProduct:5";
                    result.Message = $"Branch not found: {branchIdString}";
                    return result;
                }

                newBusinessContextProduct.AvailableAtBranches.Add(branchIdString);
            }

            // Other Information validation and assignment
            if (!changes.RootElement.TryGetProperty("otherInformation", out var otherInfoElement))
            {
                result.Code = "AddOrUpdateUserBusinessContextProduct:6";
                result.Message = "Other information not found.";
                return result;
            }

            foreach (var language in businessLanguages)
            {
                if (!otherInfoElement.TryGetProperty(language, out var languageElement))
                {
                    result.Code = "AddOrUpdateUserBusinessContextProduct:7";
                    result.Message = $"Other information for language {language} not found.";
                    return result;
                }

                var languageInfo = new Dictionary<string, string>();
                foreach (var info in languageElement.EnumerateObject())
                {
                    string key = info.Name;
                    string? value = info.Value.GetString();

                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    {
                        result.Code = "AddOrUpdateUserBusinessContextProduct:8";
                        result.Message = $"Invalid other information entry for language {language}";
                        return result;
                    }

                    languageInfo.Add(key, value);
                }
                newBusinessContextProduct.OtherInformation[language] = languageInfo;
            }

            // Save to database
            if (postType == "new")
            {
                newBusinessContextProduct.Id = Guid.NewGuid().ToString();
                var addResult = await _businessAppRepository.AddBusinessContextProduct(businessId, newBusinessContextProduct);
                if (!addResult)
                {
                    result.Code = "AddOrUpdateUserBusinessContextProduct:9";
                    result.Message = "Failed to add business context product.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newBusinessContextProduct.Id = existingProductId;
                var updateResult = await _businessAppRepository.UpdateBusinessContextProduct(businessId, newBusinessContextProduct);
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateUserBusinessContextProduct:10";
                    result.Message = "Failed to update business context product.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newBusinessContextProduct;
            return result;
        }

        public async Task<bool> CheckBusinessProductExists(long businessId, string productId)
        {
            var result = await _businessAppRepository.CheckBusinessAppProductExists(businessId, productId);

            return result;
        }

    }
}
