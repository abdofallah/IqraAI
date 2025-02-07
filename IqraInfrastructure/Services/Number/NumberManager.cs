using IqraCore.Entities.Helper.Number;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Number;
using IqraInfrastructure.Repositories.Number;
using IqraInfrastructure.Services.App;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.User;
using Serilog;
using System.Text.Json;

namespace IqraInfrastructure.Services.Number
{
    public class NumberManager
    {
        public readonly NumberRepository _numberRepository;

        public NumberManager(NumberRepository numberRepository) {
            _numberRepository = numberRepository;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumberByIds(List<string> numberIds, string userEmail)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            if (numberIds.Count == 0)
            {
                result.Success = true;
                result.Data = new List<NumberData>();

                return result;
            }

            var numberResults = await _numberRepository.GetUserNumberByIdsAsync(numberIds, userEmail);

            if (numberResults == null) {
                result.Code = "GetUserNumberByIds:1";

                result.Message = "Null - Numbers not found for user: " + userEmail;
                Log.Logger.Error("[NumberManager] " + result.Message);

                return result;
            }

            if (numberResults.Count != numberIds.Count)
            {
                result.Code = "GetUserNumberByIds:2";

                result.Message = "Not all numbers found for user: " + userEmail;
                Log.Logger.Error("[NumberManager] " + result.Message);

                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetBusinessNumberByIds(List<string> numberIds, long businessId)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            if (numberIds.Count == 0)
            {
                result.Success = true;
                result.Data = new List<NumberData>();

                return result;
            }

            var numberResults = await _numberRepository.GetBusinessNumberByIdsAsync(numberIds, businessId);

            if (numberResults == null)
            {
                result.Code = "GetBusinessNumberByIds:1";

                result.Message = "Null - Numbers not found for business: " + businessId;
                Log.Logger.Error("[NumberManager] " + result.Message);

                return result;
            }

            if (numberResults.Count != numberIds.Count)
            {
                result.Code = "GetBusinessNumberByIds:2";

                result.Message = "Not all numbers found for business: " + businessId;
                Log.Logger.Error("[NumberManager] " + result.Message);

                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetNumbers(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            var numberResults = await _numberRepository.GetNumbersAsync(page, pageSize);

            if (numberResults == null)
            {
                result.Code = "GetNumbers:1";

                result.Message = "Null - Numbers not found";
                Log.Logger.Error("[NumberManager] " + result.Message);
                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetNumbersByProvider(NumberProviderEnum provider, int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            var numberResults = await _numberRepository.GetNumbersByProviderAsync(provider, page, pageSize);

            if (numberResults == null)
            {
                result.Code = "GetNumbersByProvider:1";

                result.Message = "Null - Numbers not found";
                Log.Logger.Error("[NumberManager] " + result.Message);
                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumbersByProvider(NumberProviderEnum provider, string email, int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            var numberResults = await _numberRepository.GetUserNumbersByProvider(provider, email, page, pageSize);

            if (numberResults == null)
            {
                result.Code = "GetUserNumbersByProvider:1";

                result.Message = "Null - Numbers not found";
                Log.Logger.Error("[NumberManager] " + result.Message);
                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumbers(string email)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            var numberResults = await _numberRepository.GetUserNumbers(email);

            if (numberResults == null)
            {
                result.Code = "GetUserNumbers:1";

                result.Message = "Null - Numbers not found";
                Log.Logger.Error("[NumberManager] " + result.Message);
                return result;
            }

            result.Success = true;
            result.Data = numberResults;

            return result;
        }

        public async Task<NumberData?> GetUserNumberById(string exisitingNumberId, string userEmail)
        {
            return await _numberRepository.GetUserNumberById(exisitingNumberId, userEmail);
        }

        public async Task<bool> CheckUserNumberExistsByNumber(string numberCountryCode, string phoneNumber, string userEmail)
        {
            return await _numberRepository.CheckUserNumberExistsByNumber(numberCountryCode, phoneNumber, userEmail);
        }

        public async Task<bool> CheckUserNumberExistsById(string exisitingNumberId, string userEmail)
        {
            return await _numberRepository.CheckUserNumberExists(exisitingNumberId, userEmail);
        }

        /**
         * 
         * USER API CONTROLLER
         * 
         * **/

        public async Task<FunctionReturnResult<NumberData?>> AddOrUpdateUserNumber(JsonDocument? changes, string countryCode, string number, NumberProviderEnum provider, string postType, NumberData? exisitingNumberData, string userEmail, UserManager userManager, BusinessManager businessManager, RegionManager regionManager)
        {
            var result = new FunctionReturnResult<NumberData?>();         

            NumberData newNumberData = new NumberData()
            {
                MasterUserEmail = userEmail,
                CountryCode = countryCode,
                Number = number,
                Provider = provider
            };

            // Get region ID
            if (!changes.RootElement.TryGetProperty("regionId", out var regionIdElement))
            {
                result.Code = "AddOrUpdateUserNumber:1";
                result.Message = "Region ID not found in changes.";
                return result;
            }
            string? regionId = regionIdElement.GetString();
            if (string.IsNullOrWhiteSpace(regionId))
            {
                result.Code = "AddOrUpdateUserNumber:2";
                result.Message = "Region ID cannot be empty.";
                return result;
            }

            // Validate region exists
            var regionData = await regionManager.GetRegionById(regionId);
            if (regionData == null)
            {
                result.Code = "AddOrUpdateUserNumber:3";
                result.Message = "Region not found.";
                return result;
            }
            if (regionData.DisabledAt != null)
            {
                result.Code = "AddOrUpdateUserNumber:4";
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
                    result.Code = "AddOrUpdateUserNumber:5";
                    result.Message = "Invalid business ID.";
                    return result;
                }

                // Validate business exists and user owns it
                bool businessExists = await businessManager.CheckUserBusinessExists(businessId, userEmail);
                if (!businessExists)
                {
                    result.Code = "AddOrUpdateUserNumber:6";
                    result.Message = "Business not found.";
                    return result;
                }

                assignedBusinessId = businessId;
            }
            newNumberData.AssignedToBusinessId = assignedBusinessId;

            if (provider == NumberProviderEnum.Unknown)
            {
                result.Code = "AddOrUpdateUserNumber:7";
                result.Message = "Invalid provider type.";
                return result;
            }

            if (provider == NumberProviderEnum.Physical)
            {
                newNumberData = new NumberPhysicalData(newNumberData)
                {
                    Status = NumberPhysicalStatusEnum.Offline
                };
            }
            else if (provider == NumberProviderEnum.Twilio || provider == NumberProviderEnum.Vonage || provider == NumberProviderEnum.Telnyx)
            {
                result.Code = "AddOrUpdateUserNumber:8";
                result.Message = "Provider type currently not implemented.";
                return result;
            }
            else
            {
                result.Code = "AddOrUpdateUserNumber:9";
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
                    result.Code = "AddOrUpdateUserNumber:10";
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
                    result.Code = "AddOrUpdateUserNumber:11";
                    result.Message = $"Failed to update number.";
                    return result;
                }

                if (exisitingNumberData.AssignedToBusinessId != null)
                {
                    bool removeNumberFromOldBusinessResult = await businessManager.removeNumberIdFromBusiness(newNumberData.Id, exisitingNumberData.AssignedToBusinessId.Value);
                    if (!removeNumberFromOldBusinessResult)
                    {
                        // TODO CRITICAL ERROR THIS WILL BREAK NUMBERING

                        result.Code = "AddOrUpdateUserNumber:12";
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

                    result.Code = "AddOrUpdateUserNumber:13";
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
