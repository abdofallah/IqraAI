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

        public async Task<BusinessNumberData?> GetBusinessNumberById(long businessId, string numberId)
        {
            var numberData = await _businessAppRepository.GetBusinessNumberById(businessId, numberId);
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

        public async Task<FunctionReturnResult<BusinessNumberData?>> AddOrUpdateBusinessNumber(JsonDocument? changes, string countryCode, string number, BusinessNumberProviderEnum provider, string postType, BusinessNumberData? exisitingNumberData, long businessId, RegionManager regionManager)
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

                bool addNumberResult = await _businessAppRepository.AddBusinessNumber(businessId, newNumberData);
                if (!addNumberResult)
                {
                    result.Code = "AddOrUpdateBusinessNumber:10";
                    result.Message = $"Failed to add number to business.";
                    return result;
                }
            }
            else
            {
                newNumberData.Id = exisitingNumberData.Id;

                bool updateNumberResult = await _businessAppRepository.UpdateBusinessNumber(businessId, newNumberData);
                if (!updateNumberResult)
                {
                    result.Code = "AddOrUpdateBusinessNumber:11";
                    result.Message = $"Failed to update number.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newNumberData;
            return result;
        }
    }
}
