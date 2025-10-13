using IqraCore.Entities.Embedding;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Integrations;
using IqraCore.Entities.Languages;
using IqraCore.Entities.LLM;
using IqraCore.Entities.Region;
using IqraCore.Entities.Rerank;
using IqraCore.Entities.STT;
using IqraCore.Entities.TTS;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers
{
    public class SpecificationController : Controller
    {
        private readonly UserManager _userManager;
        private readonly LanguagesManager _languagesManager;
        private readonly RegionManager _regionManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly EmbeddingProviderManager _embeddingProviderManager;
        private readonly RerankProviderManager _rerankProviderManager;

        public SpecificationController(
            UserManager userManager,
            LanguagesManager languagesManager,
            RegionManager regionManager,
            IntegrationsManager integrationsManager,
            LLMProviderManager llmProviderManager,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            EmbeddingProviderManager embeddingProviderManager,
            RerankProviderManager rerankProviderManager
        )
        {
            _userManager = userManager;
            _languagesManager = languagesManager;
            _regionManager = regionManager;
            _integrationsManager = integrationsManager;
            _llmProviderManager = llmProviderManager;
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
            _embeddingProviderManager = embeddingProviderManager;
            _rerankProviderManager = rerankProviderManager;
        }

        [HttpPost("/app/specification/languages")]
        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetAppLanguages([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetAppLanguages:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetAppLanguages:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetAppLanguages:3";
                result.Message = "User not found";
                return result;
            }

            var getLanguagesListResult = await _languagesManager.GetAllLanguagesList();
            if (!getLanguagesListResult.Success)
            {
                result.Code = "GetAppLanguages:" + getLanguagesListResult.Code;
                result.Message = getLanguagesListResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = getLanguagesListResult.Data;
            return result;
        }

        [HttpPost("/app/specification/integrations")]
        public async Task<FunctionReturnResult<List<IntegrationData>?>> GetAvailableIntegrations([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<IntegrationData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetAvailableIntegrations:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetAvailableIntegrations:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetAvailableIntegrations:3";
                result.Message = "User not found";
                return result;
            }

            var getIntegrationsListResult = await _integrationsManager.GetIntegrationsList();
            if (!getIntegrationsListResult.Success)
            {
                result.Code = "GetAvailableIntegrations:" + getIntegrationsListResult.Code;
                result.Message = getIntegrationsListResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = getIntegrationsListResult.Data;
            return result;
        }

        [HttpPost("/app/specification/regions")]
        public async Task<FunctionReturnResult<List<RegionData>?>> GetRegions([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<RegionData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetRegions:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetRegions:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetRegions:3";
                result.Message = "User not found";
                return result;
            }

            var getRegionsListResult = await _regionManager.GetRegions();
            if (!getRegionsListResult.Success)
            {
                result.Code = "GetRegions:" + getRegionsListResult.Code;
                result.Message = getRegionsListResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = getRegionsListResult.Data;
            return result;
        }

        [HttpPost("/app/specification/llmproviders/getbyintegration")]
        public async Task<FunctionReturnResult<LLMProviderData?>> GetLLMProviderByIntegrationType([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetLLMProviderByIntegrationType:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetLLMProviderByIntegrationType:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetLLMProviderByIntegrationType:3";
                result.Message = "User not found";
                return result;
            }

            if (!formData.TryGetValue("integrationtype", out StringValues integrationTypeValue))
            {
                result.Code = "GetLLMProviderByIntegrationType:4";
                result.Message = "Integration type required";
                return result;
            }

            string integrationType = integrationTypeValue.ToString();
            if (string.IsNullOrEmpty(integrationType)) {
                result.Code = "GetLLMProviderByIntegrationType:5";
                result.Message = "Integration type missing";
                return result;
            }

            var getLLMProviderByIntegrationResult = await _llmProviderManager.GetProviderDataByIntegration(integrationType);
            if (!getLLMProviderByIntegrationResult.Success)
            {
                result.Code = "GetLLMProviderByIntegrationType:" + getLLMProviderByIntegrationResult.Code;
                result.Message = getLLMProviderByIntegrationResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = getLLMProviderByIntegrationResult.Data;
            return result;
        }

        [HttpPost("/app/specification/sttproviders/getbyintegration")]
        public async Task<FunctionReturnResult<STTProviderData?>> GetSTTProviderByIntegrationType([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<STTProviderData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetSTTProviderByIntegrationType:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetSTTProviderByIntegrationType:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetSTTProviderByIntegrationType:3";
                result.Message = "User not found";
                return result;
            }

            if (!formData.TryGetValue("integrationtype", out StringValues integrationTypeValue))
            {
                result.Code = "GetSTTProviderByIntegrationType:4";
                result.Message = "Integration type required";
                return result;
            }

            string integrationType = integrationTypeValue.ToString();
            if (string.IsNullOrEmpty(integrationType))
            {
                result.Code = "GetSTTProviderByIntegrationType:5";
                result.Message = "Integration type missing";
                return result;
            }

            var getSTTProviderByIntegrationResult = await _sttProviderManager.GetProviderDataByIntegration(integrationType);
            if (!getSTTProviderByIntegrationResult.Success)
            {
                result.Code = "GetSTTProviderByIntegrationType:" + getSTTProviderByIntegrationResult.Code;
                result.Message = getSTTProviderByIntegrationResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = getSTTProviderByIntegrationResult.Data;
            return result;
        }

        [HttpPost("/app/specification/ttsproviders/getbyintegration")]
        public async Task<FunctionReturnResult<TTSProviderData?>> GetTTSProviderByIntegrationType([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<TTSProviderData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetTTSProviderByIntegrationType:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetTTSProviderByIntegrationType:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetTTSProviderByIntegrationType:3";
                result.Message = "User not found";
                return result;
            }

            if (!formData.TryGetValue("integrationtype", out StringValues integrationTypeValue))
            {
                result.Code = "GetTTSProviderByIntegrationType:4";
                result.Message = "Integration type required";
                return result;
            }

            string integrationType = integrationTypeValue.ToString();
            if (string.IsNullOrEmpty(integrationType))
            {
                result.Code = "GetTTSProviderByIntegrationType:5";
                result.Message = "Integration type missing";
                return result;
            }

            var getTTSProviderByIntegrationResult = await _ttsProviderManager.GetProviderDataByIntegration(integrationType);
            if (!getTTSProviderByIntegrationResult.Success)
            {
                result.Code = "GetTTSProviderByIntegrationType:" + getTTSProviderByIntegrationResult.Code;
                result.Message = getTTSProviderByIntegrationResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = getTTSProviderByIntegrationResult.Data;
            return result;
        }

        [HttpPost("/app/specification/embeddingproviders/getbyintegration")]
        public async Task<FunctionReturnResult<EmbeddingProviderData?>> GetEmbeddingProviderByIntegrationType([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetEmbeddingProviderByIntegrationType:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetEmbeddingProviderByIntegrationType:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetEmbeddingProviderByIntegrationType:3";
                result.Message = "User not found";
                return result;
            }

            if (!formData.TryGetValue("integrationtype", out StringValues integrationTypeValue))
            {
                result.Code = "GetEmbeddingProviderByIntegrationType:4";
                result.Message = "Integration type required";
                return result;
            }

            string integrationType = integrationTypeValue.ToString();
            if (string.IsNullOrEmpty(integrationType))
            {
                result.Code = "GetEmbeddingProviderByIntegrationType:5";
                result.Message = "Integration type missing";
                return result;
            }

            var getEmbeddingProviderByIntegrationResult = await _embeddingProviderManager.GetProviderDataByIntegration(integrationType);
            if (!getEmbeddingProviderByIntegrationResult.Success)
            {
                result.Code = "GetEmbeddingProviderByIntegrationType:" + getEmbeddingProviderByIntegrationResult.Code;
                result.Message = getEmbeddingProviderByIntegrationResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = getEmbeddingProviderByIntegrationResult.Data;
            return result;
        }

        [HttpPost("/app/specification/rerankproviders/getbyintegration")]
        public async Task<FunctionReturnResult<RerankProviderData?>> GetRerankProviderByIntegrationType([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<RerankProviderData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetRerankProviderByIntegrationType:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetRerankProviderByIntegrationType:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetRerankProviderByIntegrationType:3";
                result.Message = "User not found";
                return result;
            }

            if (!formData.TryGetValue("integrationtype", out StringValues integrationTypeValue))
            {
                result.Code = "GetRerankProviderByIntegrationType:4";
                result.Message = "Integration type required";
                return result;
            }

            string integrationType = integrationTypeValue.ToString();
            if (string.IsNullOrEmpty(integrationType))
            {
                result.Code = "GetRerankProviderByIntegrationType:5";
                result.Message = "Integration type missing";
                return result;
            }

            var getRerankProviderByIntegrationResult = await _rerankProviderManager.GetProviderDataByIntegration(integrationType);
            if (!getRerankProviderByIntegrationResult.Success)
            {
                result.Code = "GetRerankProviderByIntegrationType:" + getRerankProviderByIntegrationResult.Code;
                result.Message = getRerankProviderByIntegrationResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = getRerankProviderByIntegrationResult.Data;
            return result;
        }
    }
}
