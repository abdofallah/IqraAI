using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using IqraInfrastructure.Managers.Languages;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessSettingsManager
    {
        private readonly ILogger<BusinessSettingsManager> _logger;

        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessRepository _businessRepository;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessWhiteLabelDomainRepository _businessWhiteLabelDomainRepository;
        private readonly BusinessLogoRepository _businessLogoRepository;
        private readonly BusinessDomainVestaCPRepository _businessIqraBusinessDomainsVestaCPRepository;
        private readonly LanguagesManager _languagesManager;

        private readonly List<Task> _sslFailedRetryTasks = new List<Task>();

        public BusinessSettingsManager(
            ILogger<BusinessSettingsManager> logger,
            BusinessManager businessManager,
            BusinessRepository businessRepository,
            BusinessAppRepository businessAppRepository,
            BusinessWhiteLabelDomainRepository businessWhiteLabelDomainRepository,
            BusinessLogoRepository businessLogoRepository,
            BusinessDomainVestaCPRepository businessIqraBusinessDomainsVestaCPRepository,
            LanguagesManager languagesManager
        )
        {
            _logger = logger;

            _parentBusinessManager = businessManager;

            _businessRepository = businessRepository;
            _businessAppRepository = businessAppRepository;
            _businessWhiteLabelDomainRepository = businessWhiteLabelDomainRepository;
            _businessLogoRepository = businessLogoRepository;
            _businessIqraBusinessDomainsVestaCPRepository = businessIqraBusinessDomainsVestaCPRepository;
            _languagesManager = languagesManager;
        }

        /**
         * 
         * Settings Tab
         * General
         * 
        **/

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
                    result.Message = "The business logo file is too big. Maximum size is 3MB.";
                    return result;
                }
                else if (logoValidateResult == 1)
                {
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
            string? businessSettingLanguageTabChangesString = formData["languagesTab"].ToString();
            if (!string.IsNullOrWhiteSpace(businessSettingLanguageTabChangesString))
            {
                JsonElement langaugeTabRootJson;
                try
                {
                    langaugeTabRootJson = JsonSerializer.Deserialize<JsonElement>(businessSettingLanguageTabChangesString);
                }
                catch
                {
                    result.Code = "UpdateUserBusinessSettings:5";
                    result.Message = "Unable to parse languages tab changes data.";
                    return result;
                }

                if (!langaugeTabRootJson.TryGetProperty("defaultLanguage", out var businessDefaultLanguage))
                {
                    result.Code = "UpdateUserBusinessSettings:6";
                    result.Message = "Business default language property not found.";
                    return result;
                }
                if (businessDefaultLanguage.ValueKind != JsonValueKind.String)
                {
                    result.Code = "UpdateUserBusinessSettings:7";
                    result.Message = "Business default language is not a string.";
                    return result;
                }
                var businessDefaultLanguageString = businessDefaultLanguage.GetString();
                if (string.IsNullOrWhiteSpace(businessDefaultLanguageString))
                {
                    result.Code = "UpdateUserBusinessSettings:8";
                    result.Message = "Business default language is empty.";
                    return result;
                }

                if (!langaugeTabRootJson.TryGetProperty("languages", out var businessLangauges))
                {
                    result.Code = "UpdateUserBusinessSettings:9";
                    result.Message = "Business languages property not found.";
                    return result;
                }
                var businessLanguagesJsonList = businessLangauges.EnumerateArray().ToList();
                if (businessLanguagesJsonList == null || businessLanguagesJsonList.Count == 0)
                {
                    result.Code = "UpdateUserBusinessSettings:10";
                    result.Message = "Must have at least one language selected.";
                    return result;
                }              

                List<string> builtLangaugesList = new List<string>();
                for (int i = 0; i < businessLanguagesJsonList.Count; i++)
                {
                    var languageCode = businessLanguagesJsonList[i];

                    if (languageCode.ValueKind != JsonValueKind.String)
                    {
                        result.Code = "UpdateUserBusinessSettings:11";
                        result.Message = $"Language code at index {i} is not a string";
                        return result;
                    }

                    var langaugeCodeString = languageCode.GetString();
                    if (string.IsNullOrWhiteSpace(langaugeCodeString))
                    {
                        result.Code = "UpdateUserBusinessSettings:12";
                        result.Message = $"Language code at index {i} is empty.";
                        return result;
                    }

                    var langaugeDataResult = await _languagesManager.GetLanguageByCode(langaugeCodeString);
                    if (!langaugeDataResult.Success)
                    {
                        result.Code = "UpdateUserBusinessSettings:" + langaugeDataResult.Code;
                        result.Message = langaugeDataResult.Message;
                        return result;
                    }

                    if (langaugeDataResult.Data.DisabledAt != null)
                    {
                        result.Code = "UpdateUserBusinessSettings:13";
                        result.Message = $"Selected language {langaugeCodeString} is disabled.";
                        return result;
                    }

                    builtLangaugesList.Add(langaugeCodeString);
                }

                if (!builtLangaugesList.Contains(businessDefaultLanguageString))
                {
                    result.Code = "UpdateUserBusinessSettings:14";
                    result.Message = "Default language must be selected.";
                    return result;
                }

                int addedCount = 0;
                int remainedCount = 0;
                foreach (string oldLanguage in businessData.Languages)
                {
                    if (builtLangaugesList.Contains(oldLanguage))
                    {
                        remainedCount++;
                    }
                }
                foreach (string newLanguage in builtLangaugesList)
                {
                    if (!businessData.Languages.Contains(newLanguage))
                    {
                        addedCount++;
                    }
                }

                if (remainedCount != businessData.Languages.Count || addedCount > 0)
                {
                    var businessLanguagesUpdateResult = MultiLanguageHelper.UpdateObjectMultiLanguages(businessApp, builtLangaugesList, businessData.Languages);
                    if (!businessLanguagesUpdateResult.Success)
                    {
                        result.Code = businessLanguagesUpdateResult.Code;
                        result.Message = businessLanguagesUpdateResult.Message;
                        return result;
                    }
                }

                updateDefinitions.Add(Builders<BusinessData>.Update.Set(d => d.Languages, builtLangaugesList));
                updateDefinitions.Add(Builders<BusinessData>.Update.Set(d => d.DefaultLanguage, businessDefaultLanguageString));

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
                result.Code = "UpdateUserBusinessSettings:15";
                result.Message = "Nothing to update.";
                return result;
            }

            // If all is valid, update business and businesapp
            var updateBusinessResult = await _businessRepository.UpdateBusinessAsync(businessData.Id, Builders<BusinessData>.Update.Combine(updateDefinitions));
            if (!updateBusinessResult)
            {
                result.Code = "UpdateUserBusinessSettings:16";
                result.Message = "Failed to update business data.";
                return result;
            }

            if (updateBusinessApp)
            {
                var updateBusinessAppResult = await _businessAppRepository.ReplaceBusinessAppAsync(businessApp);
                if (!updateBusinessAppResult)
                {
                    await _businessRepository.ReplaceBusinessAsync(businessDataBackup);

                    result.Code = "UpdateUserBusinessSettings:17";
                    result.Message = "Failed to update business app so reverted business data changes.";
                    return result;
                }
            }

            result.Success = true;
            return result;
        }

        /**
         * 
         * Settings Tab
         * Whilelabel Domains
         * 
        **/

        public async Task<FunctionReturnResult<List<BusinessWhiteLabelDomain>?>> GetUserBusinessWhiteLabelDomainByIds(string email, long businessId, List<long> whitelabelDomainId)
        {
            var result = new FunctionReturnResult<List<BusinessWhiteLabelDomain>?>();

            List<BusinessWhiteLabelDomain>? businessWhiteLabelDomain = await _businessWhiteLabelDomainRepository.GetBusinessWhiteLabelDomainsAsync(whitelabelDomainId);
            if (businessWhiteLabelDomain == null)
            {
                result.Code = "GetUserBusinessWhiteLabelDomainByIds:1";
                _logger.LogError("[BusinessManager] Null - Business white label domains not found for user: " + email + " business id: " + businessId);
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
                _logger.LogError("[BusinessManager] Null - Business white label domains not found for user: " + email + " business id: " + businessId);
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
                    if (addSSLResult.Success)
                    {
                        var setHTTPSTemplateResult = await _businessIqraBusinessDomainsVestaCPRepository.SetIqraSubDomainDefaultProxyTemplate(currentIqraSubdomainData.SubDomain, true);
                        if (!setHTTPSTemplateResult.Success)
                        {
                            result.Code = "AddOrUpdateUserBusinessDomain:" + setHTTPSTemplateResult.Code;
                            result.Message = setHTTPSTemplateResult.Message;
                            return result;
                        }
                    }
                    else
                    {
                        _sslFailedRetryTasks.Add(
                            Task.Run(async () =>
                                {
                                    var tries = 0;
                                    while (true)
                                    {
                                        if (tries == 6)
                                        {
                                            break;
                                        }

                                        var addSSLResult = await _businessIqraBusinessDomainsVestaCPRepository.AddIqraBusinessSubDomainLetsEncryptSSL(currentIqraSubdomainData.SubDomain);
                                        if (!addSSLResult.Success)
                                        {
                                            continue;
                                        }

                                        var setHTTPSTemplateResult = await _businessIqraBusinessDomainsVestaCPRepository.SetIqraSubDomainDefaultProxyTemplate(currentIqraSubdomainData.SubDomain, true);
                                        if (!setHTTPSTemplateResult.Success)
                                        {
                                            // TODO LOG THIS
                                        }

                                        await Task.Delay(180000); // 3 minutes
                                        tries++;
                                    }
                                }
                            )
                        );
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
                    catch (Exception ex)
                    {
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

        /**
         * 
         * Settings Tab
         * Subuser
         * 
        **/

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
                    result.Message = "The whitelabel style logo file is too big. Maximum size is 3MB.";
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
                    result.Message = "The whitelabel style favicon file is too big. Maximum size is 3MB.";
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

        private async Task<FunctionReturnResult<BusinessUserPermission?>> ValidateAndPopulateBusinessSubUserPermissions(JsonElement whiteLabelTabPermissionsTabRootElement)
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

    }
}
