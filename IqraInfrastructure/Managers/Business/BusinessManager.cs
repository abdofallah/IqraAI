using IqraCore.Entities.Business;
using IqraCore.Entities.Configuration;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.S3Storage;
using IqraCore.Utilities;
using IqraCore.Utilities.Audio;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.RAG.Extractors;
using IqraInfrastructure.Managers.RAG.Keywords;
using IqraInfrastructure.Managers.RAG.Processors;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.RAG;
using IqraInfrastructure.Repositories.S3Storage;
using IqraInfrastructure.Repositories.WebSession;
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
        private readonly BusinessToolAudioRepository? _businessToolAudioRepository;
        private readonly BusinessAgentAudioRepository? _businessAgentAudioRepository;
        private readonly OutboundCallQueueRepository? _outboundCallQueueRepository;

        private readonly S3StorageClientFactory? _s3StorageClientFactory;

        private readonly AudioFileProcessor _audioProcessor;

        private readonly LanguagesManager? _languagesManager;

        // Sub Managers
        private readonly BusinessSettingsManager? _businessSettingsManager;
        private readonly BusinessToolsManager? _businessToolsManager;
        private readonly BusinessContextManager? _businessContextManager;
        private readonly BusinessCacheManager? _businessCacheManager;
        private readonly BusinessIntegrationsManager? _businessIntegrationsManager;
        private readonly BusinessAgentsManager? _businessAgentsManager;
        private readonly BusinessScriptsManager? _businessScriptsManager;
        private readonly BusinessNumberManager? _businessNumberManager;
        private readonly BusinessRoutesManager? _businessRoutesManager;
        private readonly BusinessConversationsManager? _businessConversationsManager;
        private readonly BusinessMakeCallManager? _businessMakeCallManager;
        private readonly BusinessKnowledgeBaseManager? _businessKnowledgeBaseManager;
        private readonly BusinessCampaignManager? _businessCampaignManager;
        private readonly BusinessWebSessionManager? _businessWebSessionManager;
        private readonly BusinessPostAnalysisManager? _businessPostAnalysisManager;

        public BusinessManager(
            ILoggerFactory loggerFactory,
            IMongoClient mongoClient,
            BusinessManagerInitalizationSettings settings,
            BusinessRepository businessRepository,
            BusinessAppRepository businessAppRepository,
            BusinessLogoRepository? businessLogoRepository,
            BusinessToolAudioRepository? businessToolAudioRepository,
            BusinessAgentAudioRepository? businessAgentAudioRepository,
            ModemTelManager? modemTelManager,
            IntegrationsManager? integrationsManager,
            LanguagesManager? langaugesManager,
            InboundCallQueueRepository? inboundCallQueueRepo,
            ConversationStateRepository? conversationStateRepository,
            BusinessConversationAudioRepository? conversationAudioRepository,
            RegionManager? regionManager,
            OutboundCallQueueGroupRepository? outboundCallCampaignRepository,
            OutboundCallQueueRepository? outboundCallQueueRepository,
            LanguagesManager? languagesManager,
            TwilioManager? twilioManager,
            IntegrationConfigurationManager? integrationConfigurationManager,
            BusinessKnowledgeBaseDocumentRepository? businessKnowledgeBaseDocumentRepository,
            KnowledgeBaseVectorRepository? knowledgeBaseVectorRepository,
            IndexProcessorFactory? indexProcessorFactory,
            ExtractProcessor? extractProcessor,
            EmbeddingProviderManager? embeddingProviderManager,
            KeywordExtractor? keywordExtractor,
            RAGKeywordStore? ragKeywordStore,
            WebSessionRepository? webSessionRepoistory,
            UserUsageValidationManager? billingValidationManager,
            ServerSelectionManager? serverSelectionManager,
            IHttpClientFactory? httpClientFactory,
            S3StorageClientFactory? s3StorageClientFactory
        )
        {
            _logger = loggerFactory.CreateLogger<BusinessManager>();

            _settings = settings;

            _businessRepository = businessRepository;
            _businessAppRepository = businessAppRepository;
            _businessLogoRepository = businessLogoRepository;
            _businessToolAudioRepository = businessToolAudioRepository;
            _businessAgentAudioRepository = businessAgentAudioRepository;
            _outboundCallQueueRepository = outboundCallQueueRepository;

            _s3StorageClientFactory = s3StorageClientFactory;

            _audioProcessor = new AudioFileProcessor();

            _languagesManager = langaugesManager;

            // Sub Managers
            if (_settings.InitalizeSettingsManager)
            {
                if (businessLogoRepository == null || langaugesManager == null || _s3StorageClientFactory == null)
                {
                    throw new Exception("Null constructor input variable for BusinessSettingsManager");
                }
                _businessSettingsManager = new BusinessSettingsManager(loggerFactory.CreateLogger<BusinessSettingsManager>(), this, businessRepository, businessAppRepository, businessLogoRepository, langaugesManager, _s3StorageClientFactory);
            }
            if (_settings.InitalizeToolsManager || _settings.InitalizeToolsCURDManager)
            {
                if (_settings.InitalizeToolsManager && (businessAppRepository == null || businessRepository == null || businessToolAudioRepository == null || _s3StorageClientFactory == null))
                {
                    throw new Exception("Null constructor input variable for BusinessToolsManager");
                }
                if (_settings.InitalizeToolsCURDManager && (businessAppRepository == null || businessRepository == null))
                {
                    throw new Exception("Null constructor input variable for BusinessToolsManager with CURD");
                }

                _businessToolsManager = new BusinessToolsManager(this, businessAppRepository, businessRepository, businessToolAudioRepository, _audioProcessor, _s3StorageClientFactory);
            }
            if (_settings.InitalizeContextManager)
            {
                _businessContextManager = new BusinessContextManager(this, businessAppRepository, businessRepository);
            }
            if (_settings.InitalizeCacheManager)
            {
                _businessCacheManager = new BusinessCacheManager(loggerFactory.CreateLogger<BusinessCacheManager>(), this, businessAppRepository, businessRepository);
            }
            if (_settings.InitalizeIntegrationsManager)
            {
                _businessIntegrationsManager = new BusinessIntegrationsManager(this, businessAppRepository);
            }
            if (_settings.InitalizeAgentsManager)
            {
                if (businessAgentAudioRepository == null || integrationConfigurationManager == null || _s3StorageClientFactory == null)
                {
                    throw new Exception("Null constructor input variable for BusinessAgentsManager");
                }
                _businessAgentsManager = new BusinessAgentsManager(this, mongoClient, businessAppRepository, businessRepository, _s3StorageClientFactory, businessAgentAudioRepository, _audioProcessor, integrationConfigurationManager);
            }
            if (_settings.InitalizeScriptsManager)
            {
                if (businessAppRepository == null || businessRepository == null)
                {
                    throw new Exception("Null constructor input variable for BusinessScriptsManager");
                }
                _businessScriptsManager = new BusinessScriptsManager(this, mongoClient, businessAppRepository, businessRepository);
            }
            if (_settings.InitalizeNumberManager)
            {
                if (modemTelManager == null || twilioManager == null || integrationsManager == null || regionManager == null)
                {
                    throw new Exception("Null constructor input variable for BusinessNumberManager");
                }
                _businessNumberManager = new BusinessNumberManager(this, mongoClient, businessAppRepository, businessRepository, modemTelManager, twilioManager, integrationsManager, regionManager);
            }
            if (_settings.InitalizeRoutesManager)
            {
                if (integrationConfigurationManager == null)
                {
                    throw new Exception("Null constructor input variable for BusinessRoutesManager");
                }

                _businessRoutesManager = new BusinessRoutesManager(this, mongoClient, businessAppRepository, businessRepository, integrationConfigurationManager);
            }
            if (_settings.InitalizeConversationsManager)
            {
                if (conversationStateRepository == null || conversationAudioRepository == null || inboundCallQueueRepo == null || outboundCallQueueRepository == null || webSessionRepoistory == null)
                {
                    throw new Exception("Null constructor input variable for BusinessConversationsManager");
                }
                _businessConversationsManager = new BusinessConversationsManager(this, inboundCallQueueRepo, outboundCallQueueRepository, conversationStateRepository, conversationAudioRepository, webSessionRepoistory);
            }
            if (_settings.InitalizeMakeCallManager)
            {
                if (regionManager == null || outboundCallCampaignRepository == null || outboundCallQueueRepository == null || integrationConfigurationManager == null)
                {
                    throw new Exception("Null constructor input variable for BusinessMakeCallManager");
                }
                _businessMakeCallManager = new BusinessMakeCallManager(loggerFactory.CreateLogger<BusinessMakeCallManager>(), this, regionManager, outboundCallCampaignRepository, outboundCallQueueRepository, integrationConfigurationManager);
            }
            if (_settings.InitalizeKnowledgeBaseManager)
            {
                if (businessKnowledgeBaseDocumentRepository == null || knowledgeBaseVectorRepository == null || indexProcessorFactory == null || extractProcessor == null || embeddingProviderManager == null || keywordExtractor == null || ragKeywordStore == null)
                {
                    throw new Exception("Null constructor input variable for BusinessKnowledgeBaseManager");
                }
                _businessKnowledgeBaseManager = new BusinessKnowledgeBaseManager(this, mongoClient, businessAppRepository, businessKnowledgeBaseDocumentRepository, integrationConfigurationManager, knowledgeBaseVectorRepository, indexProcessorFactory, extractProcessor, keywordExtractor, embeddingProviderManager, ragKeywordStore);
            }
            if (_settings.InitalizeCampaignManager || _settings.InitalizeCampaignCURDManager)
            {
                if (_settings.InitalizeCampaignManager && (businessAppRepository == null || businessRepository == null || integrationConfigurationManager == null))
                {
                    throw new Exception("Null constructor input variable for BusinessCampaignManager");
                }
                if (_settings.InitalizeCampaignCURDManager && (businessAppRepository == null || businessRepository == null))
                {
                    throw new Exception("Null constructor input variable for BusinessCampaignManager with CURD");
                }

                _businessCampaignManager = new BusinessCampaignManager(this, mongoClient, businessAppRepository, businessRepository, integrationConfigurationManager);
            }
            if (_settings.InitalizeWebSessionManager)
            {
                if (webSessionRepoistory == null || billingValidationManager == null || serverSelectionManager == null || regionManager == null || httpClientFactory == null)
                {
                    throw new Exception("Null constructor input variable for BusinessWebSessionManager");
                }
                _businessWebSessionManager = new BusinessWebSessionManager(this, webSessionRepoistory, billingValidationManager, serverSelectionManager, regionManager, httpClientFactory);
            }
            if (_settings.InitalizePostAnalysisManager)
            {
                if (businessAppRepository == null || integrationConfigurationManager == null)
                {
                    throw new Exception("Null constructor input variable for BusinessPostAnalysisManager");
                }
               _businessPostAnalysisManager = new BusinessPostAnalysisManager(this, mongoClient, businessAppRepository, integrationConfigurationManager);
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
                    result.Message = "Business logo too large. Allowed file size is 3MB.";
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
                var fileName = hash + ".webp";

                bool fileExists = await _businessLogoRepository!.FileExists(fileName);
                if (!fileExists)
                {
                    await _businessLogoRepository.PutFileAsByteData(fileName, webpImage, new Dictionary<string, string>());
                }

                businessData.LogoS3StorageLink = new S3StorageFileLink
                {
                    ObjectName = fileName,
                    OriginRegion = _s3StorageClientFactory!.GetCurrentRegion()
                };
            }

            var businessApp = new BusinessApp()
            {
                Id = businessId,
            };

            await _businessAppRepository.AddBusinessAppAsync(businessApp, mongoSession);
            await _businessRepository.AddBusinessAsync(businessData, mongoSession);

            return result.SetSuccessResult(businessData);
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

        public async Task<FunctionReturnResult> CancelBusinessOutboundCallQueues(long businessIdLong, IClientSessionHandle mongoSession)
        {
            var result = new FunctionReturnResult();

            if (_outboundCallQueueRepository == null)
            {
                return result.SetFailureResult(
                    "CancelBusinessOutboundCallQueues:OUTBOUND_CALL_QUEUE_REPOSITORY_NOT_INITALIZED",
                    "Outbound call queue repository not initalized."
                );
            }

            var cancelCallQueuesResult = await _outboundCallQueueRepository.CancelBusinessCallQueuesAsync(businessIdLong, mongoSession);
            if (!cancelCallQueuesResult)
            {
                return result.SetFailureResult(
                    "CancelBusinessOutboundCallQueues:FAILED_TO_CANCEL_BUSINESS_CALL_QUEUES",
                    "Failed to cancel business call queues."
                );
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
            if ((!_settings.InitalizeToolsManager && !_settings.InitalizeToolsCURDManager) || _businessToolsManager == null) throw new Exception("Tools manager not initalized");
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

        public BusinessScriptsManager GetScriptsManager()
        {
            if (!_settings.InitalizeScriptsManager || _businessScriptsManager == null) throw new Exception("Scripts manager not initalized");
            return _businessScriptsManager;
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

        public BusinessKnowledgeBaseManager GetKnowledgeBaseManager()
        {
            if (!_settings.InitalizeKnowledgeBaseManager || _businessKnowledgeBaseManager == null) throw new Exception("Knowledge Base manager not initialized");
            return _businessKnowledgeBaseManager;
        }
    
        public BusinessCampaignManager GetCampaignManager()
        {
            if ((!_settings.InitalizeCampaignManager && !_settings.InitalizeCampaignCURDManager) || _businessCampaignManager == null) throw new Exception("Campaign manager not initialized");
            return _businessCampaignManager;
        }
    
        public BusinessWebSessionManager GetWebSessionmanager()
        {
            if (!_settings.InitalizeWebSessionManager || _businessWebSessionManager == null) throw new Exception("Web Session manager not initialized");
            return _businessWebSessionManager;
        }
    
        public BusinessPostAnalysisManager GetPostAnalysisManager() {
            if (!_settings.InitalizePostAnalysisManager || _businessPostAnalysisManager == null) throw new Exception("Post Analysis manager not initialized");
            return _businessPostAnalysisManager;
        }
    }
}
