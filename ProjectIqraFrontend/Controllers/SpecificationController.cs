using IqraCore.Entities.Embedding;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Languages;
using IqraCore.Entities.LLM;
using IqraCore.Entities.Rerank;
using IqraCore.Entities.STT;
using IqraCore.Entities.TTS;
using IqraCore.Models.Specification;
using IqraInfrastructure.Managers.Embedding;
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

        public SpecificationController(
            LanguagesManager languagesManager,
            RegionManager regionManager,
            IntegrationsManager integrationsManager,
            LLMProviderManager llmProviderManager,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            EmbeddingProviderManager embeddingProviderManager,
            RerankProviderManager rerankProviderManager,
            IntegrationsLogoRepository integrationsLogoRepository
        )
        {
            _languagesManager = languagesManager;
            _regionManager = regionManager;
            _integrationsManager = integrationsManager;
            _llmProviderManager = llmProviderManager;
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
            _embeddingProviderManager = embeddingProviderManager;
            _rerankProviderManager = rerankProviderManager;
            _integrationsLogoRepository = integrationsLogoRepository;
        }

        [HttpGet("/app/specification/languages")]
        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetAppLanguages()
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

            var getLanguagesListResult = await _languagesManager.GetAllLanguagesList();
            if (!getLanguagesListResult.Success)
            {
                return result.SetFailureResult(
                    $"GetAppLanguages:{getLanguagesListResult.Code}",
                    getLanguagesListResult.Message
                );
            }

            return result.SetSuccessResult(getLanguagesListResult.Data);
        }

        [HttpGet("/app/specification/integrations")]
        public async Task<FunctionReturnResult<List<IntegrationViewModel>?>> GetAvailableIntegrations()
        {
            var result = new FunctionReturnResult<List<IntegrationViewModel>?>();

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

        [HttpGet("/app/specification/regions")]
        public async Task<FunctionReturnResult<List<RegionViewModel>?>> GetRegions()
        {
            var result = new FunctionReturnResult<List<RegionViewModel>?>();

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

        [HttpGet("/app/specification/llmproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<LLMProviderData?>> GetLLMProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<LLMProviderData?>();

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

        [HttpGet("/app/specification/sttproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<STTProviderData?>> GetSTTProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<STTProviderData?>();

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

        [HttpGet("/app/specification/ttsproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<TTSProviderData?>> GetTTSProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<TTSProviderData?>();

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

        [HttpGet("/app/specification/embeddingproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<EmbeddingProviderData?>> GetEmbeddingProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData?>();

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

        [HttpGet("/app/specification/rerankproviders/getbyintegration/{integrationType}")]
        public async Task<FunctionReturnResult<RerankProviderData?>> GetRerankProviderByIntegrationType(string integrationType)
        {
            var result = new FunctionReturnResult<RerankProviderData?>();

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
    }
}
