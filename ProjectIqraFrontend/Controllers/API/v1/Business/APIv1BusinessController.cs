using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.User.Business;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}")]
    public class APIv1BusinessController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly BusinessManager _businessManager;
        private readonly BusinessLogoRepository _businessLogoRepository;
        private readonly BusinessAgentAudioRepository _businessAgentAudioRepository;

        public APIv1BusinessController(
            ISessionValidationAndPermissionHelper sessionValidationAndPermissionHelper,
            BusinessManager businessManager,
            BusinessLogoRepository businessLogoRepository,
            BusinessAgentAudioRepository businessAgentAudioRepository
        ) {
            _userSessionValidationAndPermissionHelper = sessionValidationAndPermissionHelper;
            _businessManager = businessManager;
            _businessLogoRepository = businessLogoRepository;
            _businessAgentAudioRepository = businessAgentAudioRepository;
        }

        [HttpGet]
        public async Task<FunctionReturnResult<GetUserBusinessFullReturnModel?>> GetBusiness(long businessId)
        {
            var result = new FunctionReturnResult<GetUserBusinessFullReturnModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    checkAPIKeyBusinessRestriction: true,
                    // User Permissions
                    checkUserDisabled: true,
                    // User Business Permissions
                    checkUserBusinessesDisabled: true,
                    // Business Permissions
                    checkBusinessIsDisabled: true
                );
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusiness:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var userData = apiKeyValidaiton.Data!.userData!;
                var businessData = apiKeyValidaiton.Data!.businessData!;

                FunctionReturnResult<BusinessApp?> businessAppResult = await _businessManager.GetUserBusinessAppById(businessId, userData.Email);
                if (!businessAppResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusiness:{businessAppResult.Code}",
                        businessAppResult.Message
                    );
                }

                var businessMetaDataModel = new GetUseBusinessFullResultMetaDataModel(businessData);
                if (businessData.LogoS3StorageLink != null)
                {
                    businessMetaDataModel.LogoUrl = _businessLogoRepository.GeneratePresignedUrl(businessData.LogoS3StorageLink.ObjectName, 86400, businessData.LogoS3StorageLink.OriginRegion);
                }

                var businessAppModel = new GetUseBusinessFullResultAppModel(businessAppResult.Data!);
                foreach (var agent in businessAppResult.Data!.Agents)
                {
                    var modelAgent = businessAppModel.Agents.FirstOrDefault(a => a.Id == agent.Id);
                    if (modelAgent == null) continue;

                    if (agent.Settings.BackgroundAudioS3StorageLink != null)
                    {
                        modelAgent.Settings.BackgroundAudioUrl = _businessAgentAudioRepository.GeneratePresignedUrl(agent.Settings.BackgroundAudioS3StorageLink.ObjectName, 30000, agent.Settings.BackgroundAudioS3StorageLink.OriginRegion);
                    }
                }

                var fullBusinessReturnModel = new GetUserBusinessFullReturnModel()
                {
                    BusinessData = businessMetaDataModel,
                    BusinessApp = businessAppModel
                };

                return result.SetSuccessResult(fullBusinessReturnModel);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusiness:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
        }
    }
}
