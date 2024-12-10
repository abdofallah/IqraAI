using IqraCore.Entities.Business;
using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Helper;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraCore.Utilities.Audio;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Serilog;
using System.Net;
using System.Text.Json;

namespace IqraInfrastructure.Services.Business
{
    public class BusinessManager
    {
        private readonly BusinessRepository _businessRepository;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessLogoRepository _businessLogoRepository;
        private readonly BusinessWhiteLabelDomainRepository _businessWhiteLabelDomainRepository;
        private readonly BusinessDomainVestaCPRepository _businessIqraBusinessDomainsVestaCPRepository;
        private readonly BusinessToolAudioRepository _businessToolAudioRepository;

        private readonly AudioFileProcessor _audioProcessor;

        public BusinessManager(
            BusinessRepository businessRepository,
            BusinessAppRepository businessAppRepository,
            BusinessLogoRepository businessLogoRepository,
            BusinessWhiteLabelDomainRepository businessWhiteLabelDomainRepository,
            BusinessDomainVestaCPRepository businessIqraBusinessDomainsVestaCPRepository,
            BusinessToolAudioRepository businessToolAudioRepository
        )
        {
            _businessRepository = businessRepository;
            _businessAppRepository = businessAppRepository;
            _businessLogoRepository = businessLogoRepository;
            _businessWhiteLabelDomainRepository = businessWhiteLabelDomainRepository;
            _businessIqraBusinessDomainsVestaCPRepository = businessIqraBusinessDomainsVestaCPRepository;
            _businessToolAudioRepository = businessToolAudioRepository;

            _audioProcessor = new AudioFileProcessor();
        }

        public async Task<FunctionReturnResult<BusinessData?>> AddBusiness(BusinessData businessData, IFormFile? businessLogoFile)
        {
            var result = new FunctionReturnResult<BusinessData?>();

            long businessId = await _businessRepository.GetNextBusinessId();
            businessData.Id = businessId;

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
                Id = businessId,
            };

            string subDomainHash = SubdomainHashGenerator.GenerateSubdomainHash(businessId);

            var addDefaultDomainResult = await AddOrUpdateUserBusinessDomain(
                businessId,
                new FormCollection(
                    new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()
                    {
                        {
                            "changes",
                            JsonSerializer.Serialize(new
                                {
                                    type = ((int)BusinessUserWhiteLabelDomainTypeEnum.IqraSubdomain).ToString(),
                                    subDomain =  subDomainHash
                                }
                            )
                        }
                    }
                ),
                "new",
                null
            );
            if (!addDefaultDomainResult.Success)
            {
                result.Code = "AddBusiness:" + addDefaultDomainResult.Code;
                result.Message = addDefaultDomainResult.Message;
                return result;
            }

            long businessWhiteLabelId = addDefaultDomainResult.Data.Id;
            businessData.WhiteLabelDomainIds.Add(businessWhiteLabelId);
            
            await _businessAppRepository.AddBusinessAppAsync(businessApp);
            await _businessRepository.AddBusinessAsync(businessData);

            result.Success = true;
            result.Data = businessData;

            return result;
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

        public async Task<FunctionReturnResult<bool?>> AddBusinessWhiteLabelDomainId(long whitelabelDomainId, long businessId)
        {
            var result = new FunctionReturnResult<bool?>();

            var update = Builders<BusinessData>.Update.Push(x => x.WhiteLabelDomainIds, whitelabelDomainId);

            var updateResult = await _businessRepository.UpdateBusinessAsync(businessId, update);
            if (!updateResult)
            {
                result.Code = "AddBusinessWhiteLabelDomainId:1";
                result.Message = "Failed to update business.";
            }

            result.Success = true;
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

                var checkSubdomainResult = await _businessIqraBusinessDomainsVestaCPRepository.GetIqraBusinessSubDomainDetails(subdomainName);
                if (!checkSubdomainResult.Success || string.IsNullOrEmpty(checkSubdomainResult.Data))
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:" + checkSubdomainResult.Code;
                    result.Message = checkSubdomainResult.Message;
                    return result;
                }

                if (!checkSubdomainResult.Data.Contains("Error: web domain " + (subdomainName + "." + _businessIqraBusinessDomainsVestaCPRepository.GetBusinessDomain()) + " doesn't exist"))
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:6";
                    result.Message = "Subdomain already exists.";
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
                    result.Code = "AddOrUpdateUserBusinessDomain:7";
                    result.Message = "Custom domain name not found.";
                    return result;
                }

                var checkDomainNameResult = await _businessIqraBusinessDomainsVestaCPRepository.GetCustomBusinessDomainDetails(customDomainName);
                if (!checkDomainNameResult.Success || string.IsNullOrEmpty(checkDomainNameResult.Data))
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:" + checkDomainNameResult.Code;
                    result.Message = checkDomainNameResult.Message;
                    return result;
                }

                if (!checkDomainNameResult.Data.Contains("Error: web domain " + customDomainName + " doesn't exist"))
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:8";
                    result.Message = "Subdomain already exists.";
                    return result;
                }

                ((BusinessWhiteLabelCustomDomain)newDomainData).CustomDomain = customDomainName;

                string? useCustomSSL = changesRoot.GetProperty("useCustomSSL").ToString();
                if (!bool.TryParse(useCustomSSL, out bool useCustomSSLBoolean))
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:9";
                    result.Message = "Use Custom SSL not found";
                    return result;
                }

                if (useCustomSSLBoolean)
                {
                    ((BusinessWhiteLabelCustomDomain)newDomainData).UseCustomSSL = DateTime.UtcNow;

                    string? sslCertificate = changesRoot.GetProperty("sslCertificate").GetString();
                    if (string.IsNullOrWhiteSpace(sslCertificate))
                    {
                        result.Code = "AddOrUpdateUserBusinessDomain:10";
                        result.Message = "Ssl Certificate not found";
                        return result;
                    }

                    string? sslPrivateKey = changesRoot.GetProperty("sslPrivateKey").GetString();
                    if (string.IsNullOrWhiteSpace(sslPrivateKey))
                    {
                        result.Code = "AddOrUpdateUserBusinessDomain:11";
                        result.Message = "Ssl Private Key not found";
                        return result;
                    }

                    ((BusinessWhiteLabelCustomDomain)newDomainData).SSLCertificate = sslCertificate;
                    ((BusinessWhiteLabelCustomDomain)newDomainData).SSLPrivateKey = sslPrivateKey;
                }
            }
            else
            {
                result.Code = "AddOrUpdateUserBusinessDomain:12";
                result.Message = "Domain type not found.";
                return result;
            }

            newDomainData.Type = ((BusinessUserWhiteLabelDomainTypeEnum)domainTypeEnum);

            if (postType == "new")
            {    
                if (((BusinessUserWhiteLabelDomainTypeEnum)domainTypeEnum) == BusinessUserWhiteLabelDomainTypeEnum.IqraSubdomain)
                {
                    BusinessWhiteLabelIqraSubDomain currentIqraSubdomainData = (BusinessWhiteLabelIqraSubDomain)newDomainData;

                    var addDNSResult = await _businessIqraBusinessDomainsVestaCPRepository.AddIqraBusinessSubDomainDNSRecord(currentIqraSubdomainData.SubDomain, true);
                    if (!addDNSResult.Success)
                    {
                        result.Code = "AddOrUpdateUserBusinessDomain:" + addDNSResult.Code;
                        result.Message = addDNSResult.Message;
                        return result;
                    }

                    var addDomainResult = await _businessIqraBusinessDomainsVestaCPRepository.AddIqraBusinessSubDomain(currentIqraSubdomainData.SubDomain, true);
                    if (!addDomainResult.Success)
                    {
                        result.Code = "AddOrUpdateUserBusinessDomain:" + addDomainResult.Code;
                        result.Message = addDomainResult.Message;
                        return result;
                    }

                    var addSSLResult = await _businessIqraBusinessDomainsVestaCPRepository.AddIqraBusinessSubDomainLetsEncryptSSL(currentIqraSubdomainData.SubDomain);
                    if (!addSSLResult.Success) {
                        result.Code = "AddOrUpdateUserBusinessDomain:" + addSSLResult.Code;
                        result.Message = addSSLResult.Message;
                        return result;
                    }

                    var setHTTPSTemplateResult = await _businessIqraBusinessDomainsVestaCPRepository.SetIqraSubDomainDefaultProxyTemplate(currentIqraSubdomainData.SubDomain, true);
                    if (!setHTTPSTemplateResult.Success)
                    {
                        result.Code = "AddOrUpdateUserBusinessDomain:" + setHTTPSTemplateResult.Code;
                        result.Message = setHTTPSTemplateResult.Message;
                        return result;
                    }
                }
                else if (((BusinessUserWhiteLabelDomainTypeEnum)domainTypeEnum) == BusinessUserWhiteLabelDomainTypeEnum.CustomDomain)
                {
                    BusinessWhiteLabelCustomDomain currentCustomDomainData = (BusinessWhiteLabelCustomDomain)newDomainData;

                    try
                    {
                        var customDomainDNS = Dns.GetHostAddresses(currentCustomDomainData.CustomDomain);
                        if (customDomainDNS == null || customDomainDNS.Length == 0)
                        {
                            result.Code = "AddOrUpdateUserBusinessDomain:13";
                            result.Message = "Domain IP addresses not found.";
                            return result;
                        }

                        var domainDNSIPExists = customDomainDNS.Where((d) =>
                        {
                            return d.ToString() == _businessIqraBusinessDomainsVestaCPRepository.GetBusinessDomainDefaultIP();
                        }).FirstOrDefault();

                        if (domainDNSIPExists == null)
                        {
                            result.Code = "AddOrUpdateUserBusinessDomain:14";
                            result.Message = "Domain IP address not pointing to Iqra Domain Server IP.";
                            return result;
                        }
                    }
                    catch (Exception ex) {
                        result.Code = "AddOrUpdateUserBusinessDomain:15";
                        result.Message = ex.Message;
                        return result;
                    }

                    var addResult = await _businessIqraBusinessDomainsVestaCPRepository.AddCustomBusinessDomain(currentCustomDomainData.CustomDomain, true);
                    if (!addResult.Success)
                    {
                        result.Code = "AddOrUpdateUserBusinessDomain:" + addResult.Code;
                        result.Message = addResult.Message;
                        return result;
                    }

                    if (currentCustomDomainData.UseCustomSSL != null)
                    {
                        var addSSLResult = await _businessIqraBusinessDomainsVestaCPRepository.AddCustomBusinessDomainSSL(currentCustomDomainData.CustomDomain, currentCustomDomainData.SSLCertificate, currentCustomDomainData.SSLPrivateKey, true);
                        if (!addSSLResult.Success)
                        {
                            result.Code = "AddOrUpdateUserBusinessDomain:" + addSSLResult.Code;
                            result.Message = addSSLResult.Message;
                            return result;
                        }
                    }
                    else
                    {
                        var addSSLResult = await _businessIqraBusinessDomainsVestaCPRepository.AddCustomBusinessDomainLetsEncryptSSL(currentCustomDomainData.CustomDomain);
                        if (!addSSLResult.Success)
                        {
                            result.Code = "AddOrUpdateUserBusinessDomain:" + addSSLResult.Code;
                            result.Message = addSSLResult.Message;
                            return result;
                        }                    
                    }

                    var setHTTPTemplateResult = await _businessIqraBusinessDomainsVestaCPRepository.SetCustomDomainDefaultProxyTemplate(currentCustomDomainData.CustomDomain, true);
                    if (!setHTTPTemplateResult.Success)
                    {
                        result.Code = "AddOrUpdateUserBusinessDomain:" + setHTTPTemplateResult.Code;
                        result.Message = setHTTPTemplateResult.Message;
                        return result;
                    }
                }

                newDomainData.Id = await _businessWhiteLabelDomainRepository.GetNextBusinessWhiteLabelDomainId();
                newDomainData.BusinessId = businessId;

                await _businessWhiteLabelDomainRepository.AddBusinessWhiteLabelDomainAsync(newDomainData);
                await AddBusinessWhiteLabelDomainId(newDomainData.Id, businessId);
            }

            if (postType == "edit")
            {
                newDomainData.Id = domainData.Id;
                newDomainData.BusinessId = businessId;

                var updateResult = await _businessWhiteLabelDomainRepository.UpdateBusinessWhiteLabelDomainAsync(newDomainData);
                if (!updateResult)
                {
                    result.Code = "AddOrUpdateUserBusinessDomain:16";
                    result.Message = "Domain data update failed.";
                    return result;
                }

                throw new NotImplementedException("Domain postType edit functionality not implemented yet.");
            }

            result.Success = true;
            result.Data = newDomainData;

            return result;
        }

        public async Task<FunctionReturnResult<BusinessUser?>> AddOrUpdateUserBusinessSubUser(long businessId, IFormCollection formData, string postType, List<long> businesDatasWhiteLabelDomainIds, BusinessUser? editBusinessUserData)
        {
            var result = new FunctionReturnResult<BusinessUser?>();

            string? generalTabChangesString = formData["general"];
            if (string.IsNullOrWhiteSpace(generalTabChangesString))
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:1";
                result.Message = "Changes data not found.";
                return result;
            }

            JsonDocument? generalTabChanges = JsonDocument.Parse(generalTabChangesString);
            if (generalTabChanges == null)
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:2";
                result.Message = "Changes data not found.";
                return result;
            }

            string? whiteLabelTabChangesString = formData["whiteLabel"];
            if (string.IsNullOrWhiteSpace(whiteLabelTabChangesString))
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:3";
                result.Message = "Changes data not found.";
                return result;
            }

            JsonDocument? whiteLabelTabChanges = JsonDocument.Parse(whiteLabelTabChangesString);
            if (whiteLabelTabChanges == null)
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:4";
                result.Message = "Changes data not found.";
                return result;
            }

            string? permissionTabChangesString = formData["permissions"];
            if (string.IsNullOrWhiteSpace(permissionTabChangesString))
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:5";
                result.Message = "Changes data not found.";
                return result;
            }

            JsonDocument? permissionTabChanges = JsonDocument.Parse(permissionTabChangesString);
            if (permissionTabChanges == null)
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:6";
                result.Message = "Changes data not found.";
                return result;
            }

            IFormFile? subuserWhiteLabelLogo = formData.Files.FirstOrDefault(x => x.Name == "whiteLabel.logo");
            if (subuserWhiteLabelLogo != null)
            {
                int logoValidateResult = ImageHelper.ValidateBusinessWhiteLabelLogoFile(subuserWhiteLabelLogo);

                if (logoValidateResult == 0)
                {
                    result.Code = "AddOrUpdateUserBusinessSubUser:7";
                    result.Message = "The whitelabel style logo file is too big. Maximum size is 5MB.";
                    return result;
                }
                else if (logoValidateResult == 1)
                {
                    result.Code = "AddOrUpdateUserBusinessSubUser:8";
                    result.Message = "The whitelabel style logo file is not valid.";
                    return result;
                }
                else if (logoValidateResult != 200)
                {
                    result.Code = "AddOrUpdateUserBusinessSubUser:9";
                    result.Message = "The whitelabel style logo file is not valid.";
                    return result;
                }
            }

            IFormFile? subuserWhiteLabelFavicon = formData.Files.FirstOrDefault(x => x.Name == "whiteLabel.favicon");
            if (subuserWhiteLabelFavicon != null)
            {
                int faviconValidateResult = ImageHelper.ValidateBusinessWhiteLabelFaviconFile(subuserWhiteLabelFavicon);

                if (faviconValidateResult == 0)
                {
                    result.Code = "AddOrUpdateUserBusinessSubUser:10";
                    result.Message = "The whitelabel style favicon file is too big. Maximum size is 5MB.";
                    return result;
                }
                else if (faviconValidateResult == 1)
                {
                    result.Code = "AddOrUpdateUserBusinessSubUser:11";
                    result.Message = "The whitelabel style favicon file is not valid.";
                    return result;
                }
                else if (faviconValidateResult != 200)
                {
                    result.Code = "AddOrUpdateUserBusinessSubUser:12";
                    result.Message = "The whitelabel style favicon file is not valid.";
                    return result;
                }
            }

            BusinessUser newSubUserData = new BusinessUser();

            // General Tab
            var generalTabRootElement = generalTabChanges.RootElement;

            string? subUserEmail = generalTabRootElement.GetProperty("email").GetString();
            if (string.IsNullOrWhiteSpace(subUserEmail) || !EmailAddressValidationHelper.IsValid(subUserEmail))
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:13";
                result.Message = "Subuser email not found or is invalid.";
                return result;
            }
            newSubUserData.Email = subUserEmail;

            string? subUserPassword = generalTabRootElement.GetProperty("password").GetString();
            if (string.IsNullOrWhiteSpace(subUserPassword) || subUserPassword.Length <= 7)
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:14";
                result.Message = "Subuser password not found or is not 8 characters long.";
                return result;
            }
            newSubUserData.Password = subUserPassword;

            bool? subUserLoginDisabled = generalTabRootElement.GetProperty("isLoginDisabled").GetBoolean();
            if (subUserLoginDisabled == null)
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:15";
                result.Message = "Subuser login disabled not found.";
                return result;
            }
            
            string? subUserLoginDisabledReason = null;
            if (subUserLoginDisabled.Value == true)
            {
                if (postType == "new")
                {
                    newSubUserData.DisabledUserLoginAt = DateTime.UtcNow;
                }
                else if (postType == "edit")
                {
                    if (editBusinessUserData.DisabledUserLoginAt != null)
                    {
                        newSubUserData.DisabledUserLoginAt = editBusinessUserData.DisabledUserLoginAt;
                    } 
                }

                subUserLoginDisabledReason = generalTabRootElement.GetProperty("loginDisabledReason").GetString();
                if (!string.IsNullOrWhiteSpace(subUserLoginDisabledReason))
                {
                    newSubUserData.DisabledUserLoginReason = subUserLoginDisabledReason;
                }
            }

            // WhiteLabel Tab
            var whiteLabelTabRootElement = whiteLabelTabChanges.RootElement;

            // WhiteLabel General Tab
            var whiteLabelTabGeneralTabRootElement = whiteLabelTabChanges.RootElement.GetProperty("general");

            string? subUserWhiteLabelPlatformName = whiteLabelTabGeneralTabRootElement.GetProperty("platformName").GetString();
            if (string.IsNullOrWhiteSpace(subUserWhiteLabelPlatformName))
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:16";
                result.Message = "Subuser whitelabel platform name not found.";
                return result;
            }
            newSubUserData.WhiteLabel.PlatformName = subUserWhiteLabelPlatformName;

            string? subUserWhiteLabelPlatformTitle = whiteLabelTabGeneralTabRootElement.GetProperty("platformTitle").GetString();
            if (string.IsNullOrWhiteSpace(subUserWhiteLabelPlatformTitle))
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:17";
                result.Message = "Subuser whitelabel platform title not found.";
                return result;
            }
            newSubUserData.WhiteLabel.PlatformTitle = subUserWhiteLabelPlatformTitle;

            string? subUserWhiteLabelPlatformDescription = whiteLabelTabGeneralTabRootElement.GetProperty("platformDescription").GetString();
            if (string.IsNullOrWhiteSpace(subUserWhiteLabelPlatformDescription))
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:18";
                result.Message = "Subuser whitelabel platform description not found.";
                return result;
            }
            newSubUserData.WhiteLabel.PlatformDescription = subUserWhiteLabelPlatformDescription;

            string? subUserWhiteLabelDomainId = whiteLabelTabGeneralTabRootElement.GetProperty("domainId").GetString();
            if (string.IsNullOrWhiteSpace(subUserWhiteLabelDomainId))
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:19";
                result.Message = "Subuser whitelabel domain id not found.";
                return result;
            }
            
            if (long.TryParse(subUserWhiteLabelDomainId, out long whiteLabelDomainId) == false)
            {
                result.Code = "AddOrUpdateUserBusinessDomain:20";
                result.Message = "Subuser whitelabel domain id is not a number.";
                return result;
            }

            if (!businesDatasWhiteLabelDomainIds.Contains(whiteLabelDomainId))
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:21";
                result.Message = "Subuser whitelabel domain id is not valid.";
                return result;
            }
            newSubUserData.WhiteLabel.DomainId = whiteLabelDomainId;

            // WhiteLabel Styles
            var whiteLabelTabStylesTabRootElement = whiteLabelTabChanges.RootElement.GetProperty("styles");

            string? subUserWhiteLabelStyleCustomCSS = whiteLabelTabStylesTabRootElement.GetProperty("customCSS").GetString();
            if (!string.IsNullOrWhiteSpace(subUserWhiteLabelStyleCustomCSS))
            {
                // todo validate css
                newSubUserData.WhiteLabel.CustomCSS = subUserWhiteLabelStyleCustomCSS;
            }
            
            string? whiteLabelStyleCustomJavaScript = whiteLabelTabStylesTabRootElement.GetProperty("customJavaScript").GetString();
            if (!string.IsNullOrWhiteSpace(whiteLabelStyleCustomJavaScript))
            {
                // todo validate js
                newSubUserData.WhiteLabel.CustomJavaScript = whiteLabelStyleCustomJavaScript;
            }

            // WhiteLabel Permissions
            var whiteLabelTabPermissionsTabRootElement = permissionTabChanges.RootElement;

            var validateAndPopulateBusinessSubUserPermissionsResult = await ValidateAndPopulateBusinessSubUserPermissions(whiteLabelTabPermissionsTabRootElement);
            if (validateAndPopulateBusinessSubUserPermissionsResult.Code != null)
            {
                result.Code = "AddOrUpdateUserBusinessSubUser:" + validateAndPopulateBusinessSubUserPermissionsResult.Code;
                result.Message = validateAndPopulateBusinessSubUserPermissionsResult.Message;
                return result;
            }
            newSubUserData.Permission = validateAndPopulateBusinessSubUserPermissionsResult.Data;

            // WhiteLabel Logo
            if (subuserWhiteLabelLogo != null)
            {
                var (webpImage, hash) = await ImageHelper.ConvertScaleAndHashToWebp(subuserWhiteLabelLogo);
                bool fileExists = await _businessLogoRepository.FileExists(hash);
                if (!fileExists)
                {
                    await _businessLogoRepository.PutFileAsByteData(hash + ".webp", webpImage, new Dictionary<string, string>());
                }

                newSubUserData.WhiteLabel.LogoURL = hash;
            }

            // WhiteLabel Favicon
            if (subuserWhiteLabelFavicon != null)
            {
                var (webpImage, hash) = await ImageHelper.ConvertScaleAndHashToWebp(subuserWhiteLabelFavicon);
                bool fileExists = await _businessLogoRepository.FileExists(hash);
                if (!fileExists)
                {
                    await _businessLogoRepository.PutFileAsByteData(hash + ".webp", webpImage, new Dictionary<string, string>());
                }

                newSubUserData.WhiteLabel.FaviconIconURL = hash;
            }

            // Update Add
            if (postType == "new")
            {
                var addResult = await _businessRepository.AddBusinessSubUserAsync(businessId, newSubUserData);
                if (!addResult)
                {
                    result.Code = "AddOrUpdateUserBusinessSubUser:22";
                    result.Message = "Failed to add subuser.";
                    return result;
                }
            }

            if (postType == "edit")
            {
                var replaceResult = await _businessRepository.ReplaceBusinessSubUserAsync(businessId, newSubUserData);
                if (!replaceResult)
                {
                    result.Code = "AddOrUpdateUserBusinessSubUser:23";
                    result.Message = "Failed to replace subuser.";
                    return result;
                }
            }

            // Return Success
            result.Success = true;
            result.Data = newSubUserData;
            return result;
        }

        private async Task<FunctionReturnResult<BusinessUserPermission?>> ValidateAndPopulateBusinessSubUserPermissions (JsonElement whiteLabelTabPermissionsTabRootElement)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var result = new FunctionReturnResult<BusinessUserPermission?>();

            BusinessUserPermission newBusinessSubUserPermissions = new BusinessUserPermission();

            // SubUser Routings Permissions
            var subUserRoutingsPermissions = JsonSerializer.Deserialize<BusinessUserPermissionRouting>(whiteLabelTabPermissionsTabRootElement.GetProperty("routing").GetRawText(), options);
            if (subUserRoutingsPermissions == null)
            {
                result.Code = "ValidateAndPopulateBusinessSubUserPermissions:1";
                result.Message = "Subuser routings permissions not found.";
                return result;
            }

            if (!subUserRoutingsPermissions.TabEnabled)
            {
                subUserRoutingsPermissions = new BusinessUserPermissionRouting();
            }
            
            newBusinessSubUserPermissions.Routing = subUserRoutingsPermissions;

            // SubUser Tools Permissions
            var subUserToolsPermissions = JsonSerializer.Deserialize<BusinessUserPermissionTools>(whiteLabelTabPermissionsTabRootElement.GetProperty("tools").GetRawText(), options);
            if (subUserToolsPermissions == null)
            {
                result.Code = "ValidateAndPopulateBusinessSubUserPermissions:2";
                result.Message = "Subuser tools permissions not found.";
                return result;
            }

            if (!subUserToolsPermissions.TabEnabled)
            {
                subUserToolsPermissions = new BusinessUserPermissionTools();
            }

            newBusinessSubUserPermissions.Tools = subUserToolsPermissions;

            // SubUser Agents Permissions
            var subUserAgentsPermissions = JsonSerializer.Deserialize<BusinessUserPermissionAgents>(whiteLabelTabPermissionsTabRootElement.GetProperty("agents").GetRawText(), options);
            if (subUserAgentsPermissions == null)
            {
                result.Code = "ValidateAndPopulateBusinessSubUserPermissions:3";
                result.Message = "Subuser agents permissions not found.";
                return result;
            }

            if (!subUserAgentsPermissions.TabEnabled)
            {
                subUserAgentsPermissions = new BusinessUserPermissionAgents();
            }

            newBusinessSubUserPermissions.Agents = subUserAgentsPermissions;

            // SubUser Context Permissions
            var subUserContextPermissions = JsonSerializer.Deserialize<BusinessUserPermissionContext>(whiteLabelTabPermissionsTabRootElement.GetProperty("context").GetRawText(), options);
            if (subUserContextPermissions == null)
            {
                result.Code = "ValidateAndPopulateBusinessSubUserPermissions:4";
                result.Message = "Subuser context permissions not found.";
                return result;
            }

            if (subUserContextPermissions.TabEnabled)
            {
                // SubUser Context Branding Permissions
                if (!subUserContextPermissions.Branding.TabEnabled)
                {
                    subUserContextPermissions.Branding.Edit = false;
                }

                // SubUser Context Branches Permissions
                if (!subUserContextPermissions.Branches.TabEnabled)
                {
                    subUserContextPermissions.Branches.Add = false;
                    subUserContextPermissions.Branches.Edit = false;
                    subUserContextPermissions.Branches.Delete = false;
                }

                // SubUser Context Services Permissions
                if (!subUserContextPermissions.Services.TabEnabled)
                {
                    subUserContextPermissions.Services.Add = false;
                    subUserContextPermissions.Services.Edit = false;
                    subUserContextPermissions.Services.Delete = false;
                }

                // SubUser Context Products Permissions
                if (!subUserContextPermissions.Products.TabEnabled)
                {
                    subUserContextPermissions.Products.Add = false;
                    subUserContextPermissions.Products.Edit = false;
                    subUserContextPermissions.Products.Delete = false;
                }
            }
            else
            {
                subUserContextPermissions = new BusinessUserPermissionContext();
            }

            newBusinessSubUserPermissions.Context = subUserContextPermissions;

            // SubUser Make Calls Tab
            var subUserMakeCallsPermissions = JsonSerializer.Deserialize<BusinessUserPermissionMakeCalls>(whiteLabelTabPermissionsTabRootElement.GetProperty("makeCalls").GetRawText(), options);
            if (subUserMakeCallsPermissions == null)
            {
                result.Code = "ValidateAndPopulateBusinessSubUserPermissions:5";
                result.Message = "Subuser make calls permissions not found.";
                return result;
            }

            if (!subUserMakeCallsPermissions.TabEnabled)
            {
                subUserMakeCallsPermissions = new BusinessUserPermissionMakeCalls();
            }

            newBusinessSubUserPermissions.MakeCalls = subUserMakeCallsPermissions;

            // SubUser Conversations Tab
            var subUserConversationsPermissions = JsonSerializer.Deserialize<BusinessUserPermissionConversations>(whiteLabelTabPermissionsTabRootElement.GetProperty("conversations").GetRawText(), options);
            if (subUserConversationsPermissions == null)
            {
                result.Code = "ValidateAndPopulateBusinessSubUserPermissions:6";
                result.Message = "Subuser conversations permissions not found.";
                return result;
            }

            if (subUserConversationsPermissions.TabEnabled)
            {
                if (!subUserConversationsPermissions.Inbound.TabEnabled)
                {
                    subUserConversationsPermissions.Inbound = new BusinessUserPermissionConversationsInboundCall();
                }

                if (!subUserConversationsPermissions.Outbound.TabEnabled)
                {
                    subUserConversationsPermissions.Outbound = new BusinessUserPermissionConversationsOutboundCall();
                }

                if (!subUserConversationsPermissions.Websocket.TabEnabled)
                {
                    subUserConversationsPermissions.Websocket = new BusinessUserPermissionConversationsWebsocket();
                }
            }
            else
            {
                subUserConversationsPermissions = new BusinessUserPermissionConversations();
            }

            newBusinessSubUserPermissions.Conversations = subUserConversationsPermissions;

            // SubUser Settings Tab
            var subUserSettingsPermissions = JsonSerializer.Deserialize<BusinessUserPermissionSettings>(whiteLabelTabPermissionsTabRootElement.GetProperty("settings").GetRawText(), options);
            if (subUserSettingsPermissions == null)
            {
                result.Code = "ValidateAndPopulateBusinessSubUserPermissions:7";
                result.Message = "Subuser settings permissions not found.";
                return result;
            }

            if (subUserSettingsPermissions.TabEnabled)
            {
                if (!subUserSettingsPermissions.General.TabEnabled)
                {
                    subUserSettingsPermissions.General = new BusinessUserPermissionSettingsGeneral();
                }

                if (!subUserSettingsPermissions.Languages.TabEnabled)
                {
                    subUserSettingsPermissions.Languages = new BusinessUserPermissionSettingsLanguages();
                }

                if (!subUserSettingsPermissions.Users.TabEnabled)
                {
                    subUserSettingsPermissions.Users = new BusinessUserPermissionSettingsUsers();
                }
            }
            else
            {
                subUserSettingsPermissions = new BusinessUserPermissionSettings();
            }

            newBusinessSubUserPermissions.Settings = subUserSettingsPermissions;

            // Return Result
            result.Success = true;
            result.Data = newBusinessSubUserPermissions;

            return result;
        }

        public async Task<bool> CheckBusinessToolExists(long businessId, string toolId) {
            var result = await _businessAppRepository.CheckBusinessAppToolExists(businessId, toolId);

            return result;
        }

        public async Task<FunctionReturnResult<BusinessAppTool?>> AddOrUpdateUserBusinessTools(long businessId, IFormCollection formData, string postType, string? exisitingToolId)
        {
            var result = new FunctionReturnResult<BusinessAppTool?>();

            List<string> businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateUserBusinessTools:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateUserBusinessTools:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var NewBusinessAppToolData = new BusinessAppTool();

            // General Tab
            if (!changes.RootElement.TryGetProperty("general", out var generalTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessTools:3";
                result.Message = "General tab not found.";
                return result;
            }
            else
            {
                var generalNameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                businessLanguages,
                generalTabRootElement,
                "name",
                NewBusinessAppToolData.General.Name
            );
                if (!generalNameValidationResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessTools:" + generalNameValidationResult.Code;
                    result.Message = generalNameValidationResult.Message;
                    return result;
                }

                var generalShortDescriptionValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    generalTabRootElement,
                    "shortDescription",
                    NewBusinessAppToolData.General.ShortDescription
                );
                if (!generalShortDescriptionValidationResult.Success)
                {
                    result.Code = "AddOrUpdateUserBusinessTools:" + generalShortDescriptionValidationResult.Code;
                    result.Message = generalShortDescriptionValidationResult.Message;
                    return result;
                }
            }
            
            // Configuration Tab
            if (!changes.RootElement.TryGetProperty("configuration", out var configurationTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessTools:4";
                result.Message = "Configuration tab not found.";
                return result;
            }
            else
            {
                if (!configurationTabRootElement.TryGetProperty("inputSchemea", out var configurationTabInputSchemeaProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:5";
                    result.Message = "Configuration tab input scheme property not found.";
                    return result;
                }
                foreach (var inputScheme in configurationTabInputSchemeaProperty.EnumerateArray())
                {
                    BusinessAppToolConfigurationInputSchemea newInputSchemeaData = new BusinessAppToolConfigurationInputSchemea();

                    var inputSchemeNameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                        businessLanguages,
                        inputScheme,
                        "name",
                        newInputSchemeaData.Name
                    );
                    if (!inputSchemeNameValidationResult.Success)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:" + inputSchemeNameValidationResult.Code;
                        result.Message = inputSchemeNameValidationResult.Message;
                        return result;
                    }

                    var inputSchemeDescriptionValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                        businessLanguages,
                        inputScheme,
                        "description",
                        newInputSchemeaData.Description
                    );
                    if (!inputSchemeDescriptionValidationResult.Success)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:" + inputSchemeDescriptionValidationResult.Code;
                        result.Message = inputSchemeDescriptionValidationResult.Message;
                        return result;
                    }

                    if (!inputScheme.TryGetProperty("type", out var inputSchemeTypeProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:6";
                        result.Message = "Configuration tab input scheme type property not found.";
                        return result;
                    }
                    if (!inputSchemeTypeProperty.TryGetInt32(out var inputSchemeTypeInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:7";
                        result.Message = "Invaldi configuration tab input scheme value.";
                        return result;
                    }
                    if (!Enum.IsDefined(typeof(BusinessAppToolConfigurationInputSchemeaTypeEnum), inputSchemeTypeInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:7";
                        result.Message = "Configuration tab input scheme type not found.";
                        return result;
                    }
                    newInputSchemeaData.Type = (BusinessAppToolConfigurationInputSchemeaTypeEnum)inputSchemeTypeInt;

                    if (!inputScheme.TryGetProperty("isArray", out var isArrayProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:8";
                        result.Message = "Configuration tab input scheme isArray property not found.";
                        return result;
                    }
                    newInputSchemeaData.IsArray = isArrayProperty.GetBoolean();

                    if (!inputScheme.TryGetProperty("isRequired", out var isRequiredProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:9";
                        result.Message = "Configuration tab input scheme isRequired property not found.";
                        return result;
                    }
                    newInputSchemeaData.IsRequired = isRequiredProperty.GetBoolean();

                    newInputSchemeaData.Id = Guid.NewGuid().ToString(); // todo make more name friendly

                    NewBusinessAppToolData.Configuration.InputSchemea.Add(newInputSchemeaData);
                }

                if (!configurationTabRootElement.TryGetProperty("requestType", out var configurationRequestTypeProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:10";
                    result.Message = "Configuration tab configuration request type property not found.";
                    return result;
                }
                if (!configurationRequestTypeProperty.TryGetInt32(out var configurationRequestTypeInt))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:11";
                    result.Message = "Invaldi configuration tab configuration request type value.";
                    return result;
                }
                if (!Enum.IsDefined(typeof(HttpMethodEnum), configurationRequestTypeInt))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:12";
                    result.Message = "Configuration tab configuration request type not found.";
                    return result;
                }
                NewBusinessAppToolData.Configuration.RequestType = (HttpMethodEnum)configurationRequestTypeInt;

                if (!configurationTabRootElement.TryGetProperty("endpoint", out var configurationEndpointUrlProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:13";
                    result.Message = "Configuration tab endpoint property not found.";
                    return result;
                }
                string? configurationEndpointUrl = configurationEndpointUrlProperty.GetString();
                if (string.IsNullOrWhiteSpace(configurationEndpointUrl))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:14";
                    result.Message = "Configuration tab endpoint is empty.";
                    return result;
                }
                NewBusinessAppToolData.Configuration.Endpoint = configurationEndpointUrl;

                if (!configurationTabRootElement.TryGetProperty("headers", out var configurationHeadersProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:15";
                    result.Message = "Configuration tab headers property not found.";
                    return result;
                }
                foreach (var header in configurationHeadersProperty.EnumerateObject())
                {
                    string? headerKey = header.Name;
                    string? headerValue = header.Value.GetString();
                    if (string.IsNullOrWhiteSpace(headerKey) || string.IsNullOrWhiteSpace(headerValue))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:16";
                        result.Message = "Configuration tab header key or value is empty.";
                        return result;
                    }

                    if (NewBusinessAppToolData.Configuration.Headers.ContainsKey(headerKey))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:17";
                        result.Message = $"Configuration tab header key {headerKey} already exists.";
                        return result;
                    }

                    NewBusinessAppToolData.Configuration.Headers.Add(headerKey, headerValue);
                }

                if (!configurationTabRootElement.TryGetProperty("bodyType", out var configurationBodyTypeProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:18";
                    result.Message = "Configuration tab body type property not found.";
                    return result;
                }
                if (!configurationBodyTypeProperty.TryGetInt32(out var configurationBodyTypeInt))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:19";
                    result.Message = "Invaldi configuration tab body type value.";
                    return result;
                }
                if (!Enum.IsDefined(typeof(HttpBodyEnum), configurationBodyTypeInt))
                {
                    result.Code = "AddOrUpdateUserBusinessTools:20";
                    result.Message = "Configuration tab body type not found.";
                    return result;
                }
                NewBusinessAppToolData.Configuration.BodyType = (HttpBodyEnum)configurationBodyTypeInt;

                switch (NewBusinessAppToolData.Configuration.BodyType)
                {
                    case HttpBodyEnum.None:
                        break;

                    case HttpBodyEnum.Raw:
                        if (!configurationTabRootElement.TryGetProperty("bodyData", out var configurationbodyRawDataProperty))
                        {
                            result.Code = "AddOrUpdateUserBusinessTools:22";
                            result.Message = "Configuration tab raw body data property not found.";
                            return result;
                        }

                        string? configurationbodyRawDataString = configurationbodyRawDataProperty.GetString();

                        if (string.IsNullOrWhiteSpace(configurationbodyRawDataString))
                        {
                            result.Code = "AddOrUpdateUserBusinessTools:23";
                            result.Message = "Configuration tab raw body data is empty.";
                            return result;
                        }

                        NewBusinessAppToolData.Configuration.BodyData = configurationbodyRawDataString;
                        break;

                    case HttpBodyEnum.FormData:
                    case HttpBodyEnum.XWWWFormUrlencoded:
                        if (!configurationTabRootElement.TryGetProperty("bodyData", out var configurationbodyFormDataProperty))
                        {
                            result.Code = "AddOrUpdateUserBusinessTools:22";
                            result.Message = "Configuration tab form body data property not found.";
                            return result;
                        }

                        Dictionary<string, string> configurationBodyFormData = new Dictionary<string, string>();

                        foreach (var bodyFormData in configurationbodyFormDataProperty.EnumerateObject())
                        {
                            var key = bodyFormData.Name;
                            var value = bodyFormData.Value.GetString();

                            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                            {
                                result.Code = "AddOrUpdateUserBusinessTools:24";
                                result.Message = "Configuration tab form body data key or value is empty.";
                                return result;
                            }

                            if (configurationBodyFormData.ContainsKey(key))
                            {
                                result.Code = "AddOrUpdateUserBusinessTools:25";
                                result.Message = $"Configuration tab form body data key {key} already exists.";
                                return result;
                            }

                            configurationBodyFormData.Add(key, value);
                        }

                        NewBusinessAppToolData.Configuration.BodyData = configurationBodyFormData;
                        break;
                }
            }
            
            // Response Tab
            if (!changes.RootElement.TryGetProperty("response", out var responseTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessTools:26";
                result.Message = "Response tab root element not found.";
                return result;
            }
            else
            {
                foreach (var responseData in responseTabRootElement.EnumerateObject())
                {
                    var statusKey = responseData.Name;
                    var responseDataValue = responseData.Value;

                    if (string.IsNullOrWhiteSpace(statusKey))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:27";
                        result.Message = "Response tab status key is empty.";
                        return result;
                    }
                    if (!int.TryParse(statusKey, out var statusKeyInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:28";
                        result.Message = "Invalid response tab status key value.";
                        return result;
                    }
                    if (!Enum.IsDefined(typeof(HttpStatusEnum), statusKeyInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:29";
                        result.Message = "Response tab status key not found.";
                        return result;
                    }

                    if (NewBusinessAppToolData.Response.ContainsKey((HttpStatusEnum)statusKeyInt))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:30";
                        result.Message = $"Response tab status key {(HttpStatusEnum)statusKeyInt} already exists.";
                        return result;
                    }

                    BusinessAppToolResponse newToolResponseData = new BusinessAppToolResponse();

                    if (!responseDataValue.TryGetProperty("javascript", out var responseJavascriptProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:31";
                        result.Message = "Response tab javascript property not found.";
                        return result;
                    }
                    string? responseJavascriptString = responseJavascriptProperty.GetString();
                    if (string.IsNullOrWhiteSpace(responseJavascriptString))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:32";
                        result.Message = "Response tab javascript is empty.";
                        return result;
                    }
                    // TODO validate that javascript is valid returning a value
                    newToolResponseData.Javascript = responseJavascriptString;

                    if (!responseDataValue.TryGetProperty("hasStaticResponse", out var hasStaticResponseProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:33";
                        result.Message = "Response tab has static response property not found.";
                        return result;
                    }
                    newToolResponseData.HasStaticResponse = hasStaticResponseProperty.GetBoolean();

                    if (newToolResponseData.HasStaticResponse)
                    {
                        newToolResponseData.StaticResponse = new Dictionary<string, string>();
                        var responseStaticResponeValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                            businessLanguages,
                            responseDataValue,
                            "staticResponse",
                            newToolResponseData.StaticResponse
                        );
                        if (!responseStaticResponeValidationResult.Success)
                        {
                            result.Code = "AddOrUpdateUserBusinessTools:" + responseStaticResponeValidationResult.Code;
                            result.Message = responseStaticResponeValidationResult.Message;
                            return result;
                        }
                    }

                    NewBusinessAppToolData.Response.Add((HttpStatusEnum)statusKeyInt, newToolResponseData);
                }
            }

            // Audio Tab
            if (formData.Files.Count > 0)
            {
                var beforeSpeakingAudio = formData.Files.GetFile("audioBeforeSpeaking");
                var duringSpeakingAudio = formData.Files.GetFile("audioDuringSpeaking");
                var afterSpeakingAudio = formData.Files.GetFile("audioAfterSpeaking");

                if (beforeSpeakingAudio != null)
                {
                    var validationResult = await _audioProcessor.ValidateAudioFile(beforeSpeakingAudio);
                    if (!validationResult.IsValid)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:34";
                        result.Message = $"Before speaking audio validation failed: {validationResult.ErrorMessage}.";
                        return result;
                    }

                    bool fileExists = await _businessToolAudioRepository.FileExists(validationResult.Hash);
                    if (!fileExists)
                    {
                        var metadata = new Dictionary<string, string>
                        {
                            { "fileContentType", validationResult.ContentType }
                        };

                        await _businessToolAudioRepository.PutFileAsByteData(
                            validationResult.Hash,
                            validationResult.FileBytes,
                            metadata
                        );
                    }

                    NewBusinessAppToolData.Audio.BeforeSpeaking = validationResult.Hash;
                }

                if (duringSpeakingAudio != null)
                {
                    var validationResult = await _audioProcessor.ValidateAudioFile(duringSpeakingAudio);
                    if (!validationResult.IsValid)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:35";
                        result.Message = $"During speaking audio validation failed: {validationResult.ErrorMessage}.";
                        return result;
                    }

                    bool fileExists = await _businessToolAudioRepository.FileExists(validationResult.Hash);
                    if (!fileExists)
                    {
                        var metadata = new Dictionary<string, string>
                        {
                            { "ContentType", validationResult.ContentType }
                        };

                        await _businessToolAudioRepository.PutFileAsByteData(
                            validationResult.Hash,
                            validationResult.FileBytes,
                            metadata
                        );
                    }

                    NewBusinessAppToolData.Audio.DuringSpeaking = validationResult.Hash;
                }

                if (afterSpeakingAudio != null)
                {
                    var validationResult = await _audioProcessor.ValidateAudioFile(afterSpeakingAudio);
                    if (!validationResult.IsValid)
                    {
                        result.Code = "AddOrUpdateUserBusinessTools:36";
                        result.Message = $"After speaking audio validation failed: {validationResult.ErrorMessage}.";
                        return result;
                    }

                    bool fileExists = await _businessToolAudioRepository.FileExists(validationResult.Hash);
                    if (!fileExists)
                    {
                        var metadata = new Dictionary<string, string>
                        {
                            { "ContentType", validationResult.ContentType }
                        };

                        await _businessToolAudioRepository.PutFileAsByteData(
                            validationResult.Hash,
                            validationResult.FileBytes,
                            metadata
                        );
                    }

                    NewBusinessAppToolData.Audio.AfterSpeaking = validationResult.Hash;
                }
            }

            // Saving or Adding to Database
            if (postType == "new")
            {
                NewBusinessAppToolData.Id = Guid.NewGuid().ToString();

                var addBusinessAppToolResult = await _businessAppRepository.AddBusinessAppTool(businessId, NewBusinessAppToolData);
                if (!addBusinessAppToolResult)
                {
                    result.Code = "AddOrUpdateUserBusinessTools:37";
                    result.Message = "Failed to add business app tool.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                NewBusinessAppToolData.Id = exisitingToolId;

                var saveBusinessAppToolResult = await _businessAppRepository.UpdateBusinessAppTool(businessId, NewBusinessAppToolData);
                if (!saveBusinessAppToolResult)
                {
                    result.Code = "AddOrUpdateUserBusinessTools:38";
                    result.Message = "Failed to save business app tool.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = NewBusinessAppToolData;

            return result;
        }

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
    }
}
