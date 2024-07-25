using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
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

        public async Task<BusinessData> AddBusiness(BusinessData businessData, IFormFile businessLogoFile)
        {
            long bussinessId = await _businessRepository.GetNextBusinessId();

            var (webpImage, hash) = await ImageHelper.ConvertScaleAndHashToWebp(businessLogoFile);
            bool fileExists = await _businessLogoRepository.FileExists(hash);
            if (!fileExists)
            {
                await _businessLogoRepository.PutFileAsByteData(hash + ".webp", webpImage, new Dictionary<string, string>());
            }

            businessData.Id = bussinessId;
            businessData.LogoURL = hash;

            await _businessRepository.AddBusinessAsync(businessData);
            await _businessAppRepository.AddBusinessAppAsync(
                new BusinessApp()
                {
                    Id = bussinessId,
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

        public async Task<FunctionReturnResult<bool?>> UpdateUserBusinessSettings(long businessId, IFormCollection formData)
        {
            var result = new FunctionReturnResult<bool?>();

            BusinessData? businessData = await _businessRepository.GetBusinessAsync(businessId);
            BusinessApp? businessApp = await _businessAppRepository.GetBusinessAppAsync(businessId);

            UpdateDefinition<BusinessData>? BusinessUpdateDefinition = null;

            // General
            string? businessName = formData["general.name"];
            if (string.IsNullOrWhiteSpace(businessName))
            {
                result.Code = 1;
                result.Message = "The business name cannot be empty.";
                return result;
            }
            BusinessUpdateDefinition = Builders<BusinessData>.Update.Set(x => x.Name, businessName);

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
            List<string?>? businessLanguages = formData["languages"].ToList();   
            if (businessLanguages == null || businessLanguages.Count == 0)
            {
                result.Code = 5;
                result.Message = "The business must have atleast one language added.";
                return result;
            }
            // todo validate languages - create a language repo or list taht iqra allows somewhere
            BusinessUpdateDefinition = BusinessUpdateDefinition.Set(d => d.Languages, businessLanguages);

            var businessLanguagesUpdateResult = MultiLanguageHelper.UpdateObjectMultiLanguages(businessApp, businessLanguages, businessData.Languages);
            if (!businessLanguagesUpdateResult.Success)
            {
                result.Code = 100 + businessLanguagesUpdateResult.Code;
                result.Message = businessLanguagesUpdateResult.Message;
                return result;
            }

            // unpublish the business, calls etc if languages are different than saved ones

            // Subusers
            List<string?>? businessRemovedSubusers = formData["subusers.removed"].ToList();
            if (businessRemovedSubusers != null && businessRemovedSubusers.Count > 0)
            {
                foreach (string? subuser in businessRemovedSubusers)
                {
                    var removeSubuserFilter = Builders<BusinessUser>.Filter.Eq(x => x.Email, subuser);
                    BusinessUpdateDefinition = BusinessUpdateDefinition.PullFilter(x => x.SubUsers, removeSubuserFilter);
                }
            }

            string? businessEditedSubusersJsonString = formData["subusers.edited"];
            if (!string.IsNullOrWhiteSpace(businessEditedSubusersJsonString))
            {
                List<BusinessUser>? businessEditedSubusers = null;
                try
                {
                    businessEditedSubusers = JsonSerializer.Deserialize<List<BusinessUser>?>(businessEditedSubusersJsonString);

                    if (businessEditedSubusers != null && businessEditedSubusers.Count > 0)
                    {
                        foreach (BusinessUser subuser in businessEditedSubusers)
                        {
                            var editSubuserFilter = Builders<BusinessData>.Filter.ElemMatch(x => x.SubUsers, su => su.Email == subuser.Email);
                            var update = Builders<BusinessData>.Update.Set(x => x.SubUsers.FirstMatchingElement(), subuser);
                        }
                    }
                }
                catch
                {
                    result.Code = 6;
                    result.Message = "The edited subusers data is not valid.";
                    return result;
                }
            }

            string? businessAddedSubusersJsonString = formData["subusers.added"];
            if (!string.IsNullOrWhiteSpace(businessAddedSubusersJsonString))
            {
                List<BusinessUser>? businessAddedSubusers = null;
                try
                {
                    businessAddedSubusers = JsonSerializer.Deserialize<List<BusinessUser>?>(businessAddedSubusersJsonString);

                    if (businessAddedSubusers != null && businessAddedSubusers.Count > 0)
                    {
                        foreach (BusinessUser subuser in businessAddedSubusers)
                        {
                            // Remove if exists first
                            // todo confirm if this works in order
                            var removeSubuserFilter = Builders<BusinessUser>.Filter.Eq(x => x.Email, subuser.Email);
                            BusinessUpdateDefinition = BusinessUpdateDefinition.PullFilter(x => x.SubUsers, removeSubuserFilter);
                            // add new
                            BusinessUpdateDefinition = BusinessUpdateDefinition.Push(x => x.SubUsers, subuser);
                        }
                    }
                }
                catch
                {
                    result.Code = 7;
                    result.Message = "The added subusers data is not valid.";
                    return result;
                }
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

                BusinessUpdateDefinition = BusinessUpdateDefinition
                    .Set(x => x.LogoURL, hash);
            }

            // If all is valid, update
            var updateResult = await _businessRepository.UpdateBusinessAsync(businessData.Id, BusinessUpdateDefinition);
            if (!updateResult)
            {
                result.Code = 8;
                result.Message = "Failed to update business.";
                return result;
            }

            return result;
        }
    }
}
