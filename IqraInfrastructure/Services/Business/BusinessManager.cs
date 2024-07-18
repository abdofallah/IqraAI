using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace IqraInfrastructure.Services.Business
{
    public class BusinessManager
    {
        private readonly BusinessRepository _businessRepository;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessLogoRepository _businessLogoRepository;

        public BusinessManager(BusinessRepository businessRepository, BusinessAppRepository businessAppRepository, BusinessLogoRepository businessLogoRepository)
        {
            _businessRepository = businessRepository;
            _businessAppRepository = businessAppRepository;
            _businessLogoRepository = businessLogoRepository;
        }

        public async Task<BusinessData> AddBusiness(BusinessData businessData, IFormFile businessLogoFile)
        {
            businessData.Id = await _businessRepository.GetNextBusinessId();

            var (webpImage, hash) = await ImageHelper.ConvertScaleAndHashToWebp(businessLogoFile);
            bool fileExists = await _businessLogoRepository.FileExists(hash);
            if (!fileExists)
            {
                await _businessLogoRepository.PutFileAsByteData(hash + ".webp", webpImage, new Dictionary<string, string>());
            }

            businessData.LogoURL = hash;

            await _businessRepository.AddBusinessAsync(businessData);
            await _businessAppRepository.AddBusinessAppAsync(
                new BusinessApp()
                {
                    Id = businessData.Id,
                }
            );

            return businessData;
        }

        public async Task<FunctionReturnResult<List<BusinessData>>> GetUserBusinessesByEmail(string userEmail)
        {
            var result = new FunctionReturnResult<List<BusinessData>>();
            result.Data = new List<BusinessData>();

            var businesses = await _businessRepository.GetBusinessesByMasterUserEmailAsync(userEmail);
            if (businesses == null)
            {
                result.Code = 1;
                result.Message = "Null - Businesses not found for user: " + userEmail;
                Log.Logger.Error("[BusinessManager] " + result.Message);
            }
            else
            {
                result.Success = true;
                result.Data = businesses;
            }

            return result;
        }

        public async Task<FunctionReturnResult<List<BusinessData>?>> GetUserBusinessesByIds(List<long> businessesId, string userEmail)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();
            result.Data = null;

            if (businessesId.Count == 0)
            {
                result.Success = true;
                result.Data = new List<BusinessData>();
                return result;
            }

            var getResult = await _businessRepository.GetBusinessesAsync(businessesId);
            if (getResult == null)
            {
                result.Code = 1;
                result.Message = "Null - Businesses not found for user: " + userEmail;
                Log.Logger.Error("[BusinessManager] " + result.Message);
            }
            else if (businessesId.Count != getResult.Count)
            {
                result.Code = 2;
                result.Message = "Not all bussiness found for user: " + userEmail;
                Log.Logger.Error("[BusinessManager] " + result.Message);
            }
            else
            {
                result.Success = true;
                result.Data = getResult;
            }

            return result;
        }

        public async Task<FunctionReturnResult<BusinessData?>> GetUserBusinessById(long businessId, string userEmail)
        {
            var result = new FunctionReturnResult<BusinessData?>();
            result.Data = null;

            BusinessData? businessData = await _businessRepository.GetBusinessAsync(businessId);
            if (businessData == null)
            {
                result.Code = 1;
                Log.Logger.Error("[BusinessManager] Null - Business not found for user: " + userEmail);
            }
            else
            {
                result.Success = true;
                result.Data = businessData;
            }

            return result;
        }

        public async Task<FunctionReturnResult<BusinessApp?>> GetUserBusinessAppById(long businessId, string userEmail)
        {
            var result = new FunctionReturnResult<BusinessApp?>();
            result.Data = null;

            BusinessApp? businessApp = await _businessAppRepository.GetBusinessAppAsync(businessId);
            if (businessApp == null)
            {
                result.Code = 1;
                Log.Logger.Error("[BusinessManager] Null - Business app not found for user: " + userEmail);
            }
            else
            {
                result.Success = true;
                result.Data = businessApp;
            }

            return result;
        }

        public async Task<FunctionReturnResult<List<BusinessData>?>> GetBusinesses(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();
            result.Data = null;

            var businesses = await _businessRepository.GetBusinessesAsync(page, pageSize);
            if (businesses == null)
            {
                result.Code = 1;
                Log.Logger.Error("[BusinessManager] Null - Businesses not found");
            }
            else
            {
                result.Success = true;
                result.Data = businesses;
            }

            return result;
        }
    
        public async Task<FunctionReturnResult<List<BusinessData>?>> SearchBusinesses(string query, int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();
            result.Data = null;

            var businesses = await _businessRepository.SearchBusinessesAsync(query, page, pageSize);
            if (businesses == null)
            {
                result.Code = 1;
                Log.Logger.Error("[BusinessManager] Null - Search Businesses not found");
            }
            else
            {
                result.Success = true;
                result.Data = businesses;
            }

            return result;
        }

        public async Task<FunctionReturnResult<bool?>> UpdateUserBusinessSettings()
        {
            var result = new FunctionReturnResult<bool?>();
            
            return result;
        }
    }
}
