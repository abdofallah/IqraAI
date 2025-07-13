using IqraCore.Entities.Business;
using IqraCore.Entities.Configuration;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraCore.Utilities.Audio;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessManager
    {
        private readonly ILogger<BusinessManager> _logger;

        private readonly BusinessManagerInitalizationSettings _settings;

        private readonly BusinessRepository _businessRepository;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessLogoRepository? _businessLogoRepository;
        private readonly BusinessWhiteLabelDomainRepository? _businessWhiteLabelDomainRepository;
        private readonly BusinessDomainVestaCPRepository? _businessIqraBusinessDomainsVestaCPRepository;
        private readonly BusinessToolAudioRepository? _businessToolAudioRepository;
        private readonly BusinessAgentAudioRepository? _businessAgentAudioRepository;

        private readonly AudioFileProcessor _audioProcessor;

        private readonly LanguagesManager? _languagesManager;

        // Sub Managers
        private readonly BusinessSettingsManager? _businessSettingsManager;
        private readonly BusinessToolsManager? _businessToolsManager;
        private readonly BusinessContextManager? _businessContextManager;
        private readonly BusinessCacheManager? _businessCacheManager;
        private readonly BusinessIntegrationsManager? _businessIntegrationsManager;
        private readonly BusinessAgentsManager? _businessAgentsManager;
        private readonly BusinessNumberManager? _businessNumberManager;
        private readonly BusinessRoutesManager? _businessRoutesManager;
        private readonly BusinessConversationsManager? _businessConversationsManager;
        private readonly BusinessMakeCallManager? _businessMakeCallManager;

        public BusinessManager(
            ILoggerFactory loggerFactory,
            BusinessManagerInitalizationSettings settings,
            BusinessRepository businessRepository,
            BusinessAppRepository businessAppRepository,
            BusinessLogoRepository? businessLogoRepository,
            BusinessWhiteLabelDomainRepository? businessWhiteLabelDomainRepository,
            BusinessDomainVestaCPRepository? businessIqraBusinessDomainsVestaCPRepository,
            BusinessToolAudioRepository? businessToolAudioRepository,
            BusinessAgentAudioRepository? businessAgentAudioRepository,
            ModemTelManager? modemTelManager,
            IntegrationsManager? integrationsManager,
            LanguagesManager? langaugesManager,
            InboundCallQueueRepository? inboundCallQueueRepo,
            ConversationStateRepository? conversationStateRepository,
            ConversationAudioRepository? conversationAudioRepository,
            RegionManager? regionManager,
            OutboundCallCampaignRepository? outboundCallCampaignRepository,
            OutboundCallQueueRepository? outboundCallQueueRepository,
            LanguagesManager? languagesManager,
            TwilioManager? twilioManager
        )
        {
            _logger = loggerFactory.CreateLogger<BusinessManager>();

            _settings = settings;

            _businessRepository = businessRepository;
            _businessAppRepository = businessAppRepository;
            _businessLogoRepository = businessLogoRepository;
            _businessWhiteLabelDomainRepository = businessWhiteLabelDomainRepository;
            _businessIqraBusinessDomainsVestaCPRepository = businessIqraBusinessDomainsVestaCPRepository;
            _businessToolAudioRepository = businessToolAudioRepository;
            _businessAgentAudioRepository = businessAgentAudioRepository;

            _audioProcessor = new AudioFileProcessor();

            _languagesManager = langaugesManager;

            // Sub Managers
            if (_settings.InitalizeSettingsManager)
            {
                if (businessWhiteLabelDomainRepository == null || businessLogoRepository == null || businessIqraBusinessDomainsVestaCPRepository == null || langaugesManager == null)
                {
                    throw new Exception("Null constructor input variable for BusinessSettingsManager");
                }
                _businessSettingsManager = new BusinessSettingsManager(loggerFactory.CreateLogger<BusinessSettingsManager>(), this, businessRepository, businessAppRepository, businessWhiteLabelDomainRepository, businessLogoRepository, businessIqraBusinessDomainsVestaCPRepository, langaugesManager);
            }
            if (_settings.InitalizeToolsManager)
            {
                if (businessToolAudioRepository == null)
                {
                    throw new Exception("Null constructor input variable for BusinessToolsManager");
                }
                _businessToolsManager = new BusinessToolsManager(this, businessAppRepository, businessRepository, businessToolAudioRepository, _audioProcessor);
            }
            if (_settings.InitalizeContextManager)
            {
                _businessContextManager = new BusinessContextManager(this, businessAppRepository, businessRepository);
            }
            if (_settings.InitalizeCacheManager)
            {
                _businessCacheManager = new BusinessCacheManager(this, businessAppRepository, businessRepository);
            }
            if (_settings.InitalizeIntegrationsManager)
            {
                _businessIntegrationsManager = new BusinessIntegrationsManager(this, businessAppRepository);
            }
            if (_settings.InitalizeAgentsManager)
            {
                if (businessAgentAudioRepository == null)
                {
                    throw new Exception("Null constructor input variable for BusinessAgentsManager");
                }
                _businessAgentsManager = new BusinessAgentsManager(this, businessAppRepository, businessRepository, businessAgentAudioRepository, _audioProcessor);
            }
            if (_settings.InitalizeNumberManager)
            {
                if (modemTelManager == null || twilioManager == null || integrationsManager == null)
                {
                    throw new Exception("Null constructor input variable for BusinessNumberManager");
                }
                _businessNumberManager = new BusinessNumberManager(this, businessAppRepository, businessRepository, modemTelManager, twilioManager, integrationsManager);
            }
            if (_settings.InitalizeRoutesManager)
            {
                _businessRoutesManager = new BusinessRoutesManager(this, businessAppRepository, businessRepository);
            }
            if (_settings.InitalizeConversationsManager)
            {
                if (conversationStateRepository == null || conversationAudioRepository == null || inboundCallQueueRepo == null)
                {
                    throw new Exception("Null constructor input variable for BusinessConversationsManager");
                }
                _businessConversationsManager = new BusinessConversationsManager(this, inboundCallQueueRepo, conversationStateRepository, conversationAudioRepository);
            }
            if (_settings.InitalizeMakeCallManager)
            {
                if (regionManager == null || outboundCallCampaignRepository == null || outboundCallQueueRepository == null)
                {
                    throw new Exception("Null constructor input variable for BusinessMakeCallManager");
                }
                _businessMakeCallManager = new BusinessMakeCallManager(loggerFactory.CreateLogger<BusinessMakeCallManager>(), this, regionManager, outboundCallCampaignRepository, outboundCallQueueRepository);
            }
        }

        /**
         * 
         * General Functions
         *
        **/

        public async Task<FunctionReturnResult<BusinessData?>> AddBusiness(string userEmail, IFormCollection formData, IClientSessionHandle mongoSession)
        {
            var result = new FunctionReturnResult<BusinessData?>();

            string? businessName = formData["BusinessName"];
            string? businessDefaultLanguage = formData["BusinessDefaultLanguage"];
            IFormFile? businessLogoFile = formData.Files.GetFile("BusinessLogo");

            if (string.IsNullOrWhiteSpace(businessName) || businessName.Length > 64)
            {
                result.Code = "AddBusiness:5";
                result.Message = "Invalid business name. Minimum length is 1 and maximum length is 64.";
                return result;
            }

            // Valdiate Langauge
            if (string.IsNullOrWhiteSpace(businessDefaultLanguage))
            {
                result.Code = "AddUserBusiness:6";
                result.Message = "Missing business default language.";
                return result;
            }
            var langaugeData = await _languagesManager.GetLanguageByCode(businessDefaultLanguage);
            if (!langaugeData.Success)
            {
                result.Code = "AddUserBusiness:" + langaugeData.Code;
                result.Message = langaugeData.Message;
                return result;
            }
            if (langaugeData.Data.DisabledAt != null)
            {
                result.Code = "AddUserBusiness:7";
                result.Message = "Business default language is disabled.";
                return result;
            }

            // Valdiate Business Logo if exists
            if (businessLogoFile != null)
            {
                int imageResult = ImageHelper.ValidateBusinessLogoFile(businessLogoFile);
                if (imageResult == 0)
                {
                    result.Code = "AddUserBusiness:8";
                    result.Message = "Business logo too large. Allowed file size is 5MB.";
                    return result;
                }

                if (imageResult == 1)
                {
                    result.Code = "AddUserBusiness:9";
                    result.Message = "Invalid business logo file. Allowed file types are: png, jpg, jpeg, webp, gif.";
                    return result;
                }

                if (imageResult != 200)
                {
                    result.Code = "AddUserBusiness:10";
                    result.Message = "Failed to validate business logo.";
                    return result;
                }
            }

            BusinessData businessData = new BusinessData()
            {
                Name = businessName,
                MasterUserEmail = userEmail,
                DefaultLanguage = businessDefaultLanguage,
                Languages = new List<string> { businessDefaultLanguage },
                Tutorials = new Dictionary<string, object>()
                    {
                        { "NewBusinessTutorial", true}
                    }
            };

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

            /** TODO this takes too long so we disable it for now, enable it to run in background, requires overhauling the subdomain system
            string subDomainHash = SubdomainHashGenerator.GenerateSubdomainHash(businessId);
            var addDefaultDomainResult = await _businessSettingsManager.AddOrUpdateUserBusinessDomain(
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
            **/

            await _businessAppRepository.AddBusinessAppAsync(businessApp, mongoSession);
            await _businessRepository.AddBusinessAsync(businessData, mongoSession);

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
                _logger.LogError("[BusinessManager] " + result.Message);
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
                _logger.LogError("[BusinessManager] " + result.Message);
            }
            else if (businessesId.Count != getResult.Count)
            {
                result.Code = "GetUserBusinessesByIds:2";
                result.Message = "Not all bussiness found for user: " + userEmail;
                _logger.LogError("[BusinessManager] " + result.Message);
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
                _logger.LogError("[BusinessManager] Null - Business not found for user: " + userEmail);
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
                _logger.LogError("[BusinessManager] Null - Business app not found for user: " + userEmail);
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
                _logger.LogError("[BusinessManager] Null - Businesses not found");
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
                _logger.LogError("[BusinessManager] Null - Search Businesses not found");
            }
            else
            {
                result.Success = true;
                result.Data = businesses;
            }

            return result;
        }

        public async Task<bool> CheckUserBusinessExists(long businessId, string userEmail)
        {
            var result = await _businessRepository.CheckBusinessExists(businessId, userEmail);
            return result;
        }


        public async Task<FunctionReturnResult> DeleteBusiness(long businessIdLong, IClientSessionHandle mongoSession)
        {
            var result = new FunctionReturnResult();

            var deleteAppResult = await _businessAppRepository.MoveBusinessToArchivedAsync(businessIdLong, mongoSession);
            if (!deleteAppResult)
            {
                return result.SetFailureResult("DeleteBusiness:1", "Failed to delete business app.");
            }

            var deleteDataResult = await _businessRepository.MoveBusinessToArchivedAsync(businessIdLong, mongoSession);
            if (!deleteDataResult)
            {
                return result.SetFailureResult("DeleteBusiness:2", "Failed to delete business data.");
            }

            return result.SetSuccessResult();
        }

        /**
         * 
         * Sub Managers
         *
        **/

        public BusinessSettingsManager GetSettingsManager()
        {
            if (!_settings.InitalizeSettingsManager || _businessSettingsManager == null) throw new Exception("Settings manager not initalized");
            return _businessSettingsManager;
        }

        public BusinessToolsManager GetToolsManager()
        {
            if (!_settings.InitalizeToolsManager || _businessToolsManager == null) throw new Exception("Tools manager not initalized");
            return _businessToolsManager;
        }

        public BusinessContextManager GetContextManager()
        {
            if (!_settings.InitalizeContextManager || _businessContextManager == null) throw new Exception("Context manager not initalized");
            return _businessContextManager;
        }

        public BusinessCacheManager GetCacheManager()
        {
            if (!_settings.InitalizeCacheManager || _businessCacheManager == null) throw new Exception("Cache manager not initalized");
            return _businessCacheManager;
        }

        public BusinessIntegrationsManager GetIntegrationsManager()
        {
            if (!_settings.InitalizeIntegrationsManager || _businessIntegrationsManager == null) throw new Exception("Integrations manager not initalized");
            return _businessIntegrationsManager;
        }

        public BusinessAgentsManager GetAgentsManager()
        {
            if (!_settings.InitalizeAgentsManager || _businessAgentsManager == null) throw new Exception("Agents manager not initalized");
            return _businessAgentsManager;
        }

        public BusinessNumberManager GetNumberManager()
        {
            if (!_settings.InitalizeNumberManager || _businessNumberManager == null) throw new Exception("Number manager not initalized");
            return _businessNumberManager;
        }

        public BusinessRoutesManager GetRoutesManager()
        {
            if (!_settings.InitalizeRoutesManager || _businessRoutesManager == null) throw new Exception("Routes manager not initalized");
            return _businessRoutesManager;
        }
    
        public BusinessConversationsManager GetConversationsManager() {
            if (!_settings.InitalizeConversationsManager || _businessConversationsManager == null) throw new Exception("Conversations manager not initalized");
            return _businessConversationsManager;
        }

        public BusinessMakeCallManager GetMakeCallManager()
        {
            if (!_settings.InitalizeMakeCallManager || _businessMakeCallManager == null) throw new Exception("Make Call manager not initialized");
            return _businessMakeCallManager;
        }

    }
}
