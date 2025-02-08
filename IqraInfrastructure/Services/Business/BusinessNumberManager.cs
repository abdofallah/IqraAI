using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Services.App;
using IqraInfrastructure.Services.User;
using System.Text.Json;

namespace IqraInfrastructure.Services.Business
{
    public class BusinessNumberManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;

        public BusinessNumberManager(BusinessManager businessManager, BusinessAppRepository businessAppRepository, BusinessRepository businessRepository)
        {
            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
        }

        public async Task<BusinessNumberData?> GetBusinessNumberById(long businessId)
        {
            var numberData = await _businessAppRepository.GetBusinessNumberById(businessId);
            return numberData;
        }

        public async Task<bool> CheckBusinessNumberExistsByNumber(string numberCountryCode, string phoneNumber, long businessId)
        {
            return await _businessAppRepository.CheckBusinessNumberExistsByNumber(numberCountryCode, phoneNumber, businessId);
        }

        public async Task<bool> CheckBusinessNumberExistsById(string exisitingNumberId, long businessId)
        {
            return await _businessAppRepository.CheckBusinessNumberExistsById(exisitingNumberId, businessId);
        }

        public async Task<FunctionReturnResult<BusinessNumberData?>> AddOrUpdateBusinessNumber(JsonDocument? changes, string countryCode, string number, BusinessNumberProviderEnum provider, string postType, BusinessNumberData? exisitingNumberData, string userEmail, RegionManager regionManager)
        {
            var result = new FunctionReturnResult<BusinessNumberData?>();

            BusinessNumberData newNumberData = new BusinessNumberData()
            {
                CountryCode = countryCode,
                Number = number,
                Provider = provider
            };

            // Get region ID
            if (!changes.RootElement.TryGetProperty("regionId", out var regionIdElement))
            {
                result.Code = "AddOrUpdateBusinessNumber:1";
                result.Message = "Region ID not found in changes.";
                return result;
            }
            string? regionId = regionIdElement.GetString();
            if (string.IsNullOrWhiteSpace(regionId))
            {
                result.Code = "AddOrUpdateBusinessNumber:2";
                result.Message = "Region ID cannot be empty.";
                return result;
            }

            // Validate region exists
            var regionData = await regionManager.GetRegionById(regionId);
            if (regionData == null)
            {
                result.Code = "AddOrUpdateBusinessNumber:3";
                result.Message = "Region not found.";
                return result;
            }
            if (regionData.DisabledAt != null)
            {
                result.Code = "AddOrUpdateBusinessNumber:4";
                result.Message = "Region is disabled.";
                return result;
            }

            newNumberData.RegionId = regionId;

            // Get assigned business ID
            long? assignedBusinessId = null;
            if (changes.RootElement.TryGetProperty("assignedToBusinessId", out var businessIdElement) && businessIdElement.ValueKind != JsonValueKind.Null)
            {
                if (!businessIdElement.TryGetInt64(out var businessId))
                {
                    result.Code = "AddOrUpdateBusinessNumber:5";
                    result.Message = "Invalid business ID.";
                    return result;
                }

                // Validate business exists and user owns it
                bool businessExists = await businessManager.CheckUserBusinessExists(businessId, userEmail);
                if (!businessExists)
                {
                    result.Code = "AddOrUpdateBusinessNumber:6";
                    result.Message = "Business not found.";
                    return result;
                }

                assignedBusinessId = businessId;
            }
            newNumberData.AssignedToBusinessId = assignedBusinessId;

            if (provider == BusinessNumberProviderEnum.Unknown)
            {
                result.Code = "AddOrUpdateBusinessNumber:7";
                result.Message = "Invalid provider type.";
                return result;
            }

            if (provider == BusinessNumberProviderEnum.Physical)
            {
                newNumberData = new BusinessNumberPhysicalData(newNumberData)
                {
                    Status = BusinessNumberPhysicalStatusEnum.Offline
                };
            }
            else if (provider == BusinessNumberProviderEnum.Twilio || provider == BusinessNumberProviderEnum.Vonage || provider == BusinessNumberProviderEnum.Telnyx)
            {
                result.Code = "AddOrUpdateBusinessNumber:8";
                result.Message = "Provider type currently not implemented.";
                return result;
            }
            else
            {
                result.Code = "AddOrUpdateBusinessNumber:9";
                result.Message = "Invalid provider type.";
                return result;
            }

            if (postType == "new")
            {
                newNumberData.Id = Guid.NewGuid().ToString();

                await _numberRepository.InsertNumberAsync(newNumberData);

                bool addNumberUserResult = await userManager.addNumberIdToUser(newNumberData.Id, userEmail);
                if (!addNumberUserResult)
                {
                    result.Code = "AddOrUpdateBusinessNumber:10";
                    result.Message = $"Failed to add number to user.";
                    return result;
                }
            }
            else
            {
                newNumberData.Id = exisitingNumberData.Id;

                bool updateNumberResult = await _numberRepository.ReplaceNumberAsync(newNumberData);
                if (!updateNumberResult)
                {
                    result.Code = "AddOrUpdateBusinessNumber:11";
                    result.Message = $"Failed to update number.";
                    return result;
                }

                if (exisitingNumberData.AssignedToBusinessId != null)
                {
                    bool removeNumberFromOldBusinessResult = await businessManager.removeNumberIdFromBusiness(newNumberData.Id, exisitingNumberData.AssignedToBusinessId.Value);
                    if (!removeNumberFromOldBusinessResult)
                    {
                        // TODO CRITICAL ERROR THIS WILL BREAK NUMBERING

                        result.Code = "AddOrUpdateBusinessNumber:12";
                        result.Message = $"Failed to remove number from old business.";
                        return result;
                    }
                }
            }

            if (newNumberData.AssignedToBusinessId != null)
            {
                bool addNumberBusinessResult = await businessManager.addNumberIdToBusiness(newNumberData.Id, newNumberData.AssignedToBusinessId.Value);
                if (!addNumberBusinessResult)
                {
                    // TODO REMOVE NUMBER AND NUMBER FROM USER

                    result.Code = "AddOrUpdateBusinessNumber:13";
                    result.Message = $"Failed to add number to business.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newNumberData;
            return result;
        }
    }
}
