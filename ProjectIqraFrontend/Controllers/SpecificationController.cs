using IqraCore.Entities.Embedding;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.LLM;
using IqraCore.Entities.Rerank;
using IqraCore.Entities.STT;
using IqraCore.Entities.TTS;
using IqraCore.Models.FlowApp;
using IqraCore.Models.Specification;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.FlowApp;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Repositories.Integrations;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class SpecificationController : Controller
    {
        private readonly LanguagesManager _languagesManager;
        private readonly RegionManager _regionManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly EmbeddingProviderManager _embeddingProviderManager;
        private readonly RerankProviderManager _rerankProviderManager;
        private readonly IntegrationsLogoRepository _integrationsLogoRepository;
        private readonly FlowAppManager _flowAppManager;

        public SpecificationController(
            LanguagesManager languagesManager,
            RegionManager regionManager,
            IntegrationsManager integrationsManager,
            LLMProviderManager llmProviderManager,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            EmbeddingProviderManager embeddingProviderManager,
            RerankProviderManager rerankProviderManager,
            IntegrationsLogoRepository integrationsLogoRepository,
            FlowAppManager flowAppManager
        ) {
            _languagesManager = languagesManager;
            _regionManager = regionManager;
            _integrationsManager = integrationsManager;
            _llmProviderManager = llmProviderManager;
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
            _embeddingProviderManager = embeddingProviderManager;
            _rerankProviderManager = rerankProviderManager;
            _integrationsLogoRepository = integrationsLogoRepository;
            _flowAppManager = flowAppManager;
        }

        [HttpGet("/app/specification/languages")]
        public async Task<FunctionReturnResult<List<LanguagesViewModel>?>> GetAppLanguages()
        {
            var result = new FunctionReturnResult<List<LanguagesViewModel>?>();

            try
            {
                var getLanguagesListResult = await _languagesManager.GetAllLanguagesList();
                if (!getLanguagesListResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetAppLanguages:{getLanguagesListResult.Code}",
                        getLanguagesListResult.Message
                    );
                }

                var models = getLanguagesListResult.Data!.Select(l => LanguagesViewModel.BuildModelFromEntity(l)).ToList();

                return result.SetSuccessResult(models);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetAppLanguages:EXCEPTION",
                    ex.Message
                );
            }
        }

        [HttpGet("/app/specification/integrations")]
        public async Task<FunctionReturnResult<List<IntegrationViewModel>?>> GetAvailableIntegrations()
        {
            var result = new FunctionReturnResult<List<IntegrationViewModel>?>();

            try
            {
                var getIntegrationsListResult = await _integrationsManager.GetIntegrationsList();
                if (!getIntegrationsListResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetAvailableIntegrations:{getIntegrationsListResult.Code}",
                        getIntegrationsListResult.Message
                    );
                }

                var resultDto = new List<IntegrationViewModel>();
                foreach (var item in getIntegrationsListResult.Data!)
                {
                    var dtoItem = new IntegrationViewModel(item);
                    if (item.LogoS3StorageLink != null)
                    {
                        dtoItem.LogoUrl = _integrationsLogoRepository.GeneratePresignedUrl(item.LogoS3StorageLink.ObjectName, 86400, item.LogoS3StorageLink.OriginRegion);
                    }

                    resultDto.Add(dtoItem);
                }

                return result.SetSuccessResult(resultDto);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetAvailableIntegrations:EXCEPTION",
                    ex.Message
                );
            }
        }

        [HttpGet("/app/specification/regions")]
        public async Task<FunctionReturnResult<List<RegionViewModel>?>> GetRegions()
        {
            var result = new FunctionReturnResult<List<RegionViewModel>?>();

            try
            {
                var getRegionsListResult = await _regionManager.GetRegions();
                if (!getRegionsListResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetRegions:{getRegionsListResult.Code}",
                        getRegionsListResult.Message
                    );
                }


#if DEBUG
                const bool includeDevServers = true;
#else
            const bool includeDevServers = false;
#endif

                var modelList = getRegionsListResult.Data!.Select(r => RegionViewModel.BuildViewModelFromEntity(r, false, includeDevServers)).ToList();

                return result.SetSuccessResult(modelList);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetRegions:EXCEPTION",
                    ex.Message
                );
            }
        }

        [HttpGet("/app/specification/llmproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<LLMProviderData?>> GetLLMProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<LLMProviderData?>();

            try
            {
                var getLLMProviderByIntegrationResult = await _llmProviderManager.GetProviderDataByIntegration(integrationType);
                if (!getLLMProviderByIntegrationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetLLMProviderByIntegrationType:{getLLMProviderByIntegrationResult.Code}",
                        getLLMProviderByIntegrationResult.Message
                    );
                }

                return result.SetSuccessResult(getLLMProviderByIntegrationResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetLLMProviderByIntegrationType:EXCEPTION",
                    ex.Message
                );
            }
        }

        [HttpGet("/app/specification/sttproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<STTProviderData?>> GetSTTProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<STTProviderData?>();

            try
            {
                var getSTTProviderByIntegrationResult = await _sttProviderManager.GetProviderDataByIntegration(integrationType);
                if (!getSTTProviderByIntegrationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetSTTProviderByIntegrationType:{getSTTProviderByIntegrationResult.Code}",
                        getSTTProviderByIntegrationResult.Message
                    );
                }

                return result.SetSuccessResult(getSTTProviderByIntegrationResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetSTTProviderByIntegrationType:EXCEPTION",
                    ex.Message
                );
            }
        }

        [HttpGet("/app/specification/ttsproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<TTSProviderData?>> GetTTSProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<TTSProviderData?>();

            try
            {
                var getTTSProviderByIntegrationResult = await _ttsProviderManager.GetProviderDataByIntegration(integrationType);
                if (!getTTSProviderByIntegrationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetTTSProviderByIntegrationType:{getTTSProviderByIntegrationResult.Code}",
                        getTTSProviderByIntegrationResult.Message
                    );
                }

                return result.SetSuccessResult(getTTSProviderByIntegrationResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetTTSProviderByIntegrationType:EXCEPTION",
                    ex.Message
                );
            }
        }

        [HttpGet("/app/specification/embeddingproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<EmbeddingProviderData?>> GetEmbeddingProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData?>();

            try
            {
                var getEmbeddingProviderByIntegrationResult = await _embeddingProviderManager.GetProviderDataByIntegration(integrationType);
                if (!getEmbeddingProviderByIntegrationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetEmbeddingProviderByIntegrationType:{getEmbeddingProviderByIntegrationResult.Code}",
                        getEmbeddingProviderByIntegrationResult.Message
                    );
                }

                return result.SetSuccessResult(getEmbeddingProviderByIntegrationResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetEmbeddingProviderByIntegrationType:EXCEPTION",
                    ex.Message
                );
            }
        }

        [HttpGet("/app/specification/rerankproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<RerankProviderData?>> GetRerankProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<RerankProviderData?>();

            try
            {
                var getRerankProviderByIntegrationResult = await _rerankProviderManager.GetProviderDataByIntegration(integrationType);
                if (!getRerankProviderByIntegrationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetRerankProviderByIntegrationType:{getRerankProviderByIntegrationResult.Code}",
                        getRerankProviderByIntegrationResult.Message
                    );
                }

                return result.SetSuccessResult(getRerankProviderByIntegrationResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetRerankProviderByIntegrationType:EXCEPTION",
                    ex.Message
                );
            }
        }

        [HttpGet("/app/specification/flowapps")]
        public async Task<FunctionReturnResult<List<FlowAppDefWithPermissionModel>>> GetFlowApps()
        {
            var result = new FunctionReturnResult<List<FlowAppDefWithPermissionModel>>();

            try
            {
                var apps = await _flowAppManager.GetAllAppDefinitionsWithPermissions();
                return result.SetSuccessResult(apps);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("GetFlowApps:EXCEPTION", ex.Message);
            }
        }
    }
}
