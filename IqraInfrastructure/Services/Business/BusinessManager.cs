using IqraCore.Entities.Business;
using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;
using System.Text.Json;

namespace IqraInfrastructure.Services.Business
{
    public class BusinessManager
    {
        private readonly BusinessRepository _businessRepository;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessLogoRepository _businessLogoRepository;
        private readonly BusinessWhiteLabelDomainRepository _businessWhiteLabelDomainRepository;

        public BusinessManager(BusinessRepository businessRepository, BusinessAppRepository businessAppRepository, BusinessLogoRepository businessLogoRepository, BusinessWhiteLabelDomainRepository businessWhiteLabelDomainRepository)
        {
            _businessRepository = businessRepository;
            _businessAppRepository = businessAppRepository;
            _businessLogoRepository = businessLogoRepository;
            _businessWhiteLabelDomainRepository = businessWhiteLabelDomainRepository;
        }

        public async Task<BusinessData> AddBusiness(BusinessData businessData, IFormFile? businessLogoFile)
        {
            long bussinessId = await _businessRepository.GetNextBusinessId();
            businessData.Id = bussinessId;

            if (businessLogoFile != null)
            {
                var (webpImage, hash) = await ImageHelper.ConvertScaleAndHashToWebp(businessLogoFile);
                bool fileExists = await _businessLogoRepository.FileExists(hash);
                if (!fileExists)
                {
                    await _businessLogoRepository.PutFileAsByteData(hash + ".webp", webpImage, new Dictionary<string, string>());
                }

                businessData.LogoURL = hash;
            }

            var businessApp = new BusinessApp()
            {
                Id = bussinessId,
            };

            string subDomainHash = SubdomainHashGenerator.GenerateSubdomainHash(bussinessId);

            long businessWhiteLabelId = await _businessWhiteLabelDomainRepository.GetNextBusinessWhiteLabelDomainId();
            businessData.WhiteLabelDomainIds.Add(businessWhiteLabelId);

            var businessWhiteLabelDomain = new BusinessWhiteLabelIqraSubDomain()
            {
                Id = businessWhiteLabelId,
                BusinessId = bussinessId,
                SubDomain = subDomainHash,
                Type = BusinessUserWhiteLabelDomainTypeEnum.IqraSubdomain
            };
            
            await _businessWhiteLabelDomainRepository.AddBusinessWhiteLabelDomainAsync(businessWhiteLabelDomain);

            await _businessAppRepository.AddBusinessAppAsync(businessApp);

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

        public async Task<FunctionReturnResult<bool?>> UpdateUserBusinessSettings(long businessId, IFormCollection formData)
        {
            var result = new FunctionReturnResult<bool?>();

            bool updateBusinessApp = false;

            BusinessData? businessDataBackup = await _businessRepository.GetBusinessAsync(businessId);

            BusinessData? businessData = await _businessRepository.GetBusinessAsync(businessId);
            BusinessApp? businessApp = await _businessAppRepository.GetBusinessAppAsync(businessId);

            List<UpdateDefinition<BusinessData>> updateDefinitions = new List<UpdateDefinition<BusinessData>>();

            // General
            string? businessName = formData["general.name"];
            if (!string.IsNullOrWhiteSpace(businessName))
            {
                updateDefinitions.Add(Builders<BusinessData>.Update.Set(x => x.Name, businessName));
            }

            IFormFile? businessLogo = formData.Files.FirstOrDefault(x => x.Name == "general.logo");
            if (businessLogo != null)
            {
                int logoValidateResult = ImageHelper.ValidateBusinessLogoFile(businessLogo);

                if (logoValidateResult == 0)
                {
                    result.Code = 2;
                    result.Message = "The business logo file is too big. Maximum size is 5MB.";
                    return result;
                }
                else if (logoValidateResult == 1) {
                    result.Code = 3;
                    result.Message = "The business logo file is not valid.";
                    return result;
                }
                else if (logoValidateResult != 200)
                {
                    result.Code = 4;
                    result.Message = "The business logo file is not valid.";
                    return result;
                }
            }

            // Languages
            string? businessLanguagesString = formData["languages"].ToString();
            if (!string.IsNullOrWhiteSpace(businessLanguagesString))
            {
                List<string>? businessLanguages = businessLanguagesString.Split(',').ToList();
                if (businessLanguages == null || businessLanguages.Count == 0)
                {
                    result.Code = 5;
                    result.Message = "Must have at least one language selected.";
                    return result;
                }

                // todo validate languages - create a language repo or list taht iqra allows somewhere

                int addedCount = 0;
                int remainedCount = 0;
                foreach (string oldLanguage in businessData.Languages)
                {
                    if (businessLanguages.Contains(oldLanguage))
                    {
                        remainedCount++;
                    }
                }
                foreach (string newLanguage in businessLanguages)
                {
                    if (!businessData.Languages.Contains(newLanguage))
                    {
                        addedCount++;
                    }
                }

                if (remainedCount + addedCount == 0)
                {
                    result.Code = 6;
                    result.Message = "Must have at least one language selected.";
                    result.Data = false;
                    return result;
                }

                var businessLanguagesUpdateResult = MultiLanguageHelper.UpdateObjectMultiLanguages(businessApp, businessLanguages, businessData.Languages);
                if (!businessLanguagesUpdateResult.Success)
                {
                    result.Code = 100 + businessLanguagesUpdateResult.Code;
                    result.Message = businessLanguagesUpdateResult.Message;
                    return result;
                }

                updateDefinitions.Add(Builders<BusinessData>.Update.Set(d => d.Languages, businessLanguages));

                // todo unpublish the business, calls etc if languages are different than saved ones

                updateBusinessApp = true;
            }         

            // Logo upload after all the validation
            if (businessLogo != null)
            {
                var (webpImage, hash) = await ImageHelper.ConvertScaleAndHashToWebp(businessLogo);
                bool fileExists = await _businessLogoRepository.FileExists(hash);
                if (!fileExists)
                {
                    await _businessLogoRepository.PutFileAsByteData(hash + ".webp", webpImage, new Dictionary<string, string>());
                }

                updateDefinitions.Add(Builders<BusinessData>.Update.Set(x => x.LogoURL, hash));
            }

            if (updateDefinitions.Count == 0)
            {
                result.Code = 8;
                result.Message = "Nothing to update.";
                return result;
            }

            // If all is valid, update business and businesapp
            var updateBusinessResult = await _businessRepository.UpdateBusinessAsync(businessData.Id, Builders<BusinessData>.Update.Combine(updateDefinitions));
            if (!updateBusinessResult)
            {
                result.Code = 9;
                result.Message = "Failed to update business.";
                return result;
            }

            if (updateBusinessApp)
            {
                var updateBusinessAppResult = await _businessAppRepository.ReplaceBusinessAppAsync(businessApp);
                if (!updateBusinessAppResult)
                {
                    // revert back business data if app fails
                    await _businessRepository.ReplaceBusinessAsync(businessDataBackup);

                    result.Code = 10;
                    result.Message = "Failed to update business app.";
                    return result;
                }
            }  

            result.Success = true;
            return result;
        }

        public async Task<FunctionReturnResult<List<BusinessWhiteLabelDomain>?>> GetUserBusinessWhiteLabelDomainByIds(List<long> whitelabelDomainId, long businessId, string email)
        {
            var result = new FunctionReturnResult<List<BusinessWhiteLabelDomain>?>();

            List<BusinessWhiteLabelDomain>? businessWhiteLabelDomain = await _businessWhiteLabelDomainRepository.GetBusinessWhiteLabelDomainsAsync(whitelabelDomainId);
            if (businessWhiteLabelDomain == null)
            {
                result.Code = 1;
                Log.Logger.Error("[BusinessManager] Null - Business white label domains not found for user: " + email + " business id: " + businessId);
            }
            else
            {
                result.Success = true;
                result.Data = businessWhiteLabelDomain;
            }

            return result;
        }
    }
}
