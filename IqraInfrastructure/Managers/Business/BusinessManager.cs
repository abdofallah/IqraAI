using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraCore.Utilities.Audio;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Managers.Integrations;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Text.Json;
using IqraInfrastructure.Managers.Telephony;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessManager
    {
        private readonly BusinessRepository _businessRepository;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessLogoRepository _businessLogoRepository;
        private readonly BusinessWhiteLabelDomainRepository _businessWhiteLabelDomainRepository;
        private readonly BusinessDomainVestaCPRepository _businessIqraBusinessDomainsVestaCPRepository;
        private readonly BusinessToolAudioRepository _businessToolAudioRepository;
        private readonly BusinessAgentAudioRepository _businessAgentAudioRepository;

        private readonly AudioFileProcessor _audioProcessor;

        private readonly IntegrationsManager _integrationsManager;
        private readonly ModemTelManager _modemTelManager;

        // Sub Managers
        private readonly BusinessSettingsManager _businessSettingsManager;
        private readonly BusinessToolsManager _businessToolsManager;
        private readonly BusinessContextManager _businessContextManager;
        private readonly BusinessCacheManager _businessCacheManager;
        private readonly BusinessIntegrationsManager _businessIntegrationsManager;
        private readonly BusinessAgentsManager _businessAgentsManager;
        private readonly BusinessNumberManager _businessNumberManager;
        private readonly BusinessRoutesManager _businessRoutesManager;



        public BusinessManager(
            BusinessRepository businessRepository,
            BusinessAppRepository businessAppRepository,
            BusinessLogoRepository businessLogoRepository,
            BusinessWhiteLabelDomainRepository businessWhiteLabelDomainRepository,
            BusinessDomainVestaCPRepository businessIqraBusinessDomainsVestaCPRepository,
            BusinessToolAudioRepository businessToolAudioRepository,
            BusinessAgentAudioRepository businessAgentAudioRepository,
            ModemTelManager modemTelManager,
            IntegrationsManager integrationsManager
        )
        {
            _businessRepository = businessRepository;
            _businessAppRepository = businessAppRepository;
            _businessLogoRepository = businessLogoRepository;
            _businessWhiteLabelDomainRepository = businessWhiteLabelDomainRepository;
            _businessIqraBusinessDomainsVestaCPRepository = businessIqraBusinessDomainsVestaCPRepository;
            _businessToolAudioRepository = businessToolAudioRepository;
            _businessAgentAudioRepository = businessAgentAudioRepository;

            _audioProcessor = new AudioFileProcessor();

            _integrationsManager = integrationsManager;
            _modemTelManager = modemTelManager;

            // Sub Managers
            _businessSettingsManager = new BusinessSettingsManager(this, businessRepository, businessAppRepository, businessWhiteLabelDomainRepository, businessLogoRepository, businessIqraBusinessDomainsVestaCPRepository);
            _businessToolsManager = new BusinessToolsManager(this, businessAppRepository, businessRepository, businessToolAudioRepository, _audioProcessor);
            _businessContextManager = new BusinessContextManager(this, businessAppRepository, businessRepository);
            _businessCacheManager = new BusinessCacheManager(this, businessAppRepository, businessRepository);
            _businessIntegrationsManager = new BusinessIntegrationsManager(this, businessAppRepository);
            _businessAgentsManager = new BusinessAgentsManager(this, businessAppRepository, businessRepository, businessAgentAudioRepository, _audioProcessor);
            _businessNumberManager = new BusinessNumberManager(this, businessAppRepository, businessRepository, modemTelManager, integrationsManager);
            _businessRoutesManager = new BusinessRoutesManager(this, businessAppRepository, businessRepository);
        }

        /**
         * 
         * General Functions
         *
        **/

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

        public async Task<bool> CheckUserBusinessExists(long businessId, string userEmail)
        {
            var result = await _businessRepository.CheckBusinessExists(businessId, userEmail);
            return result;
        }

        /**
         * 
         * Sub Managers
         *
        **/

        public BusinessSettingsManager GetSettingsManager()
        {
            return _businessSettingsManager;
        }

        public BusinessToolsManager GetToolsManager()
        {
            return _businessToolsManager;
        }

        public BusinessContextManager GetContextManager()
        {
            return _businessContextManager;
        }

        public BusinessCacheManager GetCacheManager()
        {
            return _businessCacheManager;
        }

        public BusinessIntegrationsManager GetIntegrationsManager()
        {
            return _businessIntegrationsManager;
        }

        public BusinessAgentsManager GetAgentsManager()
        {
            return _businessAgentsManager;
        }

        public BusinessNumberManager GetNumberManager()
        {
            return _businessNumberManager;
        }

        public BusinessRoutesManager GetRoutesManager()
        {
            return _businessRoutesManager;
        }
    }
}
