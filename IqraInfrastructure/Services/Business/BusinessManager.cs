using IqraCore.Entities.Business;
using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
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
        private readonly BusinessDomainHostingRepository _businessDomainHostingRepository;

        public BusinessManager(BusinessRepository businessRepository, BusinessAppRepository businessAppRepository, BusinessLogoRepository businessLogoRepository, BusinessWhiteLabelDomainRepository businessWhiteLabelDomainRepository, BusinessDomainHostingRepository businessDomainHostingRepository)
        {
            _businessRepository = businessRepository;
            _businessAppRepository = businessAppRepository;
            _businessLogoRepository = businessLogoRepository;
            _businessWhiteLabelDomainRepository = businessWhiteLabelDomainRepository;
            _businessDomainHostingRepository = businessDomainHostingRepository;
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

            await _businessRepository.AddBusinessAsync(businessData);

            return businessData;
        }

        public async Task<FunctionReturnResult<List<BusinessData>>> GetUserBusinessesByEmail(string userEmail)
        {
            var result = new FunctionReturnResult<List<BusinessData>>();
            result.Data = new List<BusinessData>();

            var businesses = await _businessRepository.GetBusinessesByMasterUserEmailAsync(userEmail);
            if (businesses == null)
            {
                result.Code = "GetUserBusinessesByEmail:1";
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
                result.Code = "GetUserBusinessesByIds:1";
                result.Message = "Null - Businesses not found for user: " + userEmail;
                Log.Logger.Error("[BusinessManager] " + result.Message);
            }
            else if (businessesId.Count != getResult.Count)
            {
                result.Code = "GetUserBusinessesByIds:2";
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
                result.Code = "GetUserBusinessById:1";
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
                result.Code = "GetUserBusinessAppById:1";
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
                result.Code = "GetBusinesses:1";
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
                result.Code = "SearchBusinesses:1";
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
                    result.Code = "UpdateUserBusinessSettings:4";
                    result.Message = "The business logo file is too big. Maximum size is 5MB.";
                    return result;
                }
                else if (logoValidateResult == 1) {
                    result.Code = "UpdateUserBusinessSettings:3";
                    result.Message = "The business logo file is not valid.";
                    return result;
                }
                else if (logoValidateResult != 200)
                {
                    result.Code = "UpdateUserBusinessSettings:4";
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
                    result.Code = "UpdateUserBusinessSettings:5";
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
                    result.Code = "UpdateUserBusinessSettings:6";
                    result.Message = "Must have at least one language selected.";
                    result.Data = false;
                    return result;
                }

                var businessLanguagesUpdateResult = MultiLanguageHelper.UpdateObjectMultiLanguages(businessApp, businessLanguages, businessData.Languages);
                if (!businessLanguagesUpdateResult.Success)
                {
                    result.Code = businessLanguagesUpdateResult.Code;
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
                result.Code = "UpdateUserBusinessSettings:8";
                result.Message = "Nothing to update.";
                return result;
            }

            // If all is valid, update business and businesapp
            var updateBusinessResult = await _businessRepository.UpdateBusinessAsync(businessData.Id, Builders<BusinessData>.Update.Combine(updateDefinitions));
            if (!updateBusinessResult)
            {
                result.Code = "UpdateUserBusinessSettings:9";
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

                    result.Code = "UpdateUserBusinessSettings:10";
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
                result.Code = "GetUserBusinessWhiteLabelDomainByIds:1";
                Log.Logger.Error("[BusinessManager] Null - Business white label domains not found for user: " + email + " business id: " + businessId);
            }
            else
            {
                result.Success = true;
                result.Data = businessWhiteLabelDomain;
            }

            return result;
        }

        public async Task<FunctionReturnResult<BusinessWhiteLabelDomain?>> GetUserBusinessWhiteLabelDomain(long whitelabelDomainId, long businessId, string email)
        {
            var result = new FunctionReturnResult<BusinessWhiteLabelDomain?>();

            BusinessWhiteLabelDomain? businessWhiteLabelDomain = await _businessWhiteLabelDomainRepository.GetBusinessWhiteLabelDomainAsync(whitelabelDomainId);
            if (businessWhiteLabelDomain == null)
            {
                result.Code = "GetUserBusinessWhiteLabelDomain:1";
                Log.Logger.Error("[BusinessManager] Null - Business white label domains not found for user: " + email + " business id: " + businessId);
            }
            else
            {
                result.Success = true;
                result.Data = businessWhiteLabelDomain;
            }

            return result;
        }

        public async Task<FunctionReturnResult<BusinessWhiteLabelDomain?>> AddOrUpdateUserBusinessDomain(long businessId, IFormCollection formData, string postType, BusinessWhiteLabelDomain? domainData)
        {
            var result = new FunctionReturnResult<BusinessWhiteLabelDomain?>();

            string? changesData = formData["changes"];
            if (string.IsNullOrWhiteSpace(changesData))
            {
                result.Code = "AddOrUpdateUserBusinessDomain:1";
                result.Message = "Changes data not found.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesData);
            if (changes == null)
            {
                result.Code = "AddOrUpdateUserBusinessDomain:2";
                result.Message = "Changes data not found.";
                return result;
            }

            var changesRoot = changes.RootElement;

            string? domainType = changesRoot.GetProperty("type").GetString();
            if (string.IsNullOrWhiteSpace(domainType))
            {
                result.Code = "AddOrUpdateUserBusinessDomain:3";
                result.Message = "Domain type not found.";
                return result;
            }

            if (!Enum.TryParse(typeof(BusinessUserWhiteLabelDomainTypeEnum), domainType, out var domainTypeEnum))
            {
                result.Code = "AddOrUpdateUserBusinessDomain:4";
                result.Message = "Domain type not found.";
                return result;
            }

            BusinessWhiteLabelDomain? newDomainData = null;
            if (((BusinessUserWhiteLabelDomainTypeEnum)domainTypeEnum) == BusinessUserWhiteLabelDomainTypeEnum.IqraSubdomain)
            {
                newDomainData = new BusinessWhiteLabelIqraSubDomain();

                string? subdomainName = changesRoot.GetProperty("subDomain").GetString();
                if (string.IsNullOrWhiteSpace(subdomainName))
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:5";
                    result.Message = "Subdomain name not found.";
                    return result;
                }

                var checkSubdomainResult = await _businessDomainHostingRepository.CheckDomainsExistsInHosting(((BusinessUserWhiteLabelDomainTypeEnum)domainTypeEnum), subdomainName);
                if (!checkSubdomainResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:" + checkSubdomainResult.Code;
                    result.Message = checkSubdomainResult.Message;
                    return result;
                }

                ((BusinessWhiteLabelIqraSubDomain)newDomainData).SubDomain = subdomainName;
            }
            else if (((BusinessUserWhiteLabelDomainTypeEnum)domainTypeEnum) == BusinessUserWhiteLabelDomainTypeEnum.CustomDomain)
            {
                newDomainData = new BusinessWhiteLabelCustomDomain();

                string? customDomainName = changesRoot.GetProperty("customDomain").GetString();
                if (string.IsNullOrWhiteSpace(customDomainName))
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:6";
                    result.Message = "Custom domain name not found.";
                    return result;
                }

                var checkDomainNameResult = await _businessDomainHostingRepository.CheckDomainsExistsInHosting(((BusinessUserWhiteLabelDomainTypeEnum)domainTypeEnum), customDomainName);
                if (!checkDomainNameResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:" + checkDomainNameResult.Code;
                    result.Message = checkDomainNameResult.Message;
                    return result;
                }

                ((BusinessWhiteLabelCustomDomain)newDomainData).CustomDomain = customDomainName;

                string? isSSLEnabled = changesRoot.GetProperty("isSSLEnabled").GetString();
                if (!bool.TryParse(isSSLEnabled, out bool isSSLEnabledBoolean))
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:7";
                    result.Message = "SSL Enabled not found";
                    return result;
                }

                if (isSSLEnabledBoolean)
                {
                    ((BusinessWhiteLabelCustomDomain)newDomainData).SSLEnabled = DateTime.UtcNow;

                    string? useLetsEncryptSSL = changesRoot.GetProperty("useLetsEncryptSSL").GetString();
                    if (!bool.TryParse(useLetsEncryptSSL, out bool useLetsEncryptSSLBoolean))
                    {
                        result.Code = "AddOrUpdateUserBusinessDomain:8";
                        result.Message = "Use Lets Encrypt SSL not found";
                        return result;
                    }

                    if (useLetsEncryptSSLBoolean)
                    {
                        ((BusinessWhiteLabelCustomDomain)newDomainData).UseLetsEncryptSSL = DateTime.UtcNow;
                    }
                    else
                    {
                        string? sslCertificate = changesRoot.GetProperty("sslCertificate").GetString();
                        if (string.IsNullOrWhiteSpace(sslCertificate))
                        {
                            result.Code = "AddOrUpdateUserBusinessDomain:9";
                            result.Message = "Ssl Certificate not found";
                            return result;
                        }

                        string? sslPrivateKey = changesRoot.GetProperty("sslPrivateKey").GetString();
                        if (string.IsNullOrWhiteSpace(sslPrivateKey))
                        {
                            result.Code = "AddOrUpdateUserBusinessDomain:10";
                            result.Message = "Ssl Private Key not found";
                            return result;
                        }

                        ((BusinessWhiteLabelCustomDomain)newDomainData).SSLCertificate = sslCertificate;
                        ((BusinessWhiteLabelCustomDomain)newDomainData).SSLPrivateKey = sslPrivateKey;
                    }
                }
            }
            else
            {
                result.Code = "AddOrUpdateUserBusinessDomain:11";
                result.Message = "Domain type not found.";
                return result;
            }

            newDomainData.Type = ((BusinessUserWhiteLabelDomainTypeEnum)domainTypeEnum);

            if (postType == "new")
            {
                newDomainData.Id = await _businessWhiteLabelDomainRepository.GetNextBusinessWhiteLabelDomainId();
                newDomainData.BusinessId = businessId;

                await _businessWhiteLabelDomainRepository.AddBusinessWhiteLabelDomainAsync(newDomainData);

                var addResult = await _businessDomainHostingRepository.AddDomainToHosting(newDomainData);
                if (!addResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessDomain" + addResult.Code;
                    result.Message = addResult.Message;
                    return result;
                }
            }

            if (postType == "edit")
            {
                newDomainData.Id = domainData.Id;
                newDomainData.BusinessId = businessId;

                var updateResult = await _businessWhiteLabelDomainRepository.UpdateBusinessWhiteLabelDomainAsync(newDomainData);
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:12";
                    result.Message = "Domain data update failed.";
                    return result;
                }

                var updateHostingResult = await _businessDomainHostingRepository.UpdateDomainToHosting(domainData, newDomainData);
                if (!updateHostingResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessDomain" + updateHostingResult.Code;
                    result.Message = updateHostingResult.Message;
                    return result;
                }
            }

            result.Success = true;
            result.Data = newDomainData;

            return result;
        }
    }
}
